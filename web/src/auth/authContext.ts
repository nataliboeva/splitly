import { createContext, useContext } from 'react';
import type { UserProfile } from '../api/types';

export interface AuthContextValue {
  user: UserProfile | null;
  /** True while a stored session is being restored on page load. */
  isRestoring: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  register: (displayName: string, email: string, password: string) => Promise<void>;
  signOut: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return context;
}
