import { cn } from '../lib/cn';

export function LogoMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={cn('size-8', className)} aria-hidden="true">
      <rect width="32" height="32" rx="9" className="fill-accent" />
      <path
        d="M11 9.5h10M11 22.5h10"
        className="stroke-accent-fg"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <path
        d="M21 9.5 11 22.5"
        className="stroke-accent-fg"
        strokeOpacity="0.7"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
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
