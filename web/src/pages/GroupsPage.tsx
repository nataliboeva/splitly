import { useState } from 'react';
import { Link } from 'react-router';
import { useGroups } from '../api/queries';
import type { GroupSummary } from '../api/types';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card, EmptyState, Skeleton } from '../components/ui/Card';
import { ArrowRightIcon, PlusIcon, UsersIcon } from '../components/ui/icons';
import { CreateGroupDialog } from '../features/groups/CreateGroupDialog';
import { formatDate } from '../lib/dates';

export function GroupsPage() {
  const groups = useGroups();
  const [isCreating, setIsCreating] = useState(false);

  const newGroupButton = (
    <Button onClick={() => setIsCreating(true)}>
      <PlusIcon className="size-4" />
      New group
    </Button>
  );

  return (
    <>
      <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">Your groups</h1>
          <p className="mt-1.5 text-sm text-muted">
            Trips, flats and dinners: everything you share with other people.
          </p>
        </div>
        {groups.data && groups.data.length > 0 && newGroupButton}
      </div>

      {groups.isPending && <GroupGridSkeleton />}
      {groups.isError && <Alert>{groups.error.message}</Alert>}
      {groups.data?.length === 0 && (
        <Card>
          <EmptyState
            icon={<UsersIcon className="size-6" />}
            title="No groups yet"
            description="Create a group for your next trip or your flat, add your friends and start adding expenses."
            action={newGroupButton}
          />
        </Card>
      )}
      {groups.data && groups.data.length > 0 && (
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {groups.data.map((group) => (
            <li key={group.id}>
              <GroupCard group={group} />
            </li>
          ))}
        </ul>
      )}

      <CreateGroupDialog open={isCreating} onClose={() => setIsCreating(false)} />
    </>
  );
}

function GroupCard({ group }: { group: GroupSummary }) {
  return (
    <Link
      to={`/groups/${group.id}`}
      className="group block rounded-2xl border border-border bg-surface p-5 shadow-card transition hover:-translate-y-0.5 hover:border-accent/40 hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="grid size-10 place-items-center rounded-xl bg-accent-soft text-accent">
          <UsersIcon className="size-5" />
        </div>
        <span className="rounded-full border border-border px-2 py-0.5 font-mono text-xs text-muted">
          {group.currency}
        </span>
      </div>
      <h2 className="mt-4 truncate font-semibold tracking-tight">{group.name}</h2>
      <div className="mt-1 flex items-center justify-between text-sm text-muted">
        <span>
          {group.memberCount} {group.memberCount === 1 ? 'member' : 'members'} · since{' '}
          {formatDate(group.createdAt.slice(0, 10))}
        </span>
        <ArrowRightIcon className="size-4 opacity-0 transition group-hover:translate-x-0.5 group-hover:opacity-100" />
      </div>
    </Link>
  );
}

function GroupGridSkeleton() {
  return (
    <div
      className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
      aria-busy="true"
      aria-label="Loading groups"
    >
      {[0, 1, 2].map((i) => (
        <Card key={i} className="p-5">
          <Skeleton className="size-10 rounded-xl" />
          <Skeleton className="mt-4 h-5 w-2/3" />
          <Skeleton className="mt-2 h-4 w-1/2" />
        </Card>
      ))}
    </div>
  );
}
