using Splitly.Domain.Expenses;
using Splitly.Domain.Splits;
using static Splitly.Domain.Tests.TestData;

namespace Splitly.Domain.Tests.Expenses;

public class ExpenseTests
{
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _boris = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();

    [Fact]
    public void Creating_an_expense_calculates_the_shares()
    {
        var group = GroupWith(_ana, _boris);
        var details = new ExpenseDetails(
            _ana, 10m, " Pizza ", Today, SplitMethod.Shares, [new(_ana, 3m), new(_boris, 1m)]);

        var result = Expense.Create(group, details, _ana, Now);

        result.IsSuccess.ShouldBeTrue();
        var expense = result.Value;
        expense.Description.ShouldBe("Pizza");
        expense.PaidById.ShouldBe(_ana);
        expense.Shares.Select(s => (s.UserId, s.Amount, s.Value)).ShouldBe([(_ana, 7.5m, 3m), (_boris, 2.5m, 1m)]);
    }

    [Fact]
    public void Equal_splits_do_not_store_entered_values()
    {
        var group = GroupWith(_ana, _boris);
        var details = new ExpenseDetails(_ana, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana, 5m), new(_boris)]);

        var expense = Expense.Create(group, details, _ana, Now).Value!;

        expense.Shares.ShouldAllBe(s => s.Value == null);
    }

    [Fact]
    public void Payer_must_be_a_member()
    {
        var group = GroupWith(_ana, _boris);
        var details = new ExpenseDetails(_stranger, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana)]);

        Expense.Create(group, details, _ana, Now).Error
            .ShouldBe("The person who paid must be a member of the group.");
    }

    [Fact]
    public void Everyone_in_the_split_must_be_a_member()
    {
        var group = GroupWith(_ana, _boris);
        var details = new ExpenseDetails(_ana, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana), new(_stranger)]);

        Expense.Create(group, details, _ana, Now).Error
            .ShouldBe("Everyone in the split must be a member of the group.");
    }

    [Fact]
    public void Split_errors_are_passed_through()
    {
        var group = GroupWith(_ana, _boris);
        var details = new ExpenseDetails(
            _ana, 10m, "Taxi", Today, SplitMethod.Percentage, [new(_ana, 50m), new(_boris, 10m)]);

        Expense.Create(group, details, _ana, Now).Error
            .ShouldBe("Percentages must add up to 100% (they add up to 60%).");
    }

    [Fact]
    public void Updating_replaces_the_details_and_shares()
    {
        var group = GroupWith(_ana, _boris);
        var expense = Expense.Create(
            group, new ExpenseDetails(_ana, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana), new(_boris)]), _ana, Now)
            .Value!;
        var later = Now.AddHours(1);

        var result = expense.Update(
            group,
            new ExpenseDetails(_boris, 30m, "Taxi home", Today, SplitMethod.Exact, [new(_ana, 30m)]),
            later);

        result.IsSuccess.ShouldBeTrue();
        expense.Amount.ShouldBe(30m);
        expense.PaidById.ShouldBe(_boris);
        expense.UpdatedAt.ShouldBe(later);
        expense.Shares.ShouldHaveSingleItem().Amount.ShouldBe(30m);
    }

    [Fact]
    public void A_failed_update_leaves_the_expense_unchanged()
    {
        var group = GroupWith(_ana, _boris);
        var expense = Expense.Create(
            group, new ExpenseDetails(_ana, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana), new(_boris)]), _ana, Now)
            .Value!;

        var result = expense.Update(
            group,
            new ExpenseDetails(_ana, 30m, "Taxi", Today, SplitMethod.Exact, [new(_ana, 10m)]),
            Now.AddHours(1));

        result.IsSuccess.ShouldBeFalse();
        expense.Amount.ShouldBe(10m);
        expense.Shares.Count.ShouldBe(2);
        expense.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Only_the_creator_can_change_an_expense()
    {
        var group = GroupWith(_ana, _boris);
        var expense = Expense.Create(
            group, new ExpenseDetails(_ana, 10m, "Taxi", Today, SplitMethod.Equal, [new(_ana)]), _ana, Now).Value!;

        expense.CanBeChangedBy(_ana).ShouldBeTrue();
        expense.CanBeChangedBy(_boris).ShouldBeFalse();
    }
}
