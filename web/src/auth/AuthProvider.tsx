import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api } from '../api/endpoints';
import { queryKeys } from '../api/queries';
import { tokenStore } from '../api/tokenStore';
import { AuthContext, type AuthContextValue } from './authContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [hasTokens, setHasTokens] = useState(() => tokenStore.get() !== null);

  // The API client clears the tokens when a refresh fails; follow along so the UI signs out too.
  useEffect(() => tokenStore.subscribe((tokens) => setHasTokens(tokens !== null)), []);

  const profile = useQuery({
    queryKey: queryKeys.profile,
    queryFn: api.getProfile,
    enabled: hasTokens,
    staleTime: Infinity,
    retry: false,
  });

  const signIn = useCallback(
    async (email: string, password: string) => {
      const tokens = await api.login(email, password);
      tokenStore.set({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken });
      await queryClient.fetchQuery({ queryKey: queryKeys.profile, queryFn: api.getProfile });
    },
    [queryClient],
  );

  const register = useCallback(
    async (displayName: string, email: string, password: string) => {
      // Identity's register endpoint only takes an email and a password; the display name is
      // saved on the profile right after signing in.
      await api.register(email, password);
      await signIn(email, password);
      queryClient.setQueryData(queryKeys.profile, await api.updateProfile(displayName));
    },
    [queryClient, signIn],
  );

  const signOut = useCallback(() => {
    tokenStore.set(null);
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: hasTokens ? (profile.data ?? null) : null,
      isRestoring: hasTokens && profile.isPending,
      signIn,
      register,
      signOut,
    }),
    [hasTokens, profile.data, profile.isPending, signIn, register, signOut],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}
