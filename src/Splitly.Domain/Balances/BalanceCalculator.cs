using Splitly.Domain.Expenses;
using Splitly.Domain.Settlements;

namespace Splitly.Domain.Balances;

public static class BalanceCalculator
{
    /// <summary>
    /// Calculates each member's net balance: positive means the group owes them money,
    /// negative means they owe the group. The balances of a group always add up to zero.
    /// </summary>
    public static IReadOnlyDictionary<Guid, decimal> Calculate(
        IEnumerable<Guid> memberIds, IEnumerable<Expense> expenses, IEnumerable<Settlement> settlements)
    {
        var balances = memberIds.ToDictionary(id => id, _ => 0m);

        foreach (var expense in expenses)
        {
            Add(balances, expense.PaidById, expense.Amount);
            foreach (var share in expense.Shares)
            {
                Add(balances, share.UserId, -share.Amount);
            }
        }

        // Paying someone back lowers both what the payer owes and what the receiver is owed.
        foreach (var settlement in settlements)
        {
            Add(balances, settlement.FromUserId, settlement.Amount);
            Add(balances, settlement.ToUserId, -settlement.Amount);
        }

        return balances;
    }

    private static void Add(Dictionary<Guid, decimal> balances, Guid userId, decimal amount) =>
        balances[userId] = balances.GetValueOrDefault(userId) + amount;
}
