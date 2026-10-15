import { cn } from '../../lib/cn';
import { formatMoney } from '../../lib/money';

/** The signed-in user's position in a group at a glance. */
export function MyBalancePill({ amount, currency }: { amount: number; currency: string }) {
  const text =
    amount > 0
      ? `You get back ${formatMoney(amount, currency)}`
      : amount < 0
        ? `You owe ${formatMoney(-amount, currency)}`
        : 'You are settled up';

  return (
    <span
      className={cn(
        'inline-block rounded-full px-2.5 py-0.5 text-xs font-medium',
        amount > 0 && 'bg-positive/10 text-positive',
        amount < 0 && 'bg-negative/10 text-negative',
        amount === 0 && 'bg-surface-muted text-muted',
      )}
    >
      {text}
    </span>
  );
}
