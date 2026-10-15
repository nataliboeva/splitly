import { Link, Outlet } from 'react-router';
import { useAuth } from '../auth/authContext';
import { Logo } from './Logo';
import { Avatar } from './ui/Avatar';
import { Button } from './ui/Button';
import { LogOutIcon } from './ui/icons';

export function AppLayout() {
  const { user, signOut } = useAuth();

  return (
    <div className="min-h-dvh">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:rounded-lg focus:bg-surface focus:px-3 focus:py-2 focus:shadow-card"
      >
        Skip to content
      </a>

      <header className="sticky top-0 z-20 border-b border-border bg-bg/80 backdrop-blur-md">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
          <Link to="/groups" className="rounded-lg" aria-label="Splitly, go to your groups">
            <Logo />
          </Link>

          {user && (
            <div className="flex items-center gap-2 sm:gap-3">
              <div className="flex items-center gap-2.5">
                <Avatar id={user.id} name={user.displayName} size="sm" />
                <span className="hidden text-sm font-medium sm:inline">{user.displayName}</span>
              </div>
              <Button variant="ghost" size="sm" onClick={signOut} aria-label="Sign out">
                <LogOutIcon className="size-4" />
                <span className="hidden sm:inline">Sign out</span>
              </Button>
            </div>
          )}
        </div>
      </header>

      <main id="main" className="mx-auto max-w-6xl px-4 py-8 sm:px-6 sm:py-10">
        <Outlet />
      </main>
    </div>
  );
}
