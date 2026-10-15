import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { useAddMember } from '../../api/queries';
import type { Group } from '../../api/types';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Card, CardHeader } from '../../components/ui/Card';
import { Input } from '../../components/ui/Field';
import type { NameLookup } from './people';

const schema = z.object({ email: z.email('Enter a valid email address.') });

export function MembersCard({ group, nameOf }: { group: Group; nameOf: NameLookup }) {
  return (
    <Card>
      <CardHeader title={`Members · ${group.members.length}`} />
      <ul className="divide-y divide-border">
        {group.members.map((member) => (
          <li key={member.userId} className="flex items-center gap-3 px-5 py-3">
            <Avatar id={member.userId} name={member.displayName} size="sm" />
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">{nameOf(member.userId)}</p>
              <p className="truncate text-xs text-muted">{member.email}</p>
            </div>
          </li>
        ))}
      </ul>
      <AddMemberForm groupId={group.id} />
    </Card>
  );
}

function AddMemberForm({ groupId }: { groupId: string }) {
  const addMember = useAddMember(groupId);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema), defaultValues: { email: '' } });

  const onSubmit = handleSubmit(({ email }) =>
    addMember.mutate(email, { onSuccess: () => reset() }),
  );

  const error = errors.email?.message ?? addMember.error?.message;

  return (
    <form onSubmit={onSubmit} noValidate className="border-t border-border px-5 py-4">
      <label htmlFor="add-member-email" className="text-sm font-medium">
        Add someone
      </label>
      <p className="mt-0.5 text-xs text-muted">They need a Splitly account first.</p>
      <div className="mt-2.5 flex gap-2">
        <Input
          id="add-member-email"
          type="email"
          placeholder="friend@example.com"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? 'add-member-error' : undefined}
          {...register('email')}
        />
        <Button type="submit" variant="secondary" isLoading={addMember.isPending}>
          Add
        </Button>
      </div>
      {error && (
        <p id="add-member-error" className="mt-2 text-sm text-danger">
          {error}
        </p>
      )}
    </form>
  );
}
