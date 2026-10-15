import { Navigate, Outlet, useLocation } from 'react-router';
import { FullPageSpinner } from '../components/ui/Spinner';
import { useAuth } from './authContext';

/** Renders the child routes for signed-in users and sends everyone else to the login page. */
export function RequireAuth() {
  const { user, isRestoring } = useAuth();
  const location = useLocation();

  if (isRestoring) return <FullPageSpinner />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;

  return <Outlet />;
}

/** Keeps signed-in users away from the login and register pages. */
export function GuestOnly() {
  const { user, isRestoring } = useAuth();

  if (isRestoring) return <FullPageSpinner />;
  if (user) return <Navigate to="/groups" replace />;

  return <Outlet />;
}
