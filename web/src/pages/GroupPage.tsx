import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { ApiError } from '../api/client';
import { useBalances, useExpenses, useGroup } from '../api/queries';
import type { Expense, Group, Transfer } from '../api/types';
import { useAuth } from '../auth/authContext';
import { Alert } from '../components/ui/Alert';
import { Avatar } from '../components/ui/Avatar';
import { Button } from '../components/ui/Button';
import { Card, EmptyState, Skeleton } from '../components/ui/Card';
import { ArrowLeftIcon, PlusIcon, ReceiptIcon, UsersIcon } from '../components/ui/icons';
import { Tabs } from '../components/ui/Tabs';
import { ActivityFeed } from '../features/activity/ActivityFeed';
import { BalancesCard } from '../features/balances/BalancesCard';
import { RecordPaymentDialog } from '../features/balances/RecordPaymentDialog';
import { DeleteExpenseDialog, ExpenseDialog } from '../features/expenses/ExpenseDialogs';
import { ExpenseList } from '../features/expenses/ExpenseList';
import { MembersCard } from '../features/groups/MembersCard';
import { createNameLookup } from '../features/groups/people';
import { cn } from '../lib/cn';
import { formatMoney } from '../lib/money';

type TabId = 'expenses' | 'activity';

export function GroupPage() {
  const { groupId = '' } = useParams();
  const group = useGroup(groupId);

  if (group.isPending) return <GroupPageSkeleton />;
  if (group.isError) return <GroupError error={group.error} />;

  return <GroupView key={group.data.id} group={group.data} />;
}

function GroupView({ group }: { group: Group }) {
  const { user } = useAuth();
  const currentUserId = user!.id;
  const expenses = useExpenses(group.id);
  const balances = useBalances(group.id);

  const [tab, setTab] = useState<TabId>('expenses');
  const [editor, setEditor] = useState<{ expense?: Expense } | null>(null);
  const [expenseToDelete, setExpenseToDelete] = useState<Expense | null>(null);
  const [payment, setPayment] = useState<Transfer | null>(null);

  const nameOf = createNameLookup(group.members, currentUserId);
  const myBalance = balances.data?.balances.find((b) => b.userId === currentUserId)?.amount;
  const addExpense = () => setEditor({});

  return (
    <>
      <Link
        to="/groups"
        className="inline-flex items-center gap-1.5 rounded-lg text-sm text-muted transition-colors hover:text-fg"
      >
        <ArrowLeftIcon className="size-4" />
        All groups
      </Link>

      <header className="mt-4 mb-8 flex flex-wrap items-end justify-between gap-4">
        <div className="min-w-0">
          <h1 className="truncate text-2xl font-semibold tracking-tight sm:text-3xl">
            {group.name}
          </h1>
          <div className="mt-2 flex flex-wrap items-center gap-3 text-sm text-muted">
            <div className="flex -space-x-2">
              {group.members.slice(0, 5).map((m) => (
                <Avatar
                  key={m.userId}
                  id={m.userId}
                  name={m.displayName}
                  size="sm"
                  className="ring-2 ring-bg"
                />
              ))}
            </div>
            <span>
              {group.members.length} {group.members.length === 1 ? 'member' : 'members'} ·{' '}
              <span className="font-mono">{group.currency}</span>
            </span>
            {myBalance !== undefined && (
              <MyBalancePill amount={myBalance} currency={group.currency} />
            )}
          </div>
        </div>
        <Button onClick={addExpense}>
          <PlusIcon className="size-4" />
          Add expense
        </Button>
      </header>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <Card className="overflow-hidden">
          <Tabs
            label="Group details"
            selected={tab}
            onSelect={setTab}
            tabs={[
              { id: 'expenses', label: 'Expenses' },
              { id: 'activity', label: 'Activity' },
            ]}
          >
            {tab === 'expenses' ? (
              <ExpensesPanel
                expenses={expenses}
                group={group}
                currentUserId={currentUserId}
                nameOf={nameOf}
                onAdd={addExpense}
                onEdit={(expense) => setEditor({ expense })}
                onDelete={setExpenseToDelete}
              />
            ) : (
              <ActivityFeed groupId={group.id} currentUserId={currentUserId} />
            )}
          </Tabs>
        </Card>

        <aside className="space-y-6">
          <BalancesCard
            balances={balances.data}
            currentUserId={currentUserId}
            nameOf={nameOf}
            onRecordPayment={setPayment}
          />
          <MembersCard group={group} nameOf={nameOf} />
        </aside>
      </div>

      <ExpenseDialog
        open={editor !== null}
        onClose={() => setEditor(null)}
        group={group}
        currentUserId={currentUserId}
        expense={editor?.expense}
      />
      <DeleteExpenseDialog
        expense={expenseToDelete}
        groupId={group.id}
        currency={group.currency}
        onClose={() => setExpenseToDelete(null)}
      />
      <RecordPaymentDialog
        transfer={payment}
        groupId={group.id}
        currency={group.currency}
        nameOf={nameOf}
        onClose={() => setPayment(null)}
      />
    </>
  );
}

