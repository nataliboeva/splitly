import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/tokenStore';
import { renderWithProviders } from '../test/renderWithProviders';
import { LoginPage } from './LoginPage';

function jsonResponse(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

async function fillInAndSubmit(email: string, password: string) {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Email'), email);
  await user.type(screen.getByLabelText('Password'), password);
  await user.click(screen.getByRole('button', { name: 'Sign in' }));
}

describe('LoginPage', () => {
  it('validates the form before calling the API', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch');
    renderWithProviders(<LoginPage />, { path: '/login' });

    await fillInAndSubmit('not-an-email', 'x');

    expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('shows a friendly message when the credentials are wrong', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(401, { title: 'Unauthorized', detail: 'Failed', status: 401 }),
    );
    renderWithProviders(<LoginPage />, { path: '/login' });

    await fillInAndSubmit('ana@example.com', 'wrong-password');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'That email and password combination is not right.',
    );
    expect(tokenStore.get()).toBeNull();
  });

  it('stores the tokens and goes to the groups page after signing in', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) =>
      String(input) === '/api/auth/login'
        ? jsonResponse(200, { accessToken: 'access', refreshToken: 'refresh', expiresIn: 3600 })
        : jsonResponse(200, { id: 'ana', email: 'ana@example.com', displayName: 'Ana' }),
    );
    renderWithProviders(<LoginPage />, { path: '/login' });

    await fillInAndSubmit('ana@example.com', 'Passw0rd!');

    expect(await screen.findByText('Groups page')).toBeInTheDocument();
    expect(tokenStore.get()).toEqual({ accessToken: 'access', refreshToken: 'refresh' });
  });
});
