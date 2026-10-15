using Splitly.Domain.Splits;

namespace Splitly.Domain.Tests.Splits;

public class SplitCalculatorTests
{
    private static readonly Currency Euro = Currency.FromCode("EUR");
    private static readonly Currency Yen = Currency.FromCode("JPY");

    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Boris = Guid.NewGuid();
    private static readonly Guid Vera = Guid.NewGuid();

    [Fact]
    public void Equal_split_gives_the_leftover_cent_to_the_first_person()
    {
        var result = SplitCalculator.Calculate(100m, Euro, SplitMethod.Equal, [new(Ana), new(Boris), new(Vera)]);

        AmountsOf(result).ShouldBe([33.34m, 33.33m, 33.33m]);
    }

    [Fact]
    public void Equal_split_respects_currencies_without_minor_units()
    {
        var result = SplitCalculator.Calculate(1000m, Yen, SplitMethod.Equal, [new(Ana), new(Boris), new(Vera)]);

        AmountsOf(result).ShouldBe([334m, 333m, 333m]);
    }

    [Fact]
    public void Equal_split_ignores_any_values_sent_along()
    {
        var result = SplitCalculator.Calculate(10m, Euro, SplitMethod.Equal, [new(Ana, 99m), new(Boris, -1m)]);

        AmountsOf(result).ShouldBe([5m, 5m]);
    }

    [Fact]
    public void Shares_split_is_proportional()
    {
        var result = SplitCalculator.Calculate(90m, Euro, SplitMethod.Shares, [new(Ana, 2m), new(Boris, 1m)]);

        AmountsOf(result).ShouldBe([60m, 30m]);
    }

    [Fact]
    public void Shares_can_be_fractional()
    {
        var result = SplitCalculator.Calculate(100m, Euro, SplitMethod.Shares, [new(Ana, 1.5m), new(Boris, 1m)]);

        AmountsOf(result).ShouldBe([60m, 40m]);
    }

    [Fact]
    public void Percentage_split_rounds_to_the_largest_remainder()
    {
        var result = SplitCalculator.Calculate(
            10m, Euro, SplitMethod.Percentage, [new(Ana, 33.33m), new(Boris, 33.33m), new(Vera, 33.34m)]);

        AmountsOf(result).ShouldBe([3.33m, 3.33m, 3.34m]);
    }

    [Fact]
    public void Percentages_must_add_up_to_100()
    {
        var result = SplitCalculator.Calculate(80m, Euro, SplitMethod.Percentage, [new(Ana, 50m), new(Boris, 40m)]);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Percentages must add up to 100% (they add up to 90%).");
    }

    [Fact]
    public void Exact_split_keeps_the_entered_amounts()
    {
        var result = SplitCalculator.Calculate(50m, Euro, SplitMethod.Exact, [new(Ana, 20.5m), new(Boris, 29.5m)]);

        AmountsOf(result).ShouldBe([20.5m, 29.5m]);
    }

    [Fact]
    public void Exact_amounts_must_add_up_to_the_total()
    {
        var result = SplitCalculator.Calculate(50m, Euro, SplitMethod.Exact, [new(Ana, 20m), new(Boris, 20m)]);

        result.Error.ShouldBe("The amounts must add up to 50.00 EUR (they add up to 40.00 EUR).");
    }

    [Fact]
    public void Exact_amounts_cannot_be_more_precise_than_the_currency()
    {
        var result = SplitCalculator.Calculate(10m, Euro, SplitMethod.Exact, [new(Ana, 5.005m), new(Boris, 4.995m)]);

        result.Error.ShouldBe("EUR amounts can have at most 2 decimal places.");
    }

    [Theory]
    [InlineData(SplitMethod.Exact)]
    [InlineData(SplitMethod.Percentage)]
    [InlineData(SplitMethod.Shares)]
    public void Methods_other_than_equal_need_a_value_for_everyone(SplitMethod method)
    {
        var result = SplitCalculator.Calculate(10m, Euro, method, [new(Ana, 100m), new(Boris)]);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData(SplitMethod.Percentage)]
    [InlineData(SplitMethod.Shares)]
    public void Weights_cannot_have_more_than_two_decimals(SplitMethod method)
    {
        var result = SplitCalculator.Calculate(10m, Euro, method, [new(Ana, 33.333m), new(Boris, 66.667m)]);

        result.Error.ShouldBe("Percentages and shares can have at most two decimal places.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Amount_must_be_positive(int amount)
    {
        var result = SplitCalculator.Calculate(amount, Euro, SplitMethod.Equal, [new(Ana)]);

        result.Error.ShouldBe("The amount must be greater than zero.");
    }

    [Fact]
    public void Amount_cannot_be_more_precise_than_the_currency()
    {
        SplitCalculator.Calculate(10.5m, Yen, SplitMethod.Equal, [new(Ana)])
            .Error.ShouldBe("JPY amounts can have at most 0 decimal places.");
    }

    [Fact]
    public void Someone_has_to_take_part()
    {
        SplitCalculator.Calculate(10m, Euro, SplitMethod.Equal, [])
            .Error.ShouldBe("Choose at least one person to split with.");
    }

    [Fact]
    public void A_person_can_only_appear_once()
    {
        SplitCalculator.Calculate(10m, Euro, SplitMethod.Equal, [new(Ana), new(Ana)])
            .Error.ShouldBe("Each person can only appear once in a split.");
    }

    private static decimal[] AmountsOf(Result<IReadOnlyList<SplitShare>> result)
    {
        result.IsSuccess.ShouldBeTrue(result.Error);
        return [.. result.Value.Select(s => s.Amount)];
    }
}
