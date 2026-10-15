using Splitly.Domain.Groups;

namespace Splitly.Domain.Settlements;

/// <summary>A payment from one member to another that settles (part of) a debt.</summary>
public sealed class Settlement
{
    private Settlement()
    {
        // Used by EF Core.
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    public Guid FromUserId { get; private set; }

    public Guid ToUserId { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly Date { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Settlement> Create(
        Group group, Guid fromUserId, Guid toUserId, decimal amount, DateOnly date, Guid createdById, DateTimeOffset now)
    {
        if (fromUserId == toUserId)
        {
            return Result<Settlement>.Failure("A payment needs two different people.");
        }

        if (!group.IsMember(fromUserId) || !group.IsMember(toUserId))
        {
            return Result<Settlement>.Failure("Both people must be members of the group.");
        }

        if (amount <= 0)
        {
            return Result<Settlement>.Failure("The amount must be greater than zero.");
        }

        if (!group.Currency.TryToMinorUnits(amount, out _))
        {
            return Result<Settlement>.Failure(
                $"{group.Currency.Code} amounts can have at most {group.Currency.MinorUnits} decimal places.");
        }

        return new Settlement
        {
            Id = Guid.CreateVersion7(now),
            GroupId = group.Id,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            Date = date,
            CreatedById = createdById,
            CreatedAt = now,
        };
    }
}
