import { zodResolver } from '@hookform/resolvers/zod';
import { useMemo } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';
import type { Currency, Expense, ExpenseRequest, GroupMember, SplitMethod } from '../../api/types';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Field, Input, Select, TextField } from '../../components/ui/Field';
import { AlertCircleIcon, CheckCircleIcon } from '../../components/ui/icons';
import { cn } from '../../lib/cn';
import { todayIso } from '../../lib/dates';
import { formatMoney, fromMinorUnits, parseMinorUnits, toInputValue } from '../../lib/money';
import { allocate, previewSplit } from '../../lib/split';

const splitMethods: Array<{ value: SplitMethod; label: string }> = [
  { value: 'equal', label: 'Equally' },
  { value: 'exact', label: 'Amounts' },
  { value: 'percentage', label: 'Percent' },
  { value: 'shares', label: 'Shares' },
];

function createSchema(currency: Currency) {
  return z.object({
    description: z
      .string()
      .trim()
      .min(1, 'What was it for?')
      .max(200, 'Keep it under 200 characters.'),
    amount: z
      .string()
      .refine(
        (value) => (parseMinorUnits(value, currency.minorUnits) ?? 0) > 0,
        currency.minorUnits === 0
          ? 'Enter a whole amount, e.g. 1500.'
          : 'Enter an amount, e.g. 12.50.',
      ),
    date: z.string().min(1, 'Pick a date.'),
    paidById: z.string().min(1, 'Who paid?'),
    splitMethod: z.enum(['equal', 'exact', 'percentage', 'shares']),
    participants: z.array(
      z.object({ userId: z.string(), included: z.boolean(), value: z.string() }),
    ),
  });
}

type ExpenseFormValues = z.infer<ReturnType<typeof createSchema>>;

function initialValues(
  members: GroupMember[],
  currentUserId: string,
  currency: Currency,
  expense?: Expense,
): ExpenseFormValues {
  if (!expense) {
    return {
      description: '',
      amount: '',
      date: todayIso(),
      paidById: currentUserId,
      splitMethod: 'equal',
      participants: members.map((m) => ({ userId: m.userId, included: true, value: '' })),
    };
  }

  const shares = new Map(expense.shares.map((s) => [s.userId, s]));
  return {
    description: expense.description,
    amount: toInputValue(expense.amount, currency.minorUnits),
    date: expense.date,
    paidById: expense.paidById,
    splitMethod: expense.splitMethod,
    participants: members.map((m) => {
      const share = shares.get(m.userId);
      return {
        userId: m.userId,
        included: share !== undefined,
        value: share?.value?.toString() ?? '',
      };
    }),
  };
}

export interface ExpenseFormProps {
  members: GroupMember[];
  currency: Currency;
  currentUserId: string;
  expense?: Expense;
  isSaving?: boolean;
  error?: string | null;
  onSubmit: (request: ExpenseRequest) => void;
  onCancel: () => void;
}

