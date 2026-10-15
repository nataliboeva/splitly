import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import { ApiError } from './api/client';
import { AuthProvider } from './auth/AuthProvider';
import { GuestOnly, RequireAuth } from './auth/RouteGuards';
import { AppLayout } from './components/AppLayout';
import { FullPageSpinner } from './components/ui/Spinner';
import { GroupsPage } from './pages/GroupsPage';
import { LoginPage } from './pages/LoginPage';
import { NotFoundPage } from './pages/NotFoundPage';

// The group page (expense form, balances, activity) and registration are loaded on demand,
// which keeps the first download small for people who only sign in and glance at their groups.
const router = createBrowserRouter([
  {
    hydrateFallbackElement: <FullPageSpinner />,
    children: [
      {
        element: <GuestOnly />,
        children: [
          { path: '/login', element: <LoginPage /> },
          {
            path: '/register',
            lazy: async () => ({ Component: (await import('./pages/RegisterPage')).RegisterPage }),
          },
        ],
      },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <AppLayout />,
            children: [
              { path: '/', element: <Navigate to="/groups" replace /> },
              { path: '/groups', element: <GroupsPage /> },
              {
                path: '/groups/:groupId',
                lazy: async () => ({ Component: (await import('./pages/GroupPage')).GroupPage }),
              },
            ],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        // Retrying a 4xx won't change the answer; only retry network and server errors.
        retry: (failureCount, error) =>
          failureCount < 2 && !(error instanceof ApiError && error.status < 500),
      },
    },
  });
}

export function App() {
  const [queryClient] = useState(createQueryClient);

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>
  );
}
