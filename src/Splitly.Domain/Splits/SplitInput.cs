namespace Splitly.Domain.Splits;

/// <summary>
/// One person taking part in a split. What <see cref="Value"/> means depends on the split method:
/// it is ignored for <see cref="SplitMethod.Equal"/>, an amount for <see cref="SplitMethod.Exact"/>,
/// a percentage for <see cref="SplitMethod.Percentage"/> and a share count for <see cref="SplitMethod.Shares"/>.
/// </summary>
public sealed record SplitInput(Guid UserId, decimal? Value = null);
