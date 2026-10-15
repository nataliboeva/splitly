import { useCurrencies, useDeleteExpense, useSaveExpense } from '../../api/queries';
import type { Currency, Expense, Group } from '../../api/types';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { formatMoney } from '../../lib/money';
import { ExpenseForm } from './ExpenseForm';

function useGroupCurrency(code: string): Currency {
  const currencies = useCurrencies();
  return currencies.data?.find((c) => c.code === code) ?? { code, minorUnits: 2 };
}

interface ExpenseDialogProps {
  open: boolean;
  onClose: () => void;
  group: Group;
  currentUserId: string;
  /** The expense to edit; leave out to add a new one. */
  expense?: Expense;
}

export function ExpenseDialog({
  open,
  onClose,
  group,
  currentUserId,
  expense,
}: ExpenseDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={expense ? 'Edit expense' : 'Add an expense'}
      description={`Everything in ${group.name} is in ${group.currency}.`}
    >
      <ExpenseDialogBody
        group={group}
        currentUserId={currentUserId}
        expense={expense}
        onClose={onClose}
      />
    </Dialog>
  );
}

function ExpenseDialogBody({
  group,
  currentUserId,
  expense,
  onClose,
}: Omit<ExpenseDialogProps, 'open'>) {
  const currency = useGroupCurrency(group.currency);
  const save = useSaveExpense(group.id);

  return (
    <ExpenseForm
      members={group.members}
      currency={currency}
      currentUserId={currentUserId}
      expense={expense}
      isSaving={save.isPending}
      error={save.error?.message}
      onCancel={onClose}
      onSubmit={(request) =>
        save.mutate({ expenseId: expense?.id, request }, { onSuccess: onClose })
      }
    />
  );
}

interface DeleteExpenseDialogProps {
  expense: Expense | null;
  groupId: string;
  currency: string;
  onClose: () => void;
}

export function DeleteExpenseDialog({
  expense,
  groupId,
  currency,
  onClose,
}: DeleteExpenseDialogProps) {
  const remove = useDeleteExpense(groupId);

  return (
    <Dialog open={expense !== null} onClose={onClose} title="Delete this expense?">
      {expense && (
        <div className="flex flex-col gap-5">
          {remove.error && <Alert>{remove.error.message}</Alert>}
          <p className="text-sm text-muted">
            <span className="font-medium text-fg">{expense.description}</span> (
            {formatMoney(expense.amount, currency)}) will be removed and everyone&apos;s balances
            recalculated. The activity feed keeps a record of it.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={onClose}>
              Keep it
            </Button>
            <Button
              variant="danger"
              isLoading={remove.isPending}
              onClick={() => remove.mutate(expense.id, { onSuccess: onClose })}
            >
              Delete expense
            </Button>
          </div>
        </div>
      )}
    </Dialog>
  );
}
