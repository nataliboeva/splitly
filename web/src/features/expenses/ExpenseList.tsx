import type { Expense, SplitMethod } from '../../api/types';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { ChevronDownIcon, PencilIcon, TrashIcon } from '../../components/ui/icons';
import { cn } from '../../lib/cn';
import { dateParts } from '../../lib/dates';
import { formatMoney } from '../../lib/money';
import type { NameLookup } from '../groups/people';

const methodLabels: Record<SplitMethod, string> = {
  equal: 'Split equally',
  exact: 'Split by exact amounts',
  percentage: 'Split by percentage',
  shares: 'Split by shares',
};

interface ExpenseListProps {
  expenses: Expense[];
  currency: string;
  currentUserId: string;
  nameOf: NameLookup;
  onEdit: (expense: Expense) => void;
  onDelete: (expense: Expense) => void;
}

export function ExpenseList({
  expenses,
  currency,
  currentUserId,
  nameOf,
  onEdit,
  onDelete,
}: ExpenseListProps) {
  return (
    <ul className="divide-y divide-border">
      {expenses.map((expense) => (
        <li key={expense.id}>
          <details className="group/expense">
            <summary className="flex cursor-pointer list-none items-center gap-3 px-4 py-3.5 transition-colors hover:bg-surface-muted/60 sm:gap-4 sm:px-5 [&::-webkit-details-marker]:hidden">
              <DateBadge date={expense.date} />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium">{expense.description}</p>
                <p className="truncate text-sm text-muted">
                  {nameOf(expense.paidById)} paid{' '}
                  <span className="font-mono">{formatMoney(expense.amount, currency)}</span>
                </p>
              </div>
              <YourPosition expense={expense} currency={currency} currentUserId={currentUserId} />
              <ChevronDownIcon className="size-4 shrink-0 text-muted transition-transform group-open/expense:rotate-180" />
            </summary>

            <div className="px-4 pb-4 sm:px-5 sm:pl-[4.75rem]">
              <p className="mb-2 text-xs font-medium tracking-wide text-muted uppercase">
                {methodLabels[expense.splitMethod]}
              </p>
              <ul className="space-y-1.5">
                {expense.shares.map((share) => (
                  <li key={share.userId} className="flex items-center gap-2.5 text-sm">
                    <Avatar
                      id={share.userId}
                      name={nameOf(share.userId, { you: false })}
                      size="sm"
                    />
                    <span className="flex-1 truncate">
                      {nameOf(share.userId)}
                      {share.value !== null && expense.splitMethod !== 'exact' && (
                        <span className="text-muted">
                          {' '}
                          ·{' '}
                          {expense.splitMethod === 'percentage'
                            ? `${share.value}%`
                            : `${share.value}×`}
                        </span>
                      )}
                    </span>
                    <span className="font-mono tabular-nums">
                      {formatMoney(share.amount, currency)}
                    </span>
                  </li>
                ))}
              </ul>

              <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
                <p className="text-xs text-muted">
                  Added by {nameOf(expense.createdById)}
                  {expense.updatedAt && ' · edited'}
                </p>
                {expense.createdById === currentUserId && (
                  <div className="flex gap-1">
                    <Button variant="ghost" size="sm" onClick={() => onEdit(expense)}>
                      <PencilIcon className="size-4" />
                      Edit
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="hover:text-danger"
                      onClick={() => onDelete(expense)}
                    >
                      <TrashIcon className="size-4" />
                      Delete
                    </Button>
                  </div>
                )}
              </div>
            </div>
          </details>
        </li>
      ))}
    </ul>
  );
}

function DateBadge({ date }: { date: string }) {
  const { month, day } = dateParts(date);
  return (
    <div className="flex w-11 shrink-0 flex-col items-center rounded-xl border border-border bg-surface-muted py-1 leading-none">
      <span className="text-[10px] font-medium tracking-wide text-muted uppercase">{month}</span>
      <span className="mt-0.5 font-mono text-base font-semibold">{day}</span>
    </div>
  );
}

/** What this expense means for the signed-in user: did they lend or borrow money? */
function YourPosition({
  expense,
  currency,
  currentUserId,
}: {
  expense: Expense;
  currency: string;
  currentUserId: string;
}) {
  const myShare = expense.shares.find((s) => s.userId === currentUserId)?.amount ?? 0;
  const lent = expense.paidById === currentUserId ? expense.amount - myShare : 0;
  const borrowed = expense.paidById === currentUserId ? 0 : myShare;

  if (lent === 0 && borrowed === 0) {
    return <span className="hidden text-right text-xs text-muted sm:block">not involved</span>;
  }

  const isLent = lent > 0;
  return (
    <div className={cn('shrink-0 text-right', isLent ? 'text-positive' : 'text-negative')}>
      <p className="text-xs">{isLent ? 'you lent' : 'you borrowed'}</p>
      <p className="font-mono text-sm font-medium tabular-nums">
        {formatMoney(isLent ? lent : borrowed, currency)}
      </p>
    </div>
  );
}
