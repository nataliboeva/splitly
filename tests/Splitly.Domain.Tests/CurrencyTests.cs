using Splitly.Domain;

namespace Splitly.Domain.Tests;

public class CurrencyTests
{
    [Theory]
    [InlineData("EUR", 2)]
    [InlineData("eur", 2)]
    [InlineData("JPY", 0)]
    public void Supported_codes_are_found_case_insensitively(string code, int minorUnits)
    {
        Currency.TryFromCode(code, out var currency).ShouldBeTrue();

        currency.Code.ShouldBe(code.ToUpperInvariant());
        currency.MinorUnits.ShouldBe(minorUnits);
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_codes_are_rejected(string? code)
    {
        Currency.TryFromCode(code, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("EUR", "12.34", 1234)]
    [InlineData("EUR", "12.3", 1230)]
    [InlineData("EUR", "12", 1200)]
    [InlineData("JPY", "1500", 1500)]
    public void Amounts_are_converted_to_minor_units(string code, string amount, long expected)
    {
        var currency = Currency.FromCode(code);

        currency.TryToMinorUnits(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), out var minor)
            .ShouldBeTrue();

        minor.ShouldBe(expected);
    }

    [Theory]
    [InlineData("EUR", "12.345")]
    [InlineData("JPY", "10.5")]
    public void Amounts_with_too_many_decimals_are_rejected(string code, string amount)
    {
        var currency = Currency.FromCode(code);

        currency.TryToMinorUnits(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), out _)
            .ShouldBeFalse();
    }

    [Fact]
    public void Minor_units_convert_back_to_the_same_amount()
    {
        var euro = Currency.FromCode("EUR");

        euro.FromMinorUnits(1234).ShouldBe(12.34m);
        euro.Format(12.5m).ShouldBe("12.50 EUR");
    }
}