function ExpensesPanel({
  expenses,
  group,
  onAdd,
  ...listProps
}: {
  expenses: ReturnType<typeof useExpenses>;
  group: Group;
  currentUserId: string;
  nameOf: ReturnType<typeof createNameLookup>;
  onAdd: () => void;
  onEdit: (expense: Expense) => void;
  onDelete: (expense: Expense) => void;
}) {
  if (expenses.isPending) {
    return (
      <div className="space-y-3 p-5" aria-busy="true" aria-label="Loading expenses">
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-14" />
        ))}
      </div>
    );
  }

  if (expenses.isError) return <Alert className="m-5">{expenses.error.message}</Alert>;

  if (expenses.data.length === 0) {
    return (
      <EmptyState
        icon={<ReceiptIcon className="size-6" />}
        title="No expenses yet"
        description={
          group.members.length > 1
            ? 'Add the first one and Splitly will work out who owes what.'
            : 'Add the people you share costs with, then add your first expense.'
        }
        action={
          <Button onClick={onAdd}>
            <PlusIcon className="size-4" />
            Add expense
          </Button>
        }
      />
    );
  }

  return <ExpenseList expenses={expenses.data} currency={group.currency} {...listProps} />;
}

function MyBalancePill({ amount, currency }: { amount: number; currency: string }) {
  const text =
    amount > 0
      ? `You get back ${formatMoney(amount, currency)}`
      : amount < 0
        ? `You owe ${formatMoney(-amount, currency)}`
        : 'You are settled up';

  return (
    <span
      className={cn(
        'rounded-full px-2.5 py-0.5 text-xs font-medium',
        amount > 0 && 'bg-positive/10 text-positive',
        amount < 0 && 'bg-negative/10 text-negative',
        amount === 0 && 'bg-surface-muted text-muted',
      )}
    >
      {text}
    </span>
  );
}

function GroupError({ error }: { error: Error }) {
  const notFound = error instanceof ApiError && error.status === 404;
  return (
    <Card>
      <EmptyState
        icon={<UsersIcon className="size-6" />}
        title={notFound ? 'Group not found' : 'Could not load this group'}
        description={notFound ? 'It may not exist, or you are not a member of it.' : error.message}
        action={
          <Link to="/groups" className="text-sm font-medium text-accent hover:underline">
            Back to your groups
          </Link>
        }
      />
    </Card>
  );
}

function GroupPageSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading group">
      <Skeleton className="h-4 w-24" />
      <Skeleton className="mt-5 h-8 w-64" />
      <Skeleton className="mt-3 h-5 w-48" />
      <div className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <Skeleton className="h-96 rounded-2xl" />
        <Skeleton className="h-72 rounded-2xl" />
      </div>
    </div>
  );
}
