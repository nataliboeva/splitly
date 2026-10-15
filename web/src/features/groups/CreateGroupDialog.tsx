import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { z } from 'zod';
import { useCreateGroup, useCurrencies } from '../../api/queries';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { Field, Select, TextField } from '../../components/ui/Field';

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Give the group a name.')
    .max(100, 'Keep it under 100 characters.'),
  currency: z.string().length(3),
});

type CreateGroupValues = z.infer<typeof schema>;

// Bulgaria switched to the euro in 2026, so EUR is the sensible default.
const DEFAULT_CURRENCY = 'EUR';

export function CreateGroupDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="New group"
      description="A trip, a flat, a dinner club. You can add people right after."
    >
      <CreateGroupForm onCancel={onClose} />
    </Dialog>
  );
}

function CreateGroupForm({ onCancel }: { onCancel: () => void }) {
  const navigate = useNavigate();
  const currencies = useCurrencies();
  const createGroup = useCreateGroup();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateGroupValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', currency: DEFAULT_CURRENCY },
  });

  const onSubmit = handleSubmit((values) =>
    createGroup.mutate(values, { onSuccess: (group) => navigate(`/groups/${group.id}`) }),
  );

  const currencyCodes = currencies.data?.map((c) => c.code) ?? [DEFAULT_CURRENCY];

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      {createGroup.error && <Alert>{createGroup.error.message}</Alert>}
      <TextField
        label="Name"
        placeholder="Weekend in Bansko"
        autoFocus
        error={errors.name?.message}
        {...register('name')}
      />
      <Field label="Currency" hint="All expenses in the group use this currency.">
        {(control) => (
          <Select {...control} {...register('currency')}>
            {currencyCodes.map((code) => (
              <option key={code} value={code}>
                {code}
              </option>
            ))}
          </Select>
        )}
      </Field>
      <div className="mt-2 flex justify-end gap-2">
        <Button variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" isLoading={createGroup.isPending}>
          Create group
        </Button>
      </div>
    </form>
  );
}
