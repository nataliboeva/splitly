using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Common;
using Splitly.Api.Data;
using Splitly.Api.Features.Users;
using Splitly.Domain;
using Splitly.Domain.Activity;
using Splitly.Domain.Groups;

namespace Splitly.Api.Features.Groups;

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/currencies", ListCurrencies).WithTags("Groups");

        var groups = app.MapGroup("/api/groups").RequireAuthorization().WithTags("Groups");
        groups.MapGet("/", ListMyGroups);
        groups.MapPost("/", CreateGroup).WithValidation<CreateGroupRequest>();

        var group = groups.MapGroup("/{groupId:guid}").RequireGroupMembership();
        group.MapGet("/", GetGroup);
        group.MapPost("/members", AddMember).WithValidation<AddMemberRequest>();

        return app;
    }

    private static Ok<CurrencyResponse[]> ListCurrencies() =>
        TypedResults.Ok(Currency.Supported.Select(c => new CurrencyResponse(c.Code, c.MinorUnits)).ToArray());

    private static async Task<Ok<List<GroupSummaryResponse>>> ListMyGroups(
        ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var rows = await db.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new { g.Id, g.Name, g.Currency, MemberCount = g.Members.Count, g.CreatedAt })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(rows
            .Select(g => new GroupSummaryResponse(g.Id, g.Name, g.Currency.Code, g.MemberCount, g.CreatedAt))
            .ToList());
    }

    private static async Task<Created<GroupResponse>> CreateGroup(
        CreateGroupRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var group = Group.Create(request.Name, Currency.FromCode(request.Currency), principal.GetUserId(), clock.GetUtcNow());

        db.Groups.Add(group);
        db.ActivityEntries.Add(ActivityEntry.GroupCreated(group));
        await db.SaveChangesAsync(cancellationToken);

        var response = await GetGroupResponseAsync(db, group.Id, cancellationToken);
        return TypedResults.Created($"/api/groups/{group.Id}", response);
    }

    private static async Task<Ok<GroupResponse>> GetGroup(
        Guid groupId, AppDbContext db, CancellationToken cancellationToken) =>
        TypedResults.Ok(await GetGroupResponseAsync(db, groupId, cancellationToken));

    private static async Task<Results<Created<GroupMemberResponse>, ValidationProblem, ProblemHttpResult>> AddMember(
        Guid groupId,
        AddMemberRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        UserManager<AppUser> users,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(AddMemberRequest.Email)] = ["There is no Splitly account with this email address."],
            });
        }

        var group = await db.Groups.Include(g => g.Members).SingleAsync(g => g.Id == groupId, cancellationToken);
        var now = clock.GetUtcNow();

        var result = group.AddMember(user.Id, now);
        if (!result.IsSuccess)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Error);
        }

        db.ActivityEntries.Add(ActivityEntry.MemberAdded(group, user.Name, principal.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/groups/{groupId}", new GroupMemberResponse(user.Id, user.Name, user.Email!));
    }

    private static async Task<GroupResponse> GetGroupResponseAsync(
        AppDbContext db, Guid groupId, CancellationToken cancellationToken)
    {
        var group = await db.Groups.AsNoTracking().SingleAsync(g => g.Id == groupId, cancellationToken);
        var members = await db.GetMembersAsync(groupId, cancellationToken);

        return new GroupResponse(group.Id, group.Name, group.Currency.Code, group.CreatedAt, members);
    }
}
