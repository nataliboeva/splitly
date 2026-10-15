import type { ReactNode } from 'react';
import { useActivity } from '../../api/queries';
import type { ActivityType } from '../../api/types';
import { Alert } from '../../components/ui/Alert';
import { EmptyState, Skeleton } from '../../components/ui/Card';
import {
  ActivityIcon,
  HandCoinsIcon,
  PencilIcon,
  PlusIcon,
  ReceiptIcon,
  TrashIcon,
  UsersIcon,
} from '../../components/ui/icons';
import { timeAgo } from '../../lib/dates';

const icons: Record<ActivityType, ReactNode> = {
  groupCreated: <UsersIcon className="size-4" />,
  memberAdded: <PlusIcon className="size-4" />,
  expenseAdded: <ReceiptIcon className="size-4" />,
  expenseUpdated: <PencilIcon className="size-4" />,
  expenseDeleted: <TrashIcon className="size-4" />,
  settlementRecorded: <HandCoinsIcon className="size-4" />,
};

export function ActivityFeed({
  groupId,
  currentUserId,
}: {
  groupId: string;
  currentUserId: string;
}) {
  const activity = useActivity(groupId);

  if (activity.isPending) {
    return (
      <div className="space-y-4 p-5" aria-busy="true" aria-label="Loading activity">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="flex gap-3">
            <Skeleton className="size-8 rounded-full" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/4" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (activity.isError) {
    return <Alert className="m-5">{activity.error.message}</Alert>;
  }

  if (activity.data.length === 0) {
    return (
      <EmptyState
        icon={<ActivityIcon className="size-6" />}
        title="Nothing has happened yet"
        description="New expenses, members and payments will show up here."
      />
    );
  }

  return (
    <ol className="divide-y divide-border">
      {activity.data.map((entry) => (
        <li key={entry.id} className="flex gap-3 px-4 py-3.5 sm:px-5">
          <span className="grid size-8 shrink-0 place-items-center rounded-full bg-accent-soft text-accent">
            {icons[entry.type]}
          </span>
          <div className="min-w-0 text-sm">
            <p>
              <span className="font-medium">
                {entry.actorId === currentUserId ? 'You' : entry.actorName}
              </span>{' '}
              <span className="text-muted">{entry.message}</span>
            </p>
            <time dateTime={entry.createdAt} className="text-xs text-muted">
              {timeAgo(entry.createdAt)}
            </time>
          </div>
        </li>
      ))}
    </ol>
  );
}
