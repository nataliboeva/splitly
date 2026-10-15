using Splitly.Domain.Groups;

namespace Splitly.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now.Date);

    public static Group GroupWith(params Guid[] memberIds) => GroupWith("EUR", memberIds);

    public static Group GroupWith(string currencyCode, params Guid[] memberIds)
    {
        var group = Group.Create("Trip", Currency.FromCode(currencyCode), memberIds[0], Now);
        foreach (var memberId in memberIds.Skip(1))
        {
            group.AddMember(memberId, Now);
        }

        return group;
    }
}
