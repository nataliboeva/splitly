using CsCheck;
using Splitly.Domain.Splits;

namespace Splitly.Domain.Tests.Splits;

public class AllocatorTests
{
    private static readonly Gen<long> Totals = Gen.Long[0, 1_000_000_000_000];
    private static readonly Gen<long[]> Weights = Gen.Long[1, 1_000_000].Array[1, 30];

    [Fact]
    public void Leftover_cent_goes_to_the_first_person_when_everyone_is_equal()
    {
        Allocator.Allocate(100, [1, 1, 1]).ShouldBe([34, 33, 33]);
    }

    [Fact]
    public void Leftover_cents_go_to_the_largest_remainders()
    {
        // 100 * 1/3 = 33.33 and 100 * 2/3 = 66.67, so the second part has the larger remainder.
        Allocator.Allocate(100, [1, 2]).ShouldBe([33, 67]);
    }

    [Fact]
    public void Ties_are_broken_by_position()
    {
        Allocator.Allocate(5, [1, 1, 1, 1, 1, 1, 1]).ShouldBe([1, 1, 1, 1, 1, 0, 0]);
    }

    [Fact]
    public void Exact_divisions_have_no_leftover()
    {
        Allocator.Allocate(9000, [50, 30, 20]).ShouldBe([4500, 2700, 1800]);
    }

    [Fact]
    public void Zero_total_gives_zero_parts()
    {
        Allocator.Allocate(0, [3, 1]).ShouldBe([0, 0]);
    }

    [Fact]
    public void Invalid_input_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Allocator.Allocate(-1, [1]));
        Should.Throw<ArgumentException>(() => Allocator.Allocate(100, []));
        Should.Throw<ArgumentException>(() => Allocator.Allocate(100, [1, 0]));
    }

    [Fact]
    public void Parts_always_add_up_to_the_total()
    {
        Gen.Select(Totals, Weights).Sample((total, weights) =>
            Allocator.Allocate(total, weights).Sum() == total);
    }

    [Fact]
    public void Each_part_is_less_than_one_unit_away_from_its_exact_share()
    {
        Gen.Select(Totals, Weights).Sample((total, weights) =>
        {
            var parts = Allocator.Allocate(total, weights);
            Int128 weightSum = weights.Sum();

            // part - total * weight / weightSum lies in (-1, 1); multiplied through by weightSum
            // to stay in exact integer arithmetic.
            return parts.Select((part, i) => (part * weightSum) - ((Int128)total * weights[i]))
                .All(error => error > -weightSum && error < weightSum);
        });
    }

    [Fact]
    public void Equal_weights_give_parts_that_differ_by_at_most_one()
    {
        Gen.Select(Totals, Gen.Int[1, 50]).Sample((total, people) =>
        {
            var parts = Allocator.Allocate(total, Enumerable.Repeat(1L, people).ToArray());
            return parts.Max() - parts.Min() <= 1;
        });
    }

    [Fact]
    public void Allocation_is_deterministic()
    {
        Gen.Select(Totals, Weights).Sample((total, weights) =>
            Allocator.Allocate(total, weights).SequenceEqual(Allocator.Allocate(total, weights)));
    }
}
