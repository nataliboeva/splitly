namespace Splitly.Domain.Splits;

public enum SplitMethod
{
    /// <summary>Everyone pays the same amount.</summary>
    Equal,

    /// <summary>Each person's amount is entered explicitly.</summary>
    Exact,

    /// <summary>Each person pays a percentage of the total.</summary>
    Percentage,

    /// <summary>Each person pays in proportion to a number of shares, e.g. 2 shares for a couple.</summary>
    Shares,
}
