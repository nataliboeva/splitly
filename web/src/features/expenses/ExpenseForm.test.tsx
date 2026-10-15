import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { Expense } from '../../api/types';
import { ExpenseForm } from './ExpenseForm';

const members = [
  { userId: 'ana', displayName: 'Ana', email: 'ana@example.com' },
  { userId: 'boris', displayName: 'Boris', email: 'boris@example.com' },
  { userId: 'vera', displayName: 'Vera', email: 'vera@example.com' },
];
const euro = { code: 'EUR', minorUnits: 2 };

function renderForm(expense?: Expense) {
  const onSubmit = vi.fn();
  render(
    <ExpenseForm
      members={members}
      currency={euro}
      currentUserId="ana"
      expense={expense}
      onSubmit={onSubmit}
      onCancel={() => {}}
    />,
  );
  return { onSubmit, user: userEvent.setup() };
}

describe('ExpenseForm', () => {
  it('splits equally between everyone by default and previews the cents', async () => {
    const { onSubmit, user } = renderForm();

    await user.type(screen.getByLabelText('Description'), 'Dinner');
    await user.type(screen.getByLabelText('Amount (EUR)'), '100');

    expect(screen.getByText('€33.34')).toBeInTheDocument();
    expect(screen.getAllByText('€33.33')).toHaveLength(2);

    await user.click(screen.getByRole('button', { name: 'Add expense' }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          description: 'Dinner',
          amount: 100,
          paidById: 'ana',
          splitMethod: 'equal',
          splits: [
            { userId: 'ana', value: null },
            { userId: 'boris', value: null },
            { userId: 'vera', value: null },
          ],
        }),
      ),
    );
  });

  it('leaves out people who are unchecked', async () => {
    const { onSubmit, user } = renderForm();

    await user.type(screen.getByLabelText('Description'), 'Taxi');
    await user.type(screen.getByLabelText('Amount (EUR)'), '10');
    await user.click(screen.getByRole('checkbox', { name: 'Vera' }));
    await user.click(screen.getByRole('button', { name: 'Add expense' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0][0].splits.map((s: { userId: string }) => s.userId)).toEqual([
      'ana',
      'boris',
    ]);
  });

  it('pre-fills an even percentage split and blocks saving until it adds up to 100%', async () => {
    const { onSubmit, user } = renderForm();

    await user.type(screen.getByLabelText('Description'), 'Hotel');
    await user.type(screen.getByLabelText('Amount (EUR)'), '80');
    await user.click(screen.getByRole('radio', { name: 'Percent' }));

    const borisPercentage = screen.getByLabelText('Boris: percentage');
    expect(borisPercentage).toHaveValue('33.33');

    await user.clear(borisPercentage);
    await user.type(borisPercentage, '10');

    expect(screen.getByRole('status')).toHaveTextContent(
      'Percentages must add up to 100% (they add up to 76.67%).',
    );

    await user.click(screen.getByRole('button', { name: 'Add expense' }));
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('shows validation messages for missing fields', async () => {
    const { onSubmit, user } = renderForm();

    await user.click(screen.getByRole('button', { name: 'Add expense' }));

    expect(await screen.findByText('What was it for?')).toBeInTheDocument();
    expect(screen.getByText('Enter an amount, e.g. 12.50.')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('opens an existing expense with the values that were entered', () => {
    renderForm({
      id: 'expense-1',
      description: 'Groceries',
      amount: 95.4,
      date: '2026-10-01',
      paidById: 'boris',
      splitMethod: 'shares',
      createdById: 'ana',
      createdAt: '2026-10-01T10:00:00Z',
      updatedAt: null,
      shares: [
        { userId: 'ana', amount: 31.8, value: 1 },
        { userId: 'boris', amount: 63.6, value: 2 },
      ],
    });

    expect(screen.getByLabelText('Amount (EUR)')).toHaveValue('95.40');
    expect(screen.getByLabelText('Boris: shares')).toHaveValue('2');
    expect(screen.getByRole('checkbox', { name: 'Vera' })).not.toBeChecked();
    expect(screen.getByRole('button', { name: 'Save changes' })).toBeInTheDocument();
  });
});
