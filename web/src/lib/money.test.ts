import { describe, expect, it } from 'vitest';
import { formatMoney, parseMinorUnits } from './money';

describe('parseMinorUnits', () => {
  it.each([
    ['12.34', 2, 1234],
    ['12,3', 2, 1230],
    ['12', 2, 1200],
    [' 7.5 ', 2, 750],
    ['1500', 0, 1500],
  ])('parses %j into minor units', (input, minorUnits, expected) => {
    expect(parseMinorUnits(input, minorUnits)).toBe(expected);
  });

  it.each([['12.345'], ['-5'], ['abc'], [''], ['1.2.3']])('rejects %j', (input) => {
    expect(parseMinorUnits(input, 2)).toBeNull();
  });

  it('rejects decimals for currencies without minor units', () => {
    expect(parseMinorUnits('10.5', 0)).toBeNull();
  });
});

describe('formatMoney', () => {
  it('formats with the currency symbol and its usual number of decimals', () => {
    expect(formatMoney(1234.5, 'EUR')).toBe('€1,234.50');
    expect(formatMoney(1500, 'JPY')).toBe('JP¥1,500');
  });
});
