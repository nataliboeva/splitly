using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Common;
using Splitly.Api.Data;
using Splitly.Domain.Activity;

namespace Splitly.Api.Features.Activity;

/// <summary>Display as "{ActorName} {Message}", e.g. "Ana added "Dinner" (60.00 EUR)".</summary>
public sealed record ActivityResponse(
    Guid Id, ActivityType Type, Guid ActorId, string ActorName, string Message, DateTimeOffset CreatedAt);

public static class ActivityEndpoints
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var activity = app.MapGroup("/api/groups/{groupId:guid}/activity")
            .RequireAuthorization()
            .RequireGroupMembership()
            .WithTags("Activity");

        activity.MapGet("/", ListActivity);

        return app;
    }

    private static async Task<Ok<List<ActivityResponse>>> ListActivity(
        Guid groupId, AppDbContext db, CancellationToken cancellationToken, int? limit = null)
    {
        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

        var entries = await (
                from entry in db.ActivityEntries
                join actor in db.Users on entry.ActorId equals actor.Id
                where entry.GroupId == groupId
                orderby entry.CreatedAt descending, entry.Id descending
                select new ActivityResponse(
                    entry.Id, entry.Type, entry.ActorId, actor.DisplayName ?? actor.Email!, entry.Message, entry.CreatedAt))
            .Take(take)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(entries);
    }
}
