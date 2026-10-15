import { Link } from 'react-router';
import { LogoMark } from '../components/Logo';

export function NotFoundPage() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center px-4 text-center">
      <LogoMark className="size-10" />
      <p className="mt-8 font-mono text-sm text-accent">404</p>
      <h1 className="mt-2 text-2xl font-semibold tracking-tight">This page doesn&apos;t exist</h1>
      <p className="mt-2 text-sm text-muted">The link may be broken or the page may have moved.</p>
      <Link to="/groups" className="mt-6 text-sm font-medium text-accent hover:underline">
        Go to your groups
      </Link>
    </main>
  );
}
