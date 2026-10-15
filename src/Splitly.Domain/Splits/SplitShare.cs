namespace Splitly.Domain.Splits;

/// <summary>The amount one person owes for an expense.</summary>
public sealed record SplitShare(Guid UserId, decimal Amount);
