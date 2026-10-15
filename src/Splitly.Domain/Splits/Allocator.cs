namespace Splitly.Domain.Splits;

public static class Allocator
{
    /// <summary>
    /// Splits <paramref name="total"/> minor units proportionally to <paramref name="weights"/> using the
    /// largest remainder method, so the parts always add up to exactly the total.
    /// </summary>
    /// <remarks>
    /// Every part is first rounded down. The few units lost to rounding (always fewer than the number of
    /// parts) are then handed out one at a time to the parts with the largest fractional remainder.
    /// Ties go to the earlier part, which keeps the result deterministic.
    /// All arithmetic is done on integers, so there is no floating point error to reason about.
    /// </remarks>
    public static long[] Allocate(long total, IReadOnlyList<long> weights)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        if (weights.Count == 0)
        {
            throw new ArgumentException("At least one weight is required.", nameof(weights));
        }

        if (weights.Any(w => w <= 0))
        {
            throw new ArgumentException("Weights must be positive.", nameof(weights));
        }

        Int128 weightSum = 0;
        foreach (var weight in weights)
        {
            weightSum += weight;
        }

        var parts = new long[weights.Count];
        var remainders = new Int128[weights.Count];
        long allocated = 0;

        for (var i = 0; i < weights.Count; i++)
        {
            var exactShare = (Int128)total * weights[i];
            parts[i] = (long)(exactShare / weightSum);
            remainders[i] = exactShare % weightSum;
            allocated += parts[i];
        }

        var leftover = (int)(total - allocated);
        var luckyIndexes = Enumerable.Range(0, weights.Count)
            .OrderByDescending(i => remainders[i])
            .ThenBy(i => i)
            .Take(leftover);

        foreach (var i in luckyIndexes)
        {
            parts[i]++;
        }

        return parts;
    }
}
