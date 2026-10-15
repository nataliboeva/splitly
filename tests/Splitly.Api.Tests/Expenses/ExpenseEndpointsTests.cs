using System.Net;
using System.Net.Http.Json;
using Splitly.Api.Features.Expenses;
using Splitly.Api.Tests.Infrastructure;
using Splitly.Domain.Splits;

namespace Splitly.Api.Tests.Expenses;

public class ExpenseEndpointsTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    [Fact]
    public async Task Equal_split_gives_the_leftover_cent_to_the_first_person()
    {
        var (ana, boris, vera, groupId) = await GroupOfThreeAsync();
        var request = Request(100m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris), Split(vera));

        var response = await ana.Client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", request, Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var expense = await ReadAsync<ExpenseResponse>(response);
        expense.Shares.ToDictionary(s => s.UserId, s => s.Amount).ShouldBe(new Dictionary<Guid, decimal>
        {
            [ana.Id] = 33.34m,
            [boris.Id] = 33.33m,
            [vera.Id] = 33.33m,
        }, ignoreOrder: true);
    }

    [Fact]
    public async Task Percentages_that_do_not_add_up_are_rejected_with_a_clear_message()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        var request = Request(80m, ana.Id, SplitMethod.Percentage, Split(ana, 50m), Split(boris, 40m));

        var response = await ana.Client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", request, Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadProblemAsync(response)).Detail.ShouldBe("Percentages must add up to 100% (they add up to 90%).");
    }

    [Fact]
    public async Task Only_group_members_can_be_part_of_an_expense()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        var outsider = await CreateUserAsync("Outsider");
        var request = Request(10m, ana.Id, SplitMethod.Equal, Split(ana), Split(outsider));

        var response = await ana.Client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", request, Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadProblemAsync(response)).Detail.ShouldBe("Everyone in the split must be a member of the group.");
    }

    [Fact]
    public async Task Malformed_requests_return_validation_errors()
    {
        var (ana, _, _, groupId) = await GroupOfThreeAsync();
        var request = new ExpenseRequest("", -5m, default, Guid.Empty, SplitMethod.Equal, []);

        var response = await ana.Client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", request, Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadValidationProblemAsync(response)).Errors.Keys
            .ShouldBe(["Description", "Amount", "Date", "PaidById", "Splits"], ignoreOrder: true);
    }

    [Fact]
    public async Task Expenses_are_listed_newest_first()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        await ana.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/expenses", Request(10m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris)) with
            {
                Description = "Older",
                Date = Today.AddDays(-3),
            }, Cancellation);
        await ana.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/expenses", Request(10m, ana.Id, SplitMethod.Equal, Split(ana)) with
            {
                Description = "Newer",
            }, Cancellation);

        var expenses = await ReadAsync<List<ExpenseResponse>>(
            await boris.Client.GetAsync($"/api/groups/{groupId}/expenses", Cancellation));

        expenses.Select(e => e.Description).ShouldBe(["Newer", "Older"]);
    }

    [Fact]
    public async Task The_creator_can_change_the_split()
    {
        var (ana, boris, vera, groupId) = await GroupOfThreeAsync();
        var created = await CreateExpenseAsync(ana, groupId, Request(90m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris), Split(vera)));
        var update = Request(90m, boris.Id, SplitMethod.Shares, Split(ana, 1m), Split(boris, 2m));

        var response = await ana.Client.PutAsJsonAsync(
            $"/api/groups/{groupId}/expenses/{created.Id}", update, Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = (await ReadAsync<List<ExpenseResponse>>(
            await ana.Client.GetAsync($"/api/groups/{groupId}/expenses", Cancellation))).ShouldHaveSingleItem();
        stored.PaidById.ShouldBe(boris.Id);
        stored.SplitMethod.ShouldBe(SplitMethod.Shares);
        stored.UpdatedAt.ShouldNotBeNull();
        stored.Shares.ToDictionary(s => s.UserId, s => (s.Amount, s.Value)).ShouldBe(new Dictionary<Guid, (decimal, decimal?)>
        {
            [ana.Id] = (30m, 1m),
            [boris.Id] = (60m, 2m),
        }, ignoreOrder: true);
    }

    [Fact]
    public async Task Other_members_cannot_change_or_delete_an_expense()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        var created = await CreateExpenseAsync(ana, groupId, Request(10m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris)));

        var update = await boris.Client.PutAsJsonAsync(
            $"/api/groups/{groupId}/expenses/{created.Id}",
            Request(1m, boris.Id, SplitMethod.Equal, Split(boris)),
            Cancellation);
        var delete = await boris.Client.DeleteAsync($"/api/groups/{groupId}/expenses/{created.Id}", Cancellation);

        update.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        delete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_creator_can_delete_an_expense()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        var created = await CreateExpenseAsync(ana, groupId, Request(10m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris)));

        var response = await ana.Client.DeleteAsync($"/api/groups/{groupId}/expenses/{created.Id}", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadAsync<List<ExpenseResponse>>(
            await ana.Client.GetAsync($"/api/groups/{groupId}/expenses", Cancellation))).ShouldBeEmpty();
    }

    [Fact]
    public async Task Expenses_cannot_be_reached_through_another_group()
    {
        var (ana, boris, _, groupId) = await GroupOfThreeAsync();
        var otherGroup = await CreateGroupAsync(ana);
        var created = await CreateExpenseAsync(ana, groupId, Request(10m, ana.Id, SplitMethod.Equal, Split(ana), Split(boris)));

        var response = await ana.Client.DeleteAsync($"/api/groups/{otherGroup.Id}/expenses/{created.Id}", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Outsiders_cannot_list_expenses()
    {
        var (_, _, _, groupId) = await GroupOfThreeAsync();
        var outsider = await CreateUserAsync("Outsider");

        var response = await outsider.Client.GetAsync($"/api/groups/{groupId}/expenses", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(TestUser Ana, TestUser Boris, TestUser Vera, Guid GroupId)> GroupOfThreeAsync()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var vera = await CreateUserAsync("Vera");
        var group = await CreateGroupAsync(ana, boris, vera);

        return (ana, boris, vera, group.Id);
    }

    private static async Task<ExpenseResponse> CreateExpenseAsync(TestUser user, Guid groupId, ExpenseRequest request) =>
        await ReadAsync<ExpenseResponse>(
            await user.Client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", request, Cancellation));

    private static ExpenseRequest Request(decimal amount, Guid paidById, SplitMethod method, params SplitRequest[] splits) =>
        new("Dinner", amount, Today, paidById, method, splits);

    private static SplitRequest Split(TestUser user, decimal? value = null) => new(user.Id, value);
}
