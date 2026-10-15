// The UI is in English; en-GB gives "€1,234.56" and day-month dates, which suits a European audience.
export const LOCALE = 'en-GB';

const formatters = new Map<string, Intl.NumberFormat>();

export function formatMoney(amount: number, currency: string): string {
  let formatter = formatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat(LOCALE, { style: 'currency', currency });
    formatters.set(currency, formatter);
  }
  return formatter.format(amount);
}

/**
 * Parses user input such as "12.5" or "12,50" into whole minor units (1250) without going through
 * floating point. Returns null for anything that isn't a valid non-negative amount with at most
 * `minorUnits` decimals.
 */
export function parseMinorUnits(input: string, minorUnits: number): number | null {
  const match = /^(\d+)(?:[.,](\d*))?$/.exec(input.trim());
  if (!match) return null;

  const [, whole, fraction = ''] = match;
  if (fraction.length > minorUnits) return null;

  const value = Number(whole + fraction.padEnd(minorUnits, '0'));
  return Number.isSafeInteger(value) ? value : null;
}

export function fromMinorUnits(minor: number, minorUnits: number): number {
  return minor / 10 ** minorUnits;
}

export function toInputValue(amount: number, minorUnits: number): string {
  return amount.toFixed(minorUnits);
}
