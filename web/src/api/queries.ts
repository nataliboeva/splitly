import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { api } from './endpoints';
import type { ExpenseRequest, SettlementRequest } from './types';

export const queryKeys = {
  profile: ['profile'] as const,
  currencies: ['currencies'] as const,
  groups: ['groups'] as const,
  group: (groupId: string) => ['groups', groupId] as const,
  expenses: (groupId: string) => ['groups', groupId, 'expenses'] as const,
  balances: (groupId: string) => ['groups', groupId, 'balances'] as const,
  activity: (groupId: string) => ['groups', groupId, 'activity'] as const,
};

// Every key of a group starts with ['groups', groupId], so this refreshes its details, expenses,
// balances and activity in one go after anything in the group changes.
const refreshGroup = (queryClient: QueryClient, groupId: string) =>
  queryClient.invalidateQueries({ queryKey: queryKeys.group(groupId) });

export function useCurrencies() {
  return useQuery({
    queryKey: queryKeys.currencies,
    queryFn: api.getCurrencies,
    staleTime: Infinity,
  });
}

export function useGroups() {
  return useQuery({ queryKey: queryKeys.groups, queryFn: api.getGroups });
}

export function useGroup(groupId: string) {
  return useQuery({ queryKey: queryKeys.group(groupId), queryFn: () => api.getGroup(groupId) });
}

export function useExpenses(groupId: string) {
  return useQuery({
    queryKey: queryKeys.expenses(groupId),
    queryFn: () => api.getExpenses(groupId),
  });
}

export function useBalances(groupId: string) {
  return useQuery({
    queryKey: queryKeys.balances(groupId),
    queryFn: () => api.getBalances(groupId),
  });
}

export function useActivity(groupId: string) {
  return useQuery({
    queryKey: queryKeys.activity(groupId),
    queryFn: () => api.getActivity(groupId),
  });
}

export function useCreateGroup() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ name, currency }: { name: string; currency: string }) =>
      api.createGroup(name, currency),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.groups }),
  });
}

export function useAddMember(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (email: string) => api.addMember(groupId, email),
    // Member counts on the group list change too, so refresh every group query.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.groups }),
  });
}

export function useSaveExpense(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ expenseId, request }: { expenseId?: string; request: ExpenseRequest }) =>
      expenseId
        ? api.updateExpense(groupId, expenseId, request)
        : api.createExpense(groupId, request),
    onSuccess: () => refreshGroup(queryClient, groupId),
  });
}

export function useDeleteExpense(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (expenseId: string) => api.deleteExpense(groupId, expenseId),
    onSuccess: () => refreshGroup(queryClient, groupId),
  });
}

export function useRecordSettlement(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: SettlementRequest) => api.recordSettlement(groupId, request),
    onSuccess: () => refreshGroup(queryClient, groupId),
  });
}
