using CsCheck;
using Splitly.Domain.Balances;
using Splitly.Domain.Expenses;
using Splitly.Domain.Groups;
using Splitly.Domain.Settlements;
using Splitly.Domain.Splits;
using static Splitly.Domain.Tests.TestData;

namespace Splitly.Domain.Tests.Balances;

public class BalanceCalculatorTests
{
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Boris = Guid.NewGuid();
    private static readonly Guid Vera = Guid.NewGuid();

    [Fact]
    public void Payer_is_owed_what_the_others_consumed()
    {
        var group = GroupWith(Ana, Boris, Vera);
        var dinner = EqualExpense(group, Ana, 90m, Ana, Boris, Vera);

        var balances = BalanceCalculator.Calculate([Ana, Boris, Vera], [dinner], []);

        balances[Ana].ShouldBe(60m);
        balances[Boris].ShouldBe(-30m);
        balances[Vera].ShouldBe(-30m);
    }

    [Fact]
    public void Settlements_reduce_the_debt()
    {
        var group = GroupWith(Ana, Boris, Vera);
        var dinner = EqualExpense(group, Ana, 90m, Ana, Boris, Vera);
        var payBack = Settlement.Create(group, Boris, Ana, 30m, Today, Boris, Now).Value!;

        var balances = BalanceCalculator.Calculate([Ana, Boris, Vera], [dinner], [payBack]);

        balances[Ana].ShouldBe(30m);
        balances[Boris].ShouldBe(0m);
        balances[Vera].ShouldBe(-30m);
    }

    [Fact]
    public void Members_without_expenses_have_a_zero_balance()
    {
        var balances = BalanceCalculator.Calculate([Ana, Boris], [], []);

        balances.Values.ShouldAllBe(b => b == 0m);
        balances.Count.ShouldBe(2);
    }

    [Fact]
    public void Balances_always_add_up_to_zero()
    {
        Guid[] members = [Ana, Boris, Vera, Guid.NewGuid()];
        var trip = GroupWith(members);

        var genExpense =
            from payer in Gen.OneOfConst(members)
            from cents in Gen.Long[1, 10_000_000]
            from participants in Gen.Shuffle(members, 1, members.Length)
            select EqualExpense(trip, payer, cents / 100m, participants);

        genExpense.Array[0, 30].Sample(expenses =>
            BalanceCalculator.Calculate(members, expenses, []).Values.Sum() == 0m);
    }

    private static Expense EqualExpense(Group group, Guid payer, decimal amount, params Guid[] participants)
    {
        SplitInput[] splits = [.. participants.Select(p => new SplitInput(p))];
        var details = new ExpenseDetails(payer, amount, "Expense", Today, SplitMethod.Equal, splits);

        return Expense.Create(group, details, payer, Now).Value!;
    }
}
