import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { AuthProvider } from '../auth/AuthProvider';

/**
 * Renders a page with the same providers as the real app. `/groups` renders a marker so tests can
 * check where the user ended up.
 */
export function renderWithProviders(page: ReactElement, { path = '/' }: { path?: string } = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const router = createMemoryRouter(
    [
      { path, element: page },
      { path: '/groups', element: <p>Groups page</p> },
    ],
    { initialEntries: [path] },
  );

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  );
}
