import { describe, expect, it, vi } from 'vitest';
import { ApiError, apiRequest } from './client';
import { tokenStore } from './tokenStore';

function jsonResponse(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('apiRequest', () => {
  it('sends the access token', async () => {
    tokenStore.set({ accessToken: 'access-1', refreshToken: 'refresh-1' });
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(200, { ok: 1 }));

    await expect(apiRequest('/api/groups')).resolves.toEqual({ ok: 1 });

    const headers = fetchMock.mock.calls[0][1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer access-1');
  });

  it('refreshes an expired token once and retries the request', async () => {
    tokenStore.set({ accessToken: 'expired', refreshToken: 'refresh-1' });
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(401))
      .mockResolvedValueOnce(
        jsonResponse(200, { accessToken: 'access-2', refreshToken: 'refresh-2', expiresIn: 3600 }),
      )
      .mockResolvedValueOnce(jsonResponse(200, ['group']));

    await expect(apiRequest('/api/groups')).resolves.toEqual(['group']);

    expect(fetchMock.mock.calls[1][0]).toBe('/api/auth/refresh');
    expect(tokenStore.get()).toEqual({ accessToken: 'access-2', refreshToken: 'refresh-2' });
    const retryHeaders = fetchMock.mock.calls[2][1]?.headers as Record<string, string>;
    expect(retryHeaders.Authorization).toBe('Bearer access-2');
  });

  it('signs the user out when the refresh token is rejected too', async () => {
    tokenStore.set({ accessToken: 'expired', refreshToken: 'revoked' });
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(401))
      .mockResolvedValueOnce(jsonResponse(401));

    await expect(apiRequest('/api/groups')).rejects.toMatchObject({ status: 401 });
    expect(tokenStore.get()).toBeNull();
  });

  it('turns validation problems into a readable error', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(400, {
        title: 'One or more validation errors occurred.',
        errors: { Description: ["'Description' must not be empty."] },
      }),
    );

    const error = await apiRequest('/api/groups/1/expenses', { method: 'POST', body: {} }).catch(
      (e: unknown) => e,
    );

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toBe("'Description' must not be empty.");
  });

  it('prefers the problem detail for business rule errors', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(400, {
        title: 'The request could not be completed.',
        detail: 'Percentages must add up to 100% (they add up to 90%).',
      }),
    );

    await expect(apiRequest('/api/groups/1/expenses')).rejects.toThrow(
      'Percentages must add up to 100% (they add up to 90%).',
    );
  });
});
