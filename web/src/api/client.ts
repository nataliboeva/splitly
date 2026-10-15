import { tokenStore } from './tokenStore';
import type { ProblemDetails, TokenResponse } from './types';

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | undefined;

  constructor(status: number, problem?: ProblemDetails) {
    super(describeProblem(status, problem));
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  /** Skip the bearer token, e.g. for login. */
  anonymous?: boolean;
}

let refreshInFlight: Promise<boolean> | null = null;

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await send(path, options);

  if (response.status === 401 && !options.anonymous && (await refreshTokens())) {
    response = await send(path, options);
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** Turns a ProblemDetails response into one readable sentence for the UI. */
export function describeProblem(status: number, problem?: ProblemDetails): string {
  const validationErrors = problem?.errors ? Object.values(problem.errors).flat() : [];
  if (validationErrors.length > 0) {
    return [...new Set(validationErrors)].join(' ');
  }

  if (problem?.detail) return problem.detail;
  if (problem?.title) return problem.title;

  return status >= 500
    ? 'Something went wrong on our side. Please try again.'
    : `The request failed with status ${status}.`;
}

function send(path: string, { method = 'GET', body, anonymous }: RequestOptions) {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const tokens = anonymous ? null : tokenStore.get();
  if (tokens) {
    headers.Authorization = `Bearer ${tokens.accessToken}`;
  }

  return fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

// Access tokens are short-lived. When one expires we trade the refresh token for a new pair once
// and retry; requests that fail at the same time share a single refresh call.
function refreshTokens(): Promise<boolean> {
  refreshInFlight ??= tryRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

async function tryRefresh(): Promise<boolean> {
  const current = tokenStore.get();
  if (!current) return false;

  try {
    const response = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: current.refreshToken }),
    });

    if (!response.ok) {
      tokenStore.set(null);
      return false;
    }

    const tokens = (await response.json()) as TokenResponse;
    tokenStore.set({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken });
    return true;
  } catch {
    return false;
  }
}
