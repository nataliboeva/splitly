using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Common;
using Splitly.Api.Data;
using Splitly.Api.Features.Groups;
using Splitly.Domain.Balances;

namespace Splitly.Api.Features.Balances;

/// <summary>Positive means the group owes this member money; negative means the member owes the group.</summary>
public sealed record MemberBalanceResponse(Guid UserId, decimal Amount);

public sealed record TransferResponse(Guid FromUserId, Guid ToUserId, decimal Amount);

public sealed record GroupBalancesResponse(
    string Currency,
    IReadOnlyList<MemberBalanceResponse> Balances,
    IReadOnlyList<TransferResponse> SuggestedTransfers);

public static class BalanceEndpoints
{
    public static IEndpointRouteBuilder MapBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var balances = app.MapGroup("/api/groups/{groupId:guid}/balances")
            .RequireAuthorization()
            .RequireGroupMembership()
            .WithTags("Balances");

        balances.MapGet("/", GetBalances);

        return app;
    }

    // Balances are recalculated from the full history on every request instead of being stored, so they
    // can never drift out of sync with the expenses. That is plenty fast for groups of friends.
    private static async Task<Ok<GroupBalancesResponse>> GetBalances(
        Guid groupId, AppDbContext db, CancellationToken cancellationToken)
    {
        var group = await db.GetGroupWithMembersAsync(groupId, cancellationToken);
        var expenses = await db.Expenses
            .AsNoTracking()
            .Include(e => e.Shares)
            .Where(e => e.GroupId == groupId)
            .ToListAsync(cancellationToken);
        var settlements = await db.Settlements
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var members = group.Members.OrderBy(m => m.JoinedAt).Select(m => m.UserId).ToList();
        var balances = BalanceCalculator.Calculate(members, expenses, settlements);
        var transfers = DebtSimplifier.Simplify(balances);

        return TypedResults.Ok(new GroupBalancesResponse(
            group.Currency.Code,
            [.. members.Select(id => new MemberBalanceResponse(id, balances[id]))],
            [.. transfers.Select(t => new TransferResponse(t.FromUserId, t.ToUserId, t.Amount))]));
    }
}