export function ExpenseForm({
  members,
  currency,
  currentUserId,
  expense,
  isSaving = false,
  error,
  onSubmit,
  onCancel,
}: ExpenseFormProps) {
  const schema = useMemo(() => createSchema(currency), [currency]);
  const {
    register,
    handleSubmit,
    control,
    getValues,
    setValue,
    formState: { errors },
  } = useForm<ExpenseFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues(members, currentUserId, currency, expense),
  });

  const [amount, splitMethod, participants] = useWatch({
    control,
    name: ['amount', 'splitMethod', 'participants'],
  });
  const included = participants.filter((p) => p.included);
  const preview = previewSplit(splitMethod, amount, included, currency);

  // Switching method pre-fills sensible values: an even percentage split or one share each.
  const prefillValues = (method: SplitMethod) => {
    const current = getValues('participants');
    const includedIndexes = current.flatMap((p, i) => (p.included ? [i] : []));
    const total = parseMinorUnits(getValues('amount'), currency.minorUnits);

    let values: string[] = [];
    if (method === 'percentage') {
      values = allocate(
        10_000,
        includedIndexes.map(() => 1),
      ).map((h) => (h / 100).toString());
    } else if (method === 'shares') {
      values = includedIndexes.map(() => '1');
    } else if (method === 'exact' && total) {
      values = allocate(
        total,
        includedIndexes.map(() => 1),
      ).map((m) => toInputValue(fromMinorUnits(m, currency.minorUnits), currency.minorUnits));
    }

    includedIndexes.forEach((index, i) => setValue(`participants.${index}.value`, values[i] ?? ''));
  };

  const submit = handleSubmit((values) => {
    const chosen = values.participants.filter((p) => p.included);
    if (!previewSplit(values.splitMethod, values.amount, chosen, currency).ok) return;

    onSubmit({
      description: values.description.trim(),
      amount: fromMinorUnits(
        parseMinorUnits(values.amount, currency.minorUnits)!,
        currency.minorUnits,
      ),
      date: values.date,
      paidById: values.paidById,
      splitMethod: values.splitMethod,
      splits: chosen.map((p) => ({
        userId: p.userId,
        value: values.splitMethod === 'equal' ? null : Number(p.value.replace(',', '.')),
      })),
    });
  });

  const memberName = (userId: string) =>
    userId === currentUserId
      ? 'You'
      : (members.find((m) => m.userId === userId)?.displayName ?? '');

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      {error && <Alert>{error}</Alert>}

      <TextField
        label="Description"
        placeholder="Dinner, tickets, groceries…"
        error={errors.description?.message}
        {...register('description')}
      />

      <div className="grid grid-cols-2 gap-3">
        <TextField
          label={`Amount (${currency.code})`}
          inputMode="decimal"
          placeholder={currency.minorUnits === 0 ? '0' : '0.00'}
          className="[&_input]:font-mono"
          error={errors.amount?.message}
          {...register('amount')}
        />
        <TextField label="Date" type="date" error={errors.date?.message} {...register('date')} />
      </div>

      <Field label="Paid by" error={errors.paidById?.message}>
        {(controlProps) => (
          <Select {...controlProps} {...register('paidById')}>
            {members.map((m) => (
              <option key={m.userId} value={m.userId}>
                {memberName(m.userId)}
              </option>
            ))}
          </Select>
        )}
      </Field>

      <fieldset>
        <legend className="text-sm font-medium">Split</legend>
        <div className="mt-1.5 grid grid-cols-4 gap-1 rounded-xl bg-surface-muted p-1">
          {splitMethods.map((method) => (
            <label key={method.value} className="relative">
              <input
                type="radio"
                value={method.value}
                className="peer sr-only"
                {...register('splitMethod', { onChange: (e) => prefillValues(e.target.value) })}
              />
              <span className="block cursor-pointer rounded-lg px-2 py-1.5 text-center text-sm font-medium text-muted transition peer-checked:bg-surface peer-checked:text-fg peer-checked:shadow-card peer-focus-visible:outline-2 peer-focus-visible:outline-accent hover:text-fg">
                {method.label}
              </span>
            </label>
          ))}
        </div>
      </fieldset>

      <ul className="divide-y divide-border rounded-xl border border-border">
        {participants.map((participant, index) => {
          const name = memberName(participant.userId);
          const share = preview.shares.get(participant.userId);
          return (
            <li key={participant.userId} className="flex items-center gap-2 px-3 py-2.5 sm:gap-3">
              <input
                type="checkbox"
                id={`include-${participant.userId}`}
                className="size-4 shrink-0 rounded accent-accent"
                {...register(`participants.${index}.included`)}
              />
              <label
                htmlFor={`include-${participant.userId}`}
                className={cn(
                  'flex min-w-0 flex-1 items-center gap-2.5 text-sm',
                  !participant.included && 'text-muted',
                )}
              >
                {/* Phones need the room for the name more than the avatar. */}
                <span className="hidden sm:block">
                  <Avatar
                    id={participant.userId}
                    name={members.find((m) => m.userId === participant.userId)?.displayName ?? name}
                    size="sm"
                  />
                </span>
                <span className="truncate">{name}</span>
              </label>
              {splitMethod !== 'equal' && (
                <div className="relative w-20 shrink-0 sm:w-24">
                  <Input
                    inputMode="decimal"
                    aria-label={`${name}: ${splitMethodValueLabel(splitMethod)}`}
                    disabled={!participant.included}
                    className="h-9 pr-7 text-right font-mono"
                    {...register(`participants.${index}.value`)}
                  />
                  <span className="pointer-events-none absolute inset-y-0 right-2.5 grid place-items-center text-xs text-muted">
                    {splitMethod === 'percentage' ? '%' : splitMethod === 'shares' ? '×' : ''}
                  </span>
                </div>
              )}
              <span className="w-18 shrink-0 text-right font-mono text-sm tabular-nums sm:w-20">
                {participant.included && share !== undefined ? (
                  formatMoney(share, currency.code)
                ) : (
                  <span className="text-muted">–</span>
                )}
              </span>
            </li>
          );
        })}
      </ul>

      <p
        role="status"
        className={cn(
          'flex items-center gap-2 text-sm',
          preview.ok ? 'text-positive' : 'text-negative',
        )}
      >
        {preview.ok ? (
          <>
            <CheckCircleIcon className="size-4" />
            Split between {included.length} {included.length === 1 ? 'person' : 'people'}, adds up
            exactly.
          </>
        ) : (
          <>
            <AlertCircleIcon className="size-4" />
            {preview.error}
          </>
        )}
      </p>

      <div className="mt-1 flex justify-end gap-2">
        <Button variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" isLoading={isSaving}>
          {expense ? 'Save changes' : 'Add expense'}
        </Button>
      </div>
    </form>
  );
}

function splitMethodValueLabel(method: SplitMethod): string {
  switch (method) {
    case 'exact':
      return 'amount';
    case 'percentage':
      return 'percentage';
    default:
      return 'shares';
  }
}
