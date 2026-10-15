using CsCheck;
using Splitly.Domain.Splits;

namespace Splitly.Domain.Tests.Splits;

/// <summary>
/// Property-based tests: CsCheck generates thousands of random expenses and checks that the
/// rules hold for every one of them, shrinking any failure to the smallest example.
/// </summary>
public class SplitCalculatorPropertyTests
{
    private const int Iterations = 2_000;

    private static readonly Gen<Currency> Currencies =
        Gen.OneOfConst(Currency.FromCode("EUR"), Currency.FromCode("JPY"));

    private static readonly Gen<long> MinorAmounts = Gen.Long[1, 100_000_000];

    private static readonly Gen<(Currency Currency, decimal Amount)> Expenses =
        Gen.Select(Currencies, MinorAmounts, (currency, minor) => (currency, currency.FromMinorUnits(minor)));

    // Share counts between 0.01 and 100.
    private static readonly Gen<decimal[]> ShareCounts = Gen.Int[1, 10_000].Select(v => v / 100m).Array[1, 20];

    // Random cut points on 0..100.00% give between 1 and 20 percentages that add up to exactly 100.
    private static readonly Gen<decimal[]> Percentages = Gen.Int[1, 9_999].Array[0, 19].Select(cuts =>
    {
        int[] points = [0, .. cuts.Distinct().Order(), 10_000];
        return points.Zip(points.Skip(1), (from, to) => (to - from) / 100m).ToArray();
    });

    [Fact]
    public void Equal_split_adds_up_and_differs_by_at_most_one_minor_unit()
    {
        Gen.Select(Expenses, Gen.Int[1, 30]).Sample((expense, people) =>
        {
            var (currency, amount) = expense;
            var shares = Split(amount, currency, SplitMethod.Equal, Participants(people));
            var oneMinorUnit = currency.FromMinorUnits(1);

            return AddsUpExactly(shares, amount, currency)
                && shares.Max(s => s.Amount) - shares.Min(s => s.Amount) <= oneMinorUnit;
        }, iter: Iterations);
    }

    [Fact]
    public void Shares_split_adds_up()
    {
        Gen.Select(Expenses, ShareCounts).Sample((expense, shareCounts) =>
        {
            var (currency, amount) = expense;
            var shares = Split(amount, currency, SplitMethod.Shares, Participants(shareCounts));

            return AddsUpExactly(shares, amount, currency);
        }, iter: Iterations);
    }

    [Fact]
    public void Percentage_split_adds_up_and_stays_within_one_minor_unit_of_the_exact_share()
    {
        Gen.Select(Expenses, Percentages).Sample((expense, percentages) =>
        {
            var (currency, amount) = expense;
            var shares = Split(amount, currency, SplitMethod.Percentage, Participants(percentages));
            var oneMinorUnit = currency.FromMinorUnits(1);

            return AddsUpExactly(shares, amount, currency)
                && shares.Select((s, i) => Math.Abs(s.Amount - (amount * percentages[i] / 100m)))
                    .All(difference => difference < oneMinorUnit);
        }, iter: Iterations);
    }

    [Fact]
    public void Exact_split_returns_the_amounts_it_was_given()
    {
        var euro = Currency.FromCode("EUR");

        Gen.Long[1, 10_000_000].Array[1, 20].Sample(minorAmounts =>
        {
            var amounts = minorAmounts.Select(euro.FromMinorUnits).ToArray();
            var shares = Split(amounts.Sum(), euro, SplitMethod.Exact, Participants(amounts));

            return shares.Select(s => s.Amount).SequenceEqual(amounts);
        }, iter: Iterations);
    }

    [Fact]
    public void Shares_are_returned_in_participant_order()
    {
        Gen.Select(Expenses, ShareCounts).Sample((expense, shareCounts) =>
        {
            var participants = Participants(shareCounts);
            var shares = Split(expense.Amount, expense.Currency, SplitMethod.Shares, participants);

            return shares.Select(s => s.UserId).SequenceEqual(participants.Select(p => p.UserId));
        }, iter: Iterations);
    }

    private static IReadOnlyList<SplitShare> Split(
        decimal amount, Currency currency, SplitMethod method, IReadOnlyList<SplitInput> participants)
    {
        var result = SplitCalculator.Calculate(amount, currency, method, participants);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error);
    }

    private static bool AddsUpExactly(IReadOnlyList<SplitShare> shares, decimal amount, Currency currency) =>
        shares.Sum(s => s.Amount) == amount
        && shares.All(s => s.Amount >= 0 && currency.TryToMinorUnits(s.Amount, out _));

    private static SplitInput[] Participants(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new SplitInput(PersonId(i)))];

    private static SplitInput[] Participants(decimal[] values) =>
        [.. values.Select((value, i) => new SplitInput(PersonId(i), value))];

    // Deterministic ids keep failing samples reproducible from their seed.
    private static Guid PersonId(int index) => new(index + 1, 0, 0, new byte[8]);
}
