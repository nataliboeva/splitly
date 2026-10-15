import { describe, expect, it } from 'vitest';
import { allocate, previewSplit } from './split';

const euro = { code: 'EUR', minorUnits: 2 };
const people = (...values: string[]) => values.map((value, i) => ({ userId: `user-${i}`, value }));
const amounts = (preview: ReturnType<typeof previewSplit>) => [...preview.shares.values()];

describe('allocate', () => {
  it('gives the leftover cent to the first person when everyone is equal', () => {
    expect(allocate(100, [1, 1, 1])).toEqual([34, 33, 33]);
  });

  it('gives leftover cents to the largest remainders', () => {
    expect(allocate(100, [1, 2])).toEqual([33, 67]);
  });

  it('always adds up to the total', () => {
    // A cheap property check: a few thousand pseudo-random cases from a fixed seed
    // (Park-Miller generator), so a failure is always reproducible.
    let seed = 42;
    const random = (max: number) => {
      seed = (seed * 48_271) % 2_147_483_647;
      return seed % max;
    };

    for (let run = 0; run < 2_000; run++) {
      const total = random(10_000_000);
      const weights = Array.from({ length: 1 + random(12) }, () => 1 + random(10_000));
      const parts = allocate(total, weights);

      expect(parts.reduce((sum, part) => sum + part, 0)).toBe(total);
      expect(parts.every((part) => part >= 0)).toBe(true);
    }
  });
});

describe('previewSplit', () => {
  it('splits equally in cents', () => {
    const preview = previewSplit('equal', '100', people('', '', ''), euro);

    expect(preview.ok).toBe(true);
    expect(amounts(preview)).toEqual([33.34, 33.33, 33.33]);
  });

  it('respects currencies without decimals', () => {
    const preview = previewSplit('equal', '1000', people('', '', ''), {
      code: 'JPY',
      minorUnits: 0,
    });

    expect(amounts(preview)).toEqual([334, 333, 333]);
  });

  it('splits by shares', () => {
    expect(amounts(previewSplit('shares', '90', people('2', '1'), euro))).toEqual([60, 30]);
  });

  it('accepts a comma as the decimal separator', () => {
    expect(amounts(previewSplit('shares', '10,50', people('1', '1'), euro))).toEqual([5.25, 5.25]);
  });

  it('explains when percentages do not add up to 100', () => {
    const preview = previewSplit('percentage', '80', people('50', '40'), euro);

    expect(preview).toMatchObject({
      ok: false,
      error: 'Percentages must add up to 100% (they add up to 90%).',
    });
  });

  it('tells the user how much is left to assign for exact amounts', () => {
    const preview = previewSplit('exact', '50', people('20', '25.5'), euro);

    expect(preview).toMatchObject({ ok: false, error: '€4.50 still to assign.' });
  });

  it('needs an amount and at least one person', () => {
    expect(previewSplit('equal', '', people(''), euro).ok).toBe(false);
    expect(previewSplit('equal', '10', [], euro)).toMatchObject({
      error: 'Choose at least one person to split with.',
    });
  });
});
