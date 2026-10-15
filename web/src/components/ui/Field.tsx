import { useId, type ComponentProps, type ReactNode } from 'react';
import { cn } from '../../lib/cn';

const controlClass =
  'h-10 w-full rounded-xl border border-border bg-surface px-3 text-sm text-fg shadow-card transition-colors placeholder:text-muted/70 focus-visible:border-accent focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-accent/20 disabled:opacity-60 aria-invalid:border-danger';

export function Input({ className, ...props }: ComponentProps<'input'>) {
  return <input className={cn(controlClass, className)} {...props} />;
}

export function Select({ className, ...props }: ComponentProps<'select'>) {
  return <select className={cn(controlClass, 'pr-8', className)} {...props} />;
}

interface FieldProps {
  label: ReactNode;
  error?: string;
  hint?: ReactNode;
  className?: string;
  children: (control: {
    id: string;
    'aria-invalid'?: true;
    'aria-describedby'?: string;
  }) => ReactNode;
}

/** Label, control and message wired together for screen readers. */
export function Field({ label, error, hint, className, children }: FieldProps) {
  const id = useId();
  const messageId = `${id}-message`;
  const message = error ?? hint;

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {children({
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': message ? messageId : undefined,
      })}
      {message && (
        <p id={messageId} className={cn('text-sm', error ? 'text-danger' : 'text-muted')}>
          {message}
        </p>
      )}
    </div>
  );
}

interface TextFieldProps extends ComponentProps<'input'> {
  label: ReactNode;
  error?: string;
  hint?: ReactNode;
}

export function TextField({ label, error, hint, className, ...props }: TextFieldProps) {
  return (
    <Field label={label} error={error} hint={hint} className={className}>
      {(control) => <Input {...control} {...props} />}
    </Field>
  );
}
