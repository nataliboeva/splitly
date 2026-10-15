using Splitly.Domain.Groups;
using Splitly.Domain.Splits;

namespace Splitly.Domain.Expenses;

public sealed class Expense
{
    public const int DescriptionMaxLength = 200;

    private readonly List<ExpenseShare> _shares = [];

    private Expense()
    {
        // Used by EF Core.
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    public Guid PaidById { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateOnly Date { get; private set; }

    public SplitMethod SplitMethod { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<ExpenseShare> Shares => _shares;

    public static Result<Expense> Create(Group group, ExpenseDetails details, Guid createdById, DateTimeOffset now)
    {
        var expense = new Expense
        {
            Id = Guid.CreateVersion7(now),
            GroupId = group.Id,
            CreatedById = createdById,
            CreatedAt = now,
        };

        var error = expense.Apply(group, details);
        return error is null ? expense : Result<Expense>.Failure(error);
    }

    public Result<Expense> Update(Group group, ExpenseDetails details, DateTimeOffset now)
    {
        if (group.Id != GroupId)
        {
            throw new ArgumentException("The expense belongs to a different group.", nameof(group));
        }

        var error = Apply(group, details);
        if (error is not null)
        {
            return Result<Expense>.Failure(error);
        }

        UpdatedAt = now;
        return this;
    }

    /// <summary>Only the person who added an expense may edit or delete it.</summary>
    public bool CanBeChangedBy(Guid userId) => CreatedById == userId;

    // Validates everything first and only then changes state, so a failed update leaves the expense untouched.
    private string? Apply(Group group, ExpenseDetails details)
    {
        if (!group.IsMember(details.PaidById))
        {
            return "The person who paid must be a member of the group.";
        }

        if (details.Splits.Any(s => !group.IsMember(s.UserId)))
        {
            return "Everyone in the split must be a member of the group.";
        }

        var split = SplitCalculator.Calculate(details.Amount, group.Currency, details.SplitMethod, details.Splits);
        if (!split.IsSuccess)
        {
            return split.Error;
        }

        PaidById = details.PaidById;
        Amount = details.Amount;
        Description = details.Description.Trim();
        Date = details.Date;
        SplitMethod = details.SplitMethod;

        var enteredValues = details.Splits.ToDictionary(s => s.UserId, s => s.Value);
        _shares.Clear();
        _shares.AddRange(split.Value.Select(share => new ExpenseShare(
            Id,
            share.UserId,
            share.Amount,
            details.SplitMethod == SplitMethod.Equal ? null : enteredValues[share.UserId])));

        return null;
    }
}
