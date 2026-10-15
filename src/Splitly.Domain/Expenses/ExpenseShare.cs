namespace Splitly.Domain.Expenses;

/// <summary>What one person owes for an expense.</summary>
public sealed class ExpenseShare
{
    private ExpenseShare()
    {
        // Used by EF Core.
    }

    internal ExpenseShare(Guid expenseId, Guid userId, decimal amount, decimal? value)
    {
        Id = Guid.CreateVersion7();
        ExpenseId = expenseId;
        UserId = userId;
        Amount = amount;
        Value = value;
    }

    public Guid Id { get; private set; }

    public Guid ExpenseId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>The calculated amount this person owes.</summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// The percentage, share count or exact amount the user entered, kept so the expense can be edited
    /// later exactly as it was entered. Null for equal splits.
    /// </summary>
    public decimal? Value { get; private set; }
}
