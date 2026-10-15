using Splitly.Domain.Splits;

namespace Splitly.Domain.Expenses;

/// <summary>Everything a user can set on an expense, used both when creating and when editing one.</summary>
public sealed record ExpenseDetails(
    Guid PaidById,
    decimal Amount,
    string Description,
    DateOnly Date,
    SplitMethod SplitMethod,
    IReadOnlyList<SplitInput> Splits);
