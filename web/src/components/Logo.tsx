import { cn } from '../lib/cn';

/** A coin split into two halves. */
export function LogoMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={cn('size-8', className)} aria-hidden="true">
      <rect width="32" height="32" rx="9" className="fill-accent" />
      <path d="M14.75 7.5a8.5 8.5 0 0 0 0 17z" className="fill-accent-fg" />
      <path d="M17.25 7.5a8.5 8.5 0 0 1 0 17z" className="fill-accent-fg" fillOpacity="0.55" />
    </svg>
  );
}

export function Logo() {
  return (
    <span className="inline-flex items-center gap-2.5">
      <LogoMark />
      <span className="text-lg font-semibold tracking-tight">Splitly</span>
    </span>
  );
}
