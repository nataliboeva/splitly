import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCurrencies, useRecordSettlement } from '../../api/queries';
import type { Transfer } from '../../api/types';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { TextField } from '../../components/ui/Field';
import { ArrowRightIcon } from '../../components/ui/icons';
import { todayIso } from '../../lib/dates';
import { fromMinorUnits, parseMinorUnits, toInputValue } from '../../lib/money';
import type { NameLookup } from '../groups/people';

interface RecordPaymentDialogProps {
  transfer: Transfer | null;
  groupId: string;
  currency: string;
  nameOf: NameLookup;
  onClose: () => void;
}

export function RecordPaymentDialog({ transfer, ...props }: RecordPaymentDialogProps) {
  return (
    <Dialog
      open={transfer !== null}
      onClose={props.onClose}
      title="Record a payment"
      description="Paid someone back outside the app? Record it here to update the balances."
    >
      {transfer && <RecordPaymentForm transfer={transfer} {...props} />}
    </Dialog>
  );
}

function RecordPaymentForm({
  transfer,
  groupId,
  currency,
  nameOf,
  onClose,
}: Omit<RecordPaymentDialogProps, 'transfer'> & { transfer: Transfer }) {
  const minorUnits = useCurrencies().data?.find((c) => c.code === currency)?.minorUnits ?? 2;
  const record = useRecordSettlement(groupId);

  const schema = z.object({
    amount: z
      .string()
      .refine((v) => (parseMinorUnits(v, minorUnits) ?? 0) > 0, 'Enter the amount that was paid.'),
    date: z.string().min(1, 'Pick a date.'),
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: { amount: toInputValue(transfer.amount, minorUnits), date: todayIso() },
  });

  const onSubmit = handleSubmit(({ amount, date }) =>
    record.mutate(
      {
        fromUserId: transfer.fromUserId,
        toUserId: transfer.toUserId,
        amount: fromMinorUnits(parseMinorUnits(amount, minorUnits)!, minorUnits),
        date,
      },
      { onSuccess: onClose },
    ),
  );

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      {record.error && <Alert>{record.error.message}</Alert>}
      <div className="flex items-center justify-center gap-3 rounded-xl bg-surface-muted px-4 py-3 text-sm font-medium">
        <span className="truncate">{nameOf(transfer.fromUserId)}</span>
        <ArrowRightIcon className="size-4 shrink-0 text-accent" />
        <span className="truncate">{nameOf(transfer.toUserId)}</span>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <TextField
          label={`Amount (${currency})`}
          inputMode="decimal"
          className="[&_input]:font-mono"
          error={errors.amount?.message}
          {...register('amount')}
        />
        <TextField label="Date" type="date" error={errors.date?.message} {...register('date')} />
      </div>
      <div className="mt-1 flex justify-end gap-2">
        <Button variant="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button type="submit" isLoading={record.isPending}>
          Record payment
        </Button>
      </div>
    </form>
  );
}
