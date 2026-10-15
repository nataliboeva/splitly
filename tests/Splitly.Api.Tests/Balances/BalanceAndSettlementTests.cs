using System.Net;
using System.Net.Http.Json;
using Splitly.Api.Features.Balances;
using Splitly.Api.Features.Expenses;
using Splitly.Api.Features.Settlements;
using Splitly.Api.Tests.Infrastructure;
using Splitly.Domain.Splits;

namespace Splitly.Api.Tests.Balances;

public class BalanceAndSettlementTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    [Fact]
    public async Task Balances_and_suggestions_follow_the_expenses()
    {
        var (ana, boris, vera, groupId) = await GroupWithDinnerPaidByAnaAsync();

        var balances = await GetBalancesAsync(boris, groupId);

        balances.Currency.ShouldBe("EUR");
        balances.Balances.ShouldBe(
        [
            new MemberBalanceResponse(ana.Id, 60m),
            new MemberBalanceResponse(boris.Id, -30m),
            new MemberBalanceResponse(vera.Id, -30m),
        ]);
        balances.SuggestedTransfers.ShouldBe(
        [
            new TransferResponse(boris.Id, ana.Id, 30m),
            new TransferResponse(vera.Id, ana.Id, 30m),
        ], ignoreOrder: true);
    }

    [Fact]
    public async Task Recording_a_settlement_updates_the_balances()
    {
        var (ana, boris, vera, groupId) = await GroupWithDinnerPaidByAnaAsync();

        var response = await boris.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/settlements", new RecordSettlementRequest(boris.Id, ana.Id, 30m, Today), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var balances = await GetBalancesAsync(ana, groupId);
        balances.Balances.Single(b => b.UserId == boris.Id).Amount.ShouldBe(0m);
        balances.SuggestedTransfers.ShouldBe([new TransferResponse(vera.Id, ana.Id, 30m)]);
    }

    [Fact]
    public async Task Settlements_need_two_different_members()
    {
        var (ana, _, _, groupId) = await GroupWithDinnerPaidByAnaAsync();
        var outsider = await CreateUserAsync("Outsider");

        var toSelf = await ana.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/settlements", new RecordSettlementRequest(ana.Id, ana.Id, 5m, Today), Cancellation);
        var toOutsider = await ana.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/settlements", new RecordSettlementRequest(ana.Id, outsider.Id, 5m, Today), Cancellation);

        (await ReadProblemAsync(toSelf)).Detail.ShouldBe("A payment needs two different people.");
        (await ReadProblemAsync(toOutsider)).Detail.ShouldBe("Both people must be members of the group.");
    }

    [Fact]
    public async Task Settlement_amount_must_be_positive()
    {
        var (ana, boris, _, groupId) = await GroupWithDinnerPaidByAnaAsync();

        var response = await boris.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/settlements", new RecordSettlementRequest(boris.Id, ana.Id, 0m, Today), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadValidationProblemAsync(response)).Errors.ShouldContainKey("Amount");
    }

    [Fact]
    public async Task Outsiders_cannot_see_balances_or_record_settlements()
    {
        var (ana, boris, _, groupId) = await GroupWithDinnerPaidByAnaAsync();
        var outsider = await CreateUserAsync("Outsider");

        var balances = await outsider.Client.GetAsync($"/api/groups/{groupId}/balances", Cancellation);
        var settlement = await outsider.Client.PostAsJsonAsync(
            $"/api/groups/{groupId}/settlements", new RecordSettlementRequest(boris.Id, ana.Id, 5m, Today), Cancellation);

        balances.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        settlement.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(TestUser Ana, TestUser Boris, TestUser Vera, Guid GroupId)> GroupWithDinnerPaidByAnaAsync()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var vera = await CreateUserAsync("Vera");
        var group = await CreateGroupAsync(ana, boris, vera);

        var dinner = new ExpenseRequest(
            "Dinner", 90m, Today, ana.Id, SplitMethod.Equal, [new(ana.Id), new(boris.Id), new(vera.Id)]);
        (await ana.Client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", dinner, Cancellation))
            .EnsureSuccessStatusCode();

        return (ana, boris, vera, group.Id);
    }

    private static async Task<GroupBalancesResponse> GetBalancesAsync(TestUser user, Guid groupId) =>
        await ReadAsync<GroupBalancesResponse>(
            await user.Client.GetAsync($"/api/groups/{groupId}/balances", Cancellation));
}
