using Microsoft.EntityFrameworkCore;
using Splitly.Api.Data;

namespace Splitly.Api.Features.Groups;

internal static class MemberQueries
{
    public static Task<List<GroupMemberResponse>> GetMembersAsync(
        this AppDbContext db, Guid groupId, CancellationToken cancellationToken) =>
        (from member in db.GroupMembers
         join user in db.Users on member.UserId equals user.Id
         where member.GroupId == groupId
         orderby member.JoinedAt
         select new GroupMemberResponse(user.Id, user.DisplayName ?? user.Email!, user.Email!))
        .ToListAsync(cancellationToken);

    public static Task<Dictionary<Guid, string>> GetNamesAsync(
        this AppDbContext db, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? u.Email!, cancellationToken);
}
