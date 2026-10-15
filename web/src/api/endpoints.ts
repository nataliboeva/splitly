import { apiRequest } from './client';
import type {
  ActivityEntry,
  Currency,
  Expense,
  ExpenseRequest,
  Group,
  GroupBalances,
  GroupMember,
  GroupSummary,
  SettlementRequest,
  TokenResponse,
  UserProfile,
} from './types';

const groupPath = (groupId: string) => `/api/groups/${groupId}`;

export const api = {
  register: (email: string, password: string) =>
    apiRequest<void>('/api/auth/register', {
      method: 'POST',
      body: { email, password },
      anonymous: true,
    }),

  login: (email: string, password: string) =>
    apiRequest<TokenResponse>('/api/auth/login', {
      method: 'POST',
      body: { email, password },
      anonymous: true,
    }),

  getProfile: () => apiRequest<UserProfile>('/api/users/me'),

  updateProfile: (displayName: string) =>
    apiRequest<UserProfile>('/api/users/me', { method: 'PUT', body: { displayName } }),

  getCurrencies: () => apiRequest<Currency[]>('/api/currencies', { anonymous: true }),

  getGroups: () => apiRequest<GroupSummary[]>('/api/groups'),

  getGroup: (groupId: string) => apiRequest<Group>(groupPath(groupId)),

  createGroup: (name: string, currency: string) =>
    apiRequest<Group>('/api/groups', { method: 'POST', body: { name, currency } }),

  addMember: (groupId: string, email: string) =>
    apiRequest<GroupMember>(`${groupPath(groupId)}/members`, { method: 'POST', body: { email } }),

  getExpenses: (groupId: string) => apiRequest<Expense[]>(`${groupPath(groupId)}/expenses`),

  createExpense: (groupId: string, request: ExpenseRequest) =>
    apiRequest<Expense>(`${groupPath(groupId)}/expenses`, { method: 'POST', body: request }),

  updateExpense: (groupId: string, expenseId: string, request: ExpenseRequest) =>
    apiRequest<Expense>(`${groupPath(groupId)}/expenses/${expenseId}`, {
      method: 'PUT',
      body: request,
    }),

  deleteExpense: (groupId: string, expenseId: string) =>
    apiRequest<void>(`${groupPath(groupId)}/expenses/${expenseId}`, { method: 'DELETE' }),

  getBalances: (groupId: string) => apiRequest<GroupBalances>(`${groupPath(groupId)}/balances`),

  recordSettlement: (groupId: string, request: SettlementRequest) =>
    apiRequest<void>(`${groupPath(groupId)}/settlements`, { method: 'POST', body: request }),

  getActivity: (groupId: string) => apiRequest<ActivityEntry[]>(`${groupPath(groupId)}/activity`),
};
