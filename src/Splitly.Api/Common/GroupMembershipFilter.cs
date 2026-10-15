using Microsoft.EntityFrameworkCore;
using Splitly.Api.Data;

namespace Splitly.Api.Common;

/// <summary>
/// Lets a request through only when the signed-in user is a member of the group in the
/// <c>{groupId}</c> route value. Applied to whole route groups so no endpoint can forget the check.
/// </summary>
public sealed class GroupMembershipFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.GetRouteValue("groupId")?.ToString(), out var groupId))
        {
            return ApiProblems.NotFound("Group not found.");
        }

        var userId = http.User.GetUserId();
        var db = http.RequestServices.GetRequiredService<AppDbContext>();
        var isMember = await db.GroupMembers.AnyAsync(
            m => m.GroupId == groupId && m.UserId == userId, http.RequestAborted);

        // Outsiders get a 404 rather than a 403, so they can't tell which group ids exist.
        return isMember ? await next(context) : ApiProblems.NotFound("Group not found.");
    }
}

public static class GroupMembershipFilterExtensions
{
    public static RouteGroupBuilder RequireGroupMembership(this RouteGroupBuilder group) =>
        group.AddEndpointFilter<RouteGroupBuilder, GroupMembershipFilter>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
