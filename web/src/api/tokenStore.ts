export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
}

type Listener = (tokens: StoredTokens | null) => void;

const STORAGE_KEY = 'splitly.tokens';
const listeners = new Set<Listener>();

/**
 * Keeps the bearer tokens in localStorage so a page refresh doesn't sign the user out.
 * Listeners are told when the tokens change, e.g. when a refresh fails and the user is signed out.
 */
export const tokenStore = {
  get(): StoredTokens | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as StoredTokens) : null;
    } catch {
      return null;
    }
  },

  set(tokens: StoredTokens | null) {
    if (tokens) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(tokens));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
    listeners.forEach((listener) => listener(tokens));
  },

  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};
