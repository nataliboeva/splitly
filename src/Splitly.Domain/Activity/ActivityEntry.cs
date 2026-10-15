using Splitly.Domain.Expenses;
using Splitly.Domain.Groups;
using Splitly.Domain.Settlements;

namespace Splitly.Domain.Activity;

/// <summary>
/// One line in a group's activity feed. The message is written when the entry is created, so the
/// history stays accurate even after the expense it mentions is edited or deleted.
/// It reads as a sentence that follows the actor's name, e.g. <c>added "Dinner" (60.00 EUR)</c>.
/// </summary>
public sealed class ActivityEntry
{
    private ActivityEntry()
    {
        // Used by EF Core.
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    public Guid ActorId { get; private set; }

    public ActivityType Type { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ActivityEntry GroupCreated(Group group) =>
        Create(group, group.CreatedById, ActivityType.GroupCreated, $"created the group \"{group.Name}\"", group.CreatedAt);

    public static ActivityEntry MemberAdded(Group group, string memberName, Guid actorId, DateTimeOffset now) =>
        Create(group, actorId, ActivityType.MemberAdded, $"added {memberName} to the group", now);

    public static ActivityEntry ExpenseAdded(Group group, Expense expense, Guid actorId, DateTimeOffset now) =>
        Create(group, actorId, ActivityType.ExpenseAdded, $"added {Describe(group, expense)}", now);

    public static ActivityEntry ExpenseUpdated(Group group, Expense expense, Guid actorId, DateTimeOffset now) =>
        Create(group, actorId, ActivityType.ExpenseUpdated, $"updated {Describe(group, expense)}", now);

    public static ActivityEntry ExpenseDeleted(Group group, Expense expense, Guid actorId, DateTimeOffset now) =>
        Create(group, actorId, ActivityType.ExpenseDeleted, $"deleted {Describe(group, expense)}", now);

    public static ActivityEntry SettlementRecorded(
        Group group, Settlement settlement, string fromName, string toName, Guid actorId, DateTimeOffset now)
    {
        var amount = group.Currency.Format(settlement.Amount);
        return Create(
            group, actorId, ActivityType.SettlementRecorded, $"recorded a payment of {amount} from {fromName} to {toName}", now);
    }

    private static string Describe(Group group, Expense expense) =>
        $"\"{expense.Description}\" ({group.Currency.Format(expense.Amount)})";

    private static ActivityEntry Create(
        Group group, Guid actorId, ActivityType type, string message, DateTimeOffset now) => new()
        {
            Id = Guid.CreateVersion7(now),
            GroupId = group.Id,
            ActorId = actorId,
            Type = type,
            Message = message,
            CreatedAt = now,
        };
}
