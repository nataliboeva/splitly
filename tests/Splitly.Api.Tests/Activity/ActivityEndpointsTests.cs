using System.Net;
using System.Net.Http.Json;
using Splitly.Api.Features.Activity;
using Splitly.Api.Features.Expenses;
using Splitly.Api.Features.Settlements;
using Splitly.Api.Tests.Infrastructure;
using Splitly.Domain.Activity;
using Splitly.Domain.Splits;

namespace Splitly.Api.Tests.Activity;

public class ActivityEndpointsTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    [Fact]
    public async Task Activity_feed_lists_what_happened_newest_first()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var group = await CreateGroupAsync(ana, boris);

        var dinner = await ReadAsync<ExpenseResponse>(await ana.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/expenses",
            new ExpenseRequest("Dinner", 60m, Today, ana.Id, SplitMethod.Equal, [new(ana.Id), new(boris.Id)]),
            Cancellation));
        await boris.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/settlements", new RecordSettlementRequest(boris.Id, ana.Id, 30m, Today), Cancellation);
        await ana.Client.DeleteAsync($"/api/groups/{group.Id}/expenses/{dinner.Id}", Cancellation);

        var feed = await ReadAsync<List<ActivityResponse>>(
            await boris.Client.GetAsync($"/api/groups/{group.Id}/activity", Cancellation));

        feed.Select(a => (a.Type, a.ActorName, a.Message)).ShouldBe(
        [
            (ActivityType.ExpenseDeleted, "Ana", "deleted \"Dinner\" (60.00 EUR)"),
            (ActivityType.SettlementRecorded, "Boris", "recorded a payment of 30.00 EUR from Boris to Ana"),
            (ActivityType.ExpenseAdded, "Ana", "added \"Dinner\" (60.00 EUR)"),
            (ActivityType.MemberAdded, "Ana", "added Boris to the group"),
            (ActivityType.GroupCreated, "Ana", "created the group \"Trip\""),
        ]);
    }

    [Fact]
    public async Task Limit_caps_the_number_of_entries()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var group = await CreateGroupAsync(ana, boris);

        var feed = await ReadAsync<List<ActivityResponse>>(
            await ana.Client.GetAsync($"/api/groups/{group.Id}/activity?limit=1", Cancellation));

        feed.ShouldHaveSingleItem().Type.ShouldBe(ActivityType.MemberAdded);
    }

    [Fact]
    public async Task Outsiders_cannot_read_the_feed()
    {
        var ana = await CreateUserAsync("Ana");
        var outsider = await CreateUserAsync("Outsider");
        var group = await CreateGroupAsync(ana);

        var response = await outsider.Client.GetAsync($"/api/groups/{group.Id}/activity", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
