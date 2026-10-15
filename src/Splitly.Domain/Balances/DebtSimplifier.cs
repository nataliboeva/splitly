namespace Splitly.Domain.Balances;

public static class DebtSimplifier
{
    /// <summary>
    /// Suggests a short list of payments that brings every balance back to zero.
    /// </summary>
    /// <remarks>
    /// Greedy "minimum cash flow": the person who owes the most pays the person who is owed the most,
    /// as much as possible, until everyone is settled. Each payment settles at least one person, so
    /// n people with a non-zero balance need at most n - 1 payments. It doesn't always find the absolute
    /// minimum (that problem is NP-hard), but it is simple, fast and good enough for groups of friends.
    /// Ties are broken by user id so the same balances always give the same suggestions.
    /// </remarks>
    public static IReadOnlyList<Transfer> Simplify(IReadOnlyDictionary<Guid, decimal> balances)
    {
        if (balances.Values.Sum() != 0)
        {
            throw new ArgumentException("Balances must add up to zero.", nameof(balances));
        }

        var creditors = balances.Where(b => b.Value > 0).Select(b => new Party(b.Key, b.Value)).ToList();
        var debtors = balances.Where(b => b.Value < 0).Select(b => new Party(b.Key, -b.Value)).ToList();
        var transfers = new List<Transfer>();

        while (creditors.Count > 0 && debtors.Count > 0)
        {
            var creditor = Largest(creditors);
            var debtor = Largest(debtors);
            var amount = Math.Min(creditor.Amount, debtor.Amount);

            transfers.Add(new Transfer(debtor.UserId, creditor.UserId, amount));

            creditor.Amount -= amount;
            debtor.Amount -= amount;
            if (creditor.Amount == 0)
            {
                creditors.Remove(creditor);
            }

            if (debtor.Amount == 0)
            {
                debtors.Remove(debtor);
            }
        }

        return transfers;
    }

    private static Party Largest(List<Party> parties) =>
        parties.OrderByDescending(p => p.Amount).ThenBy(p => p.UserId).First();

    private sealed class Party(Guid userId, decimal amount)
    {
        public Guid UserId { get; } = userId;

        public decimal Amount { get; set; } = amount;
    }
}
