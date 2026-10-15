using System.Globalization;
using Splitly.Domain.Settlements;
using static Splitly.Domain.Tests.TestData;

namespace Splitly.Domain.Tests.Settlements;

public class SettlementTests
{
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _boris = Guid.NewGuid();

    [Fact]
    public void A_payment_between_members_is_recorded()
    {
        var group = GroupWith(_ana, _boris);

        var result = Settlement.Create(group, _boris, _ana, 25.5m, Today, _boris, Now);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FromUserId.ShouldBe(_boris);
        result.Value.ToUserId.ShouldBe(_ana);
        result.Value.Amount.ShouldBe(25.5m);
    }

    [Fact]
    public void Nobody_can_pay_themselves()
    {
        var group = GroupWith(_ana, _boris);

        Settlement.Create(group, _ana, _ana, 10m, Today, _ana, Now).Error
            .ShouldBe("A payment needs two different people.");
    }

    [Fact]
    public void Both_people_must_be_members()
    {
        var group = GroupWith(_ana, _boris);

        Settlement.Create(group, _ana, Guid.NewGuid(), 10m, Today, _ana, Now).Error
            .ShouldBe("Both people must be members of the group.");
    }

    [Theory]
    [InlineData("EUR", "0", "The amount must be greater than zero.")]
    [InlineData("EUR", "1.001", "EUR amounts can have at most 2 decimal places.")]
    [InlineData("JPY", "1.5", "JPY amounts can have at most 0 decimal places.")]
    public void Amount_must_be_valid_for_the_currency(string currency, string amount, string error)
    {
        var group = GroupWith(currency, _ana, _boris);
        var value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        Settlement.Create(group, _ana, _boris, value, Today, _ana, Now).Error.ShouldBe(error);
    }
}
