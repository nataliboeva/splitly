import type { GroupBalances, Transfer } from '../../api/types';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Card, CardHeader, Skeleton } from '../../components/ui/Card';
import { CheckCircleIcon } from '../../components/ui/icons';
import { cn } from '../../lib/cn';
import { formatMoney } from '../../lib/money';
import type { NameLookup } from '../groups/people';

interface BalancesCardProps {
  balances: GroupBalances | undefined;
  currentUserId: string;
  nameOf: NameLookup;
  onRecordPayment: (transfer: Transfer) => void;
}

export function BalancesCard({
  balances,
  currentUserId,
  nameOf,
  onRecordPayment,
}: BalancesCardProps) {
  return (
    <Card>
      <CardHeader title="Balances" />
      {balances ? (
        <>
          <ul className="divide-y divide-border">
            {balances.balances.map(({ userId, amount }) => (
              <li key={userId} className="flex items-center gap-3 px-5 py-3">
                <Avatar id={userId} name={nameOf(userId, { you: false })} size="sm" />
                <span className="min-w-0 flex-1 truncate text-sm font-medium">
                  {nameOf(userId)}
                </span>
                <BalanceAmount
                  amount={amount}
                  currency={balances.currency}
                  isYou={userId === currentUserId}
                />
              </li>
            ))}
          </ul>
          <SettleUp
            transfers={balances.suggestedTransfers}
            currency={balances.currency}
            nameOf={nameOf}
            onRecordPayment={onRecordPayment}
          />
        </>
      ) : (
        <div className="space-y-3 px-5 py-4" aria-busy="true" aria-label="Loading balances">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-7" />
          ))}
        </div>
      )}
    </Card>
  );
}

export function BalanceAmount({
  amount,
  currency,
  isYou,
}: {
  amount: number;
  currency: string;
  isYou: boolean;
}) {
  if (amount === 0) {
    return <span className="text-sm text-muted">settled up</span>;
  }

  const owed = amount > 0;
  const label = owed ? (isYou ? 'get back' : 'gets back') : isYou ? 'owe' : 'owes';

  return (
    <span className={cn('text-right', owed ? 'text-positive' : 'text-negative')}>
      <span className="block text-xs">{label}</span>
      <span className="block font-mono text-sm font-medium tabular-nums">
        {formatMoney(Math.abs(amount), currency)}
      </span>
    </span>
  );
}

function SettleUp({
  transfers,
  currency,
  nameOf,
  onRecordPayment,
}: {
  transfers: Transfer[];
  currency: string;
  nameOf: NameLookup;
  onRecordPayment: (transfer: Transfer) => void;
}) {
  return (
    <div className="border-t border-border px-5 py-4">
      <h3 className="text-sm font-semibold tracking-tight">Settle up</h3>
      {transfers.length === 0 ? (
        <p className="mt-2 flex items-center gap-2 text-sm text-muted">
          <CheckCircleIcon className="size-4 text-positive" />
          Everyone is square.
        </p>
      ) : (
        <>
          <p className="mt-0.5 text-xs text-muted">
            {transfers.length === 1 ? 'One payment' : `${transfers.length} payments`} settle the
            whole group.
          </p>
          <ul className="mt-3 space-y-2">
            {transfers.map((transfer) => {
              const from = nameOf(transfer.fromUserId);
              return (
                <li
                  key={`${transfer.fromUserId}-${transfer.toUserId}`}
                  className="flex items-center gap-3 rounded-xl bg-surface-muted px-3 py-2.5"
                >
                  <div className="min-w-0 flex-1 text-sm">
                    <p className="truncate">
                      <span className="font-medium">{from}</span> {from === 'You' ? 'pay' : 'pays'}{' '}
                      <span className="font-medium">{nameOf(transfer.toUserId)}</span>
                    </p>
                    <p className="font-mono text-sm tabular-nums">
                      {formatMoney(transfer.amount, currency)}
                    </p>
                  </div>
                  <Button variant="secondary" size="sm" onClick={() => onRecordPayment(transfer)}>
                    Record
                  </Button>
                </li>
              );
            })}
          </ul>
        </>
      )}
    </div>
  );
}
