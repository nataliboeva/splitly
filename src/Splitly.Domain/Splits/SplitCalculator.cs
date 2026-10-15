using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Splitly.Domain.Splits;

/// <summary>
/// Turns an expense amount and a split definition into the exact amount each person owes.
/// The returned shares always add up to the expense amount, to the last cent.
/// </summary>
public static class SplitCalculator
{
    // Percentages and share counts may use up to two decimal places (33.33% or 1.5 shares).
    // Scaling them by 100 turns them into whole-number weights for the allocator.
    private const decimal WeightScale = 100m;
    private const decimal MaxWeightValue = 1_000_000m;

    public static Result<IReadOnlyList<SplitShare>> Calculate(
        decimal amount,
        Currency currency,
        SplitMethod method,
        IReadOnlyList<SplitInput> participants)
    {
        if (amount <= 0)
        {
            return Fail("The amount must be greater than zero.");
        }

        if (!currency.TryToMinorUnits(amount, out var totalMinor))
        {
            return Fail($"{currency.Code} amounts can have at most {currency.MinorUnits} decimal places.");
        }

        if (participants.Count == 0)
        {
            return Fail("Choose at least one person to split with.");
        }

        if (participants.DistinctBy(p => p.UserId).Count() != participants.Count)
        {
            return Fail("Each person can only appear once in a split.");
        }

        return method switch
        {
            SplitMethod.Equal => Proportionally(totalMinor, currency, participants, [.. participants.Select(_ => 1L)]),
            SplitMethod.Shares => ByShares(totalMinor, currency, participants),
            SplitMethod.Percentage => ByPercentage(totalMinor, currency, participants),
            SplitMethod.Exact => ByExactAmounts(amount, currency, participants),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown split method."),
        };
    }

    private static Result<IReadOnlyList<SplitShare>> ByShares(
        long totalMinor, Currency currency, IReadOnlyList<SplitInput> participants)
    {
        return TryGetWeights(participants, out var weights, out var error)
            ? Proportionally(totalMinor, currency, participants, weights)
            : Fail(error);
    }

    private static Result<IReadOnlyList<SplitShare>> ByPercentage(
        long totalMinor, Currency currency, IReadOnlyList<SplitInput> participants)
    {
        if (!TryGetWeights(participants, out var weights, out var error))
        {
            return Fail(error);
        }

        var totalPercentage = participants.Sum(p => p.Value!.Value);
        if (totalPercentage != 100m)
        {
            var formatted = totalPercentage.ToString("0.##", CultureInfo.InvariantCulture);
            return Fail($"Percentages must add up to 100% (they add up to {formatted}%).");
        }

        return Proportionally(totalMinor, currency, participants, weights);
    }

    private static Result<IReadOnlyList<SplitShare>> ByExactAmounts(
        decimal amount, Currency currency, IReadOnlyList<SplitInput> participants)
    {
        var shares = new List<SplitShare>(participants.Count);
        foreach (var participant in participants)
        {
            if (participant.Value is not { } value || value <= 0)
            {
                return Fail("Each person needs an amount greater than zero.");
            }

            if (!currency.TryToMinorUnits(value, out _))
            {
                return Fail($"{currency.Code} amounts can have at most {currency.MinorUnits} decimal places.");
            }

            shares.Add(new SplitShare(participant.UserId, value));
        }

        var sum = shares.Sum(s => s.Amount);
        if (sum != amount)
        {
            return Fail(
                $"The amounts must add up to {currency.Format(amount)} (they add up to {currency.Format(sum)}).");
        }

        return Result<IReadOnlyList<SplitShare>>.Success(shares);
    }

    private static Result<IReadOnlyList<SplitShare>> Proportionally(
        long totalMinor, Currency currency, IReadOnlyList<SplitInput> participants, IReadOnlyList<long> weights)
    {
        var parts = Allocator.Allocate(totalMinor, weights);
        var shares = participants
            .Select((participant, i) => new SplitShare(participant.UserId, currency.FromMinorUnits(parts[i])))
            .ToList();

        return Result<IReadOnlyList<SplitShare>>.Success(shares);
    }

    private static bool TryGetWeights(
        IReadOnlyList<SplitInput> participants,
        out List<long> weights,
        [NotNullWhen(false)] out string? error)
    {
        weights = new List<long>(participants.Count);
        foreach (var participant in participants)
        {
            if (participant.Value is not { } value || value <= 0 || value > MaxWeightValue)
            {
                error = "Each person needs a value greater than zero and no more than 1,000,000.";
                return false;
            }

            var scaled = value * WeightScale;
            if (scaled != decimal.Truncate(scaled))
            {
                error = "Percentages and shares can have at most two decimal places.";
                return false;
            }

            weights.Add(decimal.ToInt64(scaled));
        }

        error = null;
        return true;
    }

    private static Result<IReadOnlyList<SplitShare>> Fail(string error) =>
        Result<IReadOnlyList<SplitShare>>.Failure(error);
}
