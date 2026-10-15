namespace Splitly.Domain.Groups;

public sealed class Group
{
    public const int NameMaxLength = 100;

    private readonly List<GroupMember> _members = [];

    private Group()
    {
        // Used by EF Core.
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Currency Currency { get; private set; } = null!;

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<GroupMember> Members => _members;

    /// <summary>Creates a group with its creator as the first member.</summary>
    public static Group Create(string name, Currency currency, Guid creatorId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var group = new Group
        {
            Id = Guid.CreateVersion7(now),
            Name = name.Trim(),
            Currency = currency,
            CreatedById = creatorId,
            CreatedAt = now,
        };
        group._members.Add(new GroupMember(group.Id, creatorId, now));

        return group;
    }

    public bool IsMember(Guid userId) => _members.Exists(m => m.UserId == userId);

    public Result<GroupMember> AddMember(Guid userId, DateTimeOffset now)
    {
        if (IsMember(userId))
        {
            return Result<GroupMember>.Failure("This person is already a member of the group.");
        }

        var member = new GroupMember(Id, userId, now);
        _members.Add(member);

        return member;
    }
}
