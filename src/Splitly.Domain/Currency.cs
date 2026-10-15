using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Splitly.Domain;

/// <summary>
/// An ISO 4217 currency together with the number of digits it uses after the decimal point
/// (2 for EUR, 0 for JPY). Amounts are converted to whole minor units (cents) before any splitting.
/// </summary>
public sealed record Currency
{
    private static readonly Dictionary<string, Currency> SupportedByCode = new Currency[]
    {
        new("EUR", 2), new("USD", 2), new("GBP", 2), new("CHF", 2),
        new("PLN", 2), new("CZK", 2), new("HUF", 2), new("RON", 2),
        new("SEK", 2), new("NOK", 2), new("DKK", 2), new("TRY", 2),
        new("CAD", 2), new("AUD", 2), new("JPY", 0),
    }.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    public string Code { get; }

    /// <summary>Number of digits after the decimal point.</summary>
    public int MinorUnits { get; }

    public static IReadOnlyCollection<Currency> Supported => SupportedByCode.Values;

    public static bool TryFromCode(string? code, [NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        return code is not null && SupportedByCode.TryGetValue(code, out currency);
    }

    public static Currency FromCode(string code) =>
        TryFromCode(code, out var currency)
            ? currency
            : throw new ArgumentException($"Currency '{code}' is not supported.", nameof(code));

    /// <summary>
    /// Converts an amount such as 12.34 into minor units (1234). Fails when the amount has more
    /// decimal places than the currency allows, e.g. 12.345 EUR or 10.5 JPY.
    /// </summary>
    public bool TryToMinorUnits(decimal amount, out long minorUnits)
    {
        var scaled = amount * MinorUnitFactor;
        if (scaled != decimal.Truncate(scaled) || scaled > long.MaxValue || scaled < long.MinValue)
        {
            minorUnits = 0;
            return false;
        }

        minorUnits = decimal.ToInt64(scaled);
        return true;
    }

    public decimal FromMinorUnits(long minorUnits) => minorUnits / MinorUnitFactor;

    public string Format(decimal amount) =>
        $"{amount.ToString($"F{MinorUnits}", CultureInfo.InvariantCulture)} {Code}";

    public override string ToString() => Code;

    private decimal MinorUnitFactor => (decimal)Math.Pow(10, MinorUnits);
}
