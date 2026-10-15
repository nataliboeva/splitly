// Mirrors the API contracts in src/Splitly.Api/Features. Dates are ISO strings,
// money is a JSON number with at most the currency's number of decimals.

export type SplitMethod = 'equal' | 'exact' | 'percentage' | 'shares';

export type ActivityType =
  | 'groupCreated'
  | 'memberAdded'
  | 'expenseAdded'
  | 'expenseUpdated'
  | 'expenseDeleted'
  | 'settlementRecorded';

export interface TokenResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
}

export interface Currency {
  code: string;
  minorUnits: number;
}

export interface GroupSummary {
  id: string;
  name: string;
  currency: string;
  memberCount: number;
  createdAt: string;
}

export interface GroupMember {
  userId: string;
  displayName: string;
  email: string;
}

export interface Group {
  id: string;
  name: string;
  currency: string;
  createdAt: string;
  members: GroupMember[];
}

export interface ExpenseShare {
  userId: string;
  amount: number;
  /** The percentage, share count or exact amount that was entered. Null for equal splits. */
  value: number | null;
}

export interface Expense {
  id: string;
  description: string;
  amount: number;
  date: string;
  paidById: string;
  splitMethod: SplitMethod;
  createdById: string;
  createdAt: string;
  updatedAt: string | null;
  shares: ExpenseShare[];
}

export interface SplitRequest {
  userId: string;
  value: number | null;
}

export interface ExpenseRequest {
  description: string;
  amount: number;
  date: string;
  paidById: string;
  splitMethod: SplitMethod;
  splits: SplitRequest[];
}

export interface MemberBalance {
  userId: string;
  /** Positive: the group owes this member. Negative: the member owes the group. */
  amount: number;
}

export interface Transfer {
  fromUserId: string;
  toUserId: string;
  amount: number;
}

export interface GroupBalances {
  currency: string;
  balances: MemberBalance[];
  suggestedTransfers: Transfer[];
}

export interface SettlementRequest {
  fromUserId: string;
  toUserId: string;
  amount: number;
  date: string;
}

export interface ActivityEntry {
  id: string;
  type: ActivityType;
  actorId: string;
  actorName: string;
  message: string;
  createdAt: string;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
