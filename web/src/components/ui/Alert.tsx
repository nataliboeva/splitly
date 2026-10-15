import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';
import { AlertCircleIcon } from './icons';

export function Alert({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div
      role="alert"
      className={cn(
        'flex gap-2.5 rounded-xl border border-danger/25 bg-danger/8 px-3.5 py-3 text-sm text-danger',
        className,
      )}
    >
      <AlertCircleIcon className="mt-px size-4.5 shrink-0" />
      <div>{children}</div>
    </div>
  );
}
