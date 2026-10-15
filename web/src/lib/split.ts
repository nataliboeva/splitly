import type { Currency, SplitMethod } from '../api/types';
import { formatMoney, fromMinorUnits, parseMinorUnits } from './money';

/**
 * Splits `total` minor units in proportion to `weights` with the largest remainder method, so the
 * parts always add up to the total. Same algorithm as the API's Allocator: round everything down,
 * then hand the leftover units to the largest remainders (ties go to the earlier person).
 * BigInt keeps total * weight exact even for large amounts.
 */
export function allocate(total: number, weights: number[]): number[] {
  const weightSum = weights.reduce((sum, w) => sum + BigInt(w), 0n);
  const exact = weights.map((w) => BigInt(total) * BigInt(w));
  const parts = exact.map((value) => value / weightSum);
  const remainders = exact.map((value) => value % weightSum);

  const leftover = total - Number(parts.reduce((sum, part) => sum + part, 0n));
  const order = weights
    .map((_, i) => i)
    .sort((a, b) =>
      remainders[b] === remainders[a] ? a - b : remainders[b] > remainders[a] ? 1 : -1,
    );

  for (const i of order.slice(0, leftover)) parts[i] += 1n;

  return parts.map(Number);
}

export interface SplitParticipant {
  userId: string;
  /** Raw input: an amount, a percentage or a share count depending on the method. */
  value: string;
}

export type SplitPreview =
  | { ok: true; shares: Map<string, number> }
  | { ok: false; error: string; shares: Map<string, number> };

/**
 * Previews how an expense will be split while the user is still typing. The API does the real
 * calculation; this mirrors it so people can see the result (and any problem) before saving.
 */
export function previewSplit(
  method: SplitMethod,
  amount: string,
  participants: SplitParticipant[],
  currency: Currency,
): SplitPreview {
  const total = parseMinorUnits(amount, currency.minorUnits);
  const fail = (error: string): SplitPreview => ({ ok: false, error, shares: new Map() });
  const succeed = (parts: number[]): SplitPreview => ({
    ok: true,
    shares: new Map(
      participants.map((p, i) => [p.userId, fromMinorUnits(parts[i], currency.minorUnits)]),
    ),
  });

  if (total === null || total === 0) return fail('Enter the amount first.');
  if (participants.length === 0) return fail('Choose at least one person to split with.');

  switch (method) {
    case 'equal':
      return succeed(
        allocate(
          total,
          participants.map(() => 1),
        ),
      );

    case 'shares':
    case 'percentage': {
      // Shares and percentages may have two decimals, so work in hundredths.
      const weights = participants.map((p) => parseMinorUnits(p.value, 2));
      if (weights.some((w) => w === null || w === 0)) {
        return fail(
          method === 'shares'
            ? 'Give everyone in the split at least some shares.'
            : 'Give everyone in the split a percentage.',
        );
      }

      const validWeights = weights as number[];
      const weightTotal = validWeights.reduce((sum, w) => sum + w, 0);
      if (method === 'percentage' && weightTotal !== 10_000) {
        return fail(`Percentages must add up to 100% (they add up to ${weightTotal / 100}%).`);
      }

      return succeed(allocate(total, validWeights));
    }

    case 'exact': {
      const amounts = participants.map((p) => parseMinorUnits(p.value, currency.minorUnits));
      if (amounts.some((a) => a === null || a === 0)) {
        return fail('Enter an amount for everyone in the split.');
      }

      const validAmounts = amounts as number[];
      const difference = total - validAmounts.reduce((sum, a) => sum + a, 0);
      if (difference !== 0) {
        const remaining = formatMoney(
          fromMinorUnits(Math.abs(difference), currency.minorUnits),
          currency.code,
        );
        return fail(
          difference > 0 ? `${remaining} still to assign.` : `${remaining} too much assigned.`,
        );
      }

      return succeed(validAmounts);
    }
  }
}
