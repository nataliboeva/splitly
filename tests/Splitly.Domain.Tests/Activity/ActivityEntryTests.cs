using Splitly.Domain.Activity;
using Splitly.Domain.Expenses;
using Splitly.Domain.Settlements;
using Splitly.Domain.Splits;
using static Splitly.Domain.Tests.TestData;

namespace Splitly.Domain.Tests.Activity;

public class ActivityEntryTests
{
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _boris = Guid.NewGuid();

    [Fact]
    public void Expense_entries_snapshot_the_description_and_amount()
    {
        var group = GroupWith(_ana, _boris);
        var expense = Expense.Create(
            group, new ExpenseDetails(_ana, 60m, "Dinner", Today, SplitMethod.Equal, [new(_ana), new(_boris)]), _ana, Now)
            .Value!;

        var entry = ActivityEntry.ExpenseAdded(group, expense, _ana, Now);

        entry.Type.ShouldBe(ActivityType.ExpenseAdded);
        entry.ActorId.ShouldBe(_ana);
        entry.Message.ShouldBe("added \"Dinner\" (60.00 EUR)");
    }

    [Fact]
    public void Settlement_entries_name_both_people()
    {
        var group = GroupWith(_ana, _boris);
        var settlement = Settlement.Create(group, _boris, _ana, 20m, Today, _boris, Now).Value!;

        var entry = ActivityEntry.SettlementRecorded(group, settlement, "Boris", "Ana", _boris, Now);

        entry.Message.ShouldBe("recorded a payment of 20.00 EUR from Boris to Ana");
    }
}
