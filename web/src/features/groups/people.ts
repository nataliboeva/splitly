import type { GroupMember } from '../../api/types';

/** Looks up member names, calling the signed-in user "You" the way people expect in a shared ledger. */
export function createNameLookup(members: GroupMember[], currentUserId: string) {
  const names = new Map(members.map((m) => [m.userId, m.displayName]));

  return (userId: string, { you = true }: { you?: boolean } = {}) => {
    if (you && userId === currentUserId) return 'You';
    return names.get(userId) ?? 'Former member';
  };
}

export type NameLookup = ReturnType<typeof createNameLookup>;
