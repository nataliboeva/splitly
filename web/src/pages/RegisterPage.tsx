import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router';
import { z } from 'zod';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/authContext';
import { AuthLayout } from '../components/AuthLayout';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { TextField } from '../components/ui/Field';

// Mirrors the Identity password rules configured in the API.
const schema = z
  .object({
    displayName: z.string().trim().min(1, 'Tell your friends who you are.').max(50),
    email: z.email('Enter a valid email address.'),
    password: z
      .string()
      .min(8, 'Use at least 8 characters.')
      .regex(/[a-z]/, 'Add a lowercase letter.')
      .regex(/[A-Z]/, 'Add an uppercase letter.')
      .regex(/\d/, 'Add a number.')
      .regex(/[^a-zA-Z\d]/, 'Add a symbol, like ! or #.'),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'The passwords do not match.',
  });

type RegisterValues = z.infer<typeof schema>;

function describeRegistrationError(error: unknown): string {
  if (!(error instanceof ApiError)) return 'We could not create your account. Please try again.';

  const codes = Object.keys(error.problem?.errors ?? {});
  if (codes.some((code) => code.startsWith('Duplicate'))) {
    return 'An account with this email already exists. Try signing in instead.';
  }
  return error.message;
}

export function RegisterPage() {
  const { register: createAccount } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
  });

  const onSubmit = handleSubmit(async ({ displayName, email, password }) => {
    setError(null);
    try {
      await createAccount(displayName, email, password);
      navigate('/groups', { replace: true });
    } catch (e) {
      setError(describeRegistrationError(e));
    }
  });

  return (
    <AuthLayout
      title="Create your account"
      subtitle="Split bills with friends without the awkward maths."
      footer={
        <>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-accent hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {error && <Alert>{error}</Alert>}
        <TextField
          label="Your name"
          autoComplete="name"
          error={errors.displayName?.message}
          {...register('displayName')}
        />
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
          autoComplete="new-password"
          hint="At least 8 characters with upper and lowercase letters, a number and a symbol."
          error={errors.password?.message}
          {...register('password')}
        />
        <TextField
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Button type="submit" isLoading={isSubmitting} className="mt-2">
          Create account
        </Button>
      </form>
    </AuthLayout>
  );
}
