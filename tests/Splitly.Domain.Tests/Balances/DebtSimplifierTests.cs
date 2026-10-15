using CsCheck;
using Splitly.Domain.Balances;

namespace Splitly.Domain.Tests.Balances;

public class DebtSimplifierTests
{
    private static readonly Guid Ana = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Boris = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Vera = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Dimitar = new("00000000-0000-0000-0000-000000000004");

    // Between 1 and 15 people with random balances in cents; the last person absorbs the
    // difference so the group adds up to zero, like a real group always does.
    private static readonly Gen<Dictionary<Guid, decimal>> Balances =
        Gen.Long[-1_000_000, 1_000_000].Array[0, 14].Select(cents =>
        {
            long[] all = [.. cents, -cents.Sum()];
            return all
                .Select((value, i) => (Id: new Guid(i + 1, 0, 0, new byte[8]), Amount: value / 100m))
                .ToDictionary(x => x.Id, x => x.Amount);
        });

    [Fact]
    public void A_chain_of_debts_collapses_into_one_payment()
    {
        // Ana owes Boris 10 and Boris owes Vera 10, so Ana can pay Vera directly.
        var balances = new Dictionary<Guid, decimal> { [Ana] = -10m, [Boris] = 0m, [Vera] = 10m };

        DebtSimplifier.Simplify(balances).ShouldBe([new Transfer(Ana, Vera, 10m)]);
    }

    [Fact]
    public void The_biggest_debtor_pays_the_biggest_creditor_first()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Ana] = 50m,
            [Boris] = 10m,
            [Vera] = -40m,
            [Dimitar] = -20m,
        };

        DebtSimplifier.Simplify(balances).ShouldBe(
        [
            new Transfer(Vera, Ana, 40m),
            new Transfer(Dimitar, Ana, 10m),
            new Transfer(Dimitar, Boris, 10m),
        ]);
    }

    [Fact]
    public void Ties_are_broken_by_user_id()
    {
        var balances = new Dictionary<Guid, decimal> { [Vera] = 10m, [Boris] = 10m, [Ana] = -20m };

        DebtSimplifier.Simplify(balances).ShouldBe(
        [
            new Transfer(Ana, Boris, 10m),
            new Transfer(Ana, Vera, 10m),
        ]);
    }

    [Fact]
    public void Settled_groups_need_no_payments()
    {
        DebtSimplifier.Simplify(new Dictionary<Guid, decimal> { [Ana] = 0m, [Boris] = 0m }).ShouldBeEmpty();
        DebtSimplifier.Simplify(new Dictionary<Guid, decimal>()).ShouldBeEmpty();
    }

    [Fact]
    public void Balances_that_do_not_add_up_are_rejected()
    {
        var balances = new Dictionary<Guid, decimal> { [Ana] = 10m, [Boris] = -5m };

        Should.Throw<ArgumentException>(() => DebtSimplifier.Simplify(balances));
    }

    [Fact]
    public void Following_the_suggestions_settles_everyone()
    {
        Balances.Sample(balances =>
        {
            var remaining = new Dictionary<Guid, decimal>(balances);
            foreach (var transfer in DebtSimplifier.Simplify(balances))
            {
                remaining[transfer.FromUserId] += transfer.Amount;
                remaining[transfer.ToUserId] -= transfer.Amount;
            }

            return remaining.Values.All(b => b == 0m);
        }, iter: 2_000);
    }

    [Fact]
    public void Needs_at_most_one_payment_fewer_than_people_with_a_balance()
    {
        Balances.Sample(balances =>
        {
            var peopleWithBalance = balances.Values.Count(b => b != 0m);
            var transfers = DebtSimplifier.Simplify(balances);

            return transfers.Count <= Math.Max(0, peopleWithBalance - 1);
        }, iter: 2_000);
    }

    [Fact]
    public void Debtors_only_pay_and_creditors_only_receive()
    {
        Balances.Sample(balances =>
        {
            var transfers = DebtSimplifier.Simplify(balances);

            return transfers.All(t =>
                t.Amount > 0m && balances[t.FromUserId] < 0m && balances[t.ToUserId] > 0m);
        }, iter: 2_000);
    }
}
