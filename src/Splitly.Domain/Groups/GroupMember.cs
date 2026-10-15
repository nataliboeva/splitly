namespace Splitly.Domain.Groups;

public sealed class GroupMember
{
    private GroupMember()
    {
        // Used by EF Core.
    }

    internal GroupMember(Guid groupId, Guid userId, DateTimeOffset joinedAt)
    {
        GroupId = groupId;
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }
}
