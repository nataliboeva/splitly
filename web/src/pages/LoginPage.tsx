import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate } from 'react-router';
import { z } from 'zod';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/authContext';
import { AuthLayout } from '../components/AuthLayout';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { TextField } from '../components/ui/Field';

const schema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
});

type LoginValues = z.infer<typeof schema>;

const demoAccount = { email: 'demo@example.com', password: 'Demo123!' };
const showDemoHint = import.meta.env.DEV || import.meta.env.VITE_SHOW_DEMO_ACCOUNT === 'true';

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  });

  const onSubmit = handleSubmit(async ({ email, password }) => {
    setError(null);
    try {
      await signIn(email, password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? '/groups', { replace: true });
    } catch (e) {
      setError(
        e instanceof ApiError && e.status === 401
          ? 'That email and password combination is not right.'
          : 'We could not sign you in. Please try again.',
      );
    }
  });

  return (
    <AuthLayout
      title="Welcome back"
      subtitle="Sign in to see who owes what."
      footer={
        <>
          New to Splitly?{' '}
          <Link to="/register" className="font-medium text-accent hover:underline">
            Create an account
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {error && <Alert>{error}</Alert>}
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          error={errors.email?.message}
          {...register('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Button type="submit" isLoading={isSubmitting} className="mt-2">
          Sign in
        </Button>
      </form>

      {showDemoHint && (
        <div className="mt-5 rounded-xl bg-accent-soft px-3.5 py-3 text-sm">
          <p className="font-medium">Just looking around?</p>
          <p className="mt-0.5 text-muted">
            Use the demo account <span className="font-mono text-fg">{demoAccount.email}</span>.
          </p>
          <button
            type="button"
            className="mt-2 font-medium text-accent hover:underline"
            onClick={() => {
              setValue('email', demoAccount.email);
              setValue('password', demoAccount.password);
            }}
          >
            Fill in demo credentials
          </button>
        </div>
      )}
    </AuthLayout>
  );
}
