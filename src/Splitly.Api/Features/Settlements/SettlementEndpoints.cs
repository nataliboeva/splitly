using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Splitly.Api.Common;
using Splitly.Api.Data;
using Splitly.Api.Features.Groups;
using Splitly.Domain.Activity;
using Splitly.Domain.Settlements;

namespace Splitly.Api.Features.Settlements;

public static class SettlementEndpoints
{
    public static IEndpointRouteBuilder MapSettlementEndpoints(this IEndpointRouteBuilder app)
    {
        var settlements = app.MapGroup("/api/groups/{groupId:guid}/settlements")
            .RequireAuthorization()
            .RequireGroupMembership()
            .WithTags("Settlements");

        settlements.MapPost("/", RecordSettlement).WithValidation<RecordSettlementRequest>();

        return app;
    }

    private static async Task<Results<Created<SettlementResponse>, ProblemHttpResult>> RecordSettlement(
        Guid groupId,
        RecordSettlementRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var group = await db.GetGroupWithMembersAsync(groupId, cancellationToken);
        var userId = principal.GetUserId();
        var now = clock.GetUtcNow();

        var result = Settlement.Create(
            group, request.FromUserId, request.ToUserId, request.Amount, request.Date, userId, now);
        if (!result.IsSuccess)
        {
            return ApiProblems.BusinessRule(result.Error);
        }

        var settlement = result.Value;
        var names = await db.GetNamesAsync([settlement.FromUserId, settlement.ToUserId], cancellationToken);

        db.Settlements.Add(settlement);
        db.ActivityEntries.Add(ActivityEntry.SettlementRecorded(
            group, settlement, names[settlement.FromUserId], names[settlement.ToUserId], userId, now));
        await db.SaveChangesAsync(cancellationToken);

        // Settlements show up in balances and the activity feed; there is no endpoint to fetch one on its own.
        return TypedResults.Created((string?)null, SettlementResponse.From(settlement));
    }
}
