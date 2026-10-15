using Microsoft.EntityFrameworkCore;
using Splitly.Api.Data;
using Splitly.Domain.Groups;

namespace Splitly.Api.Features.Groups;

internal static class GroupQueries
{
    /// <summary>
    /// Loads a group with its members, read-only. Only call this behind the membership filter,
    /// which has already confirmed that the group exists.
    /// </summary>
    public static Task<Group> GetGroupWithMembersAsync(
        this AppDbContext db, Guid groupId, CancellationToken cancellationToken) =>
        db.Groups.AsNoTracking().Include(g => g.Members).SingleAsync(g => g.Id == groupId, cancellationToken);

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
