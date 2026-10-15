using Splitly.Domain.Groups;
using static Splitly.Domain.Tests.TestData;

namespace Splitly.Domain.Tests.Groups;

public class GroupTests
{
    private readonly Guid _creator = Guid.NewGuid();

    [Fact]
    public void Creator_becomes_the_first_member()
    {
        var group = Group.Create("  Ski trip ", Currency.FromCode("EUR"), _creator, Now);

        group.Name.ShouldBe("Ski trip");
        group.Members.ShouldHaveSingleItem().UserId.ShouldBe(_creator);
        group.IsMember(_creator).ShouldBeTrue();
    }

    [Fact]
    public void Members_can_be_added()
    {
        var group = Group.Create("Flat", Currency.FromCode("EUR"), _creator, Now);
        var friend = Guid.NewGuid();

        var result = group.AddMember(friend, Now);

        result.IsSuccess.ShouldBeTrue();
        group.IsMember(friend).ShouldBeTrue();
        group.Members.Count.ShouldBe(2);
    }

    [Fact]
    public void The_same_person_cannot_be_added_twice()
    {
        var group = Group.Create("Flat", Currency.FromCode("EUR"), _creator, Now);

        var result = group.AddMember(_creator, Now);

        result.Error.ShouldBe("This person is already a member of the group.");
        group.Members.Count.ShouldBe(1);
    }

    [Fact]
    public void Name_is_required()
    {
        Should.Throw<ArgumentException>(() => Group.Create(" ", Currency.FromCode("EUR"), _creator, Now));
    }
}
