using System.Net;
using System.Net.Http.Json;
using Splitly.Api.Features.Groups;
using Splitly.Api.Tests.Infrastructure;

namespace Splitly.Api.Tests.Groups;

public class GroupEndpointsTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Creating_a_group_makes_the_creator_its_first_member()
    {
        var ana = await CreateUserAsync("Ana");

        var response = await ana.Client.PostAsJsonAsync(
            "/api/groups", new CreateGroupRequest("Ski trip", "eur"), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var group = await ReadAsync<GroupResponse>(response);
        group.Name.ShouldBe("Ski trip");
        group.Currency.ShouldBe("EUR");
        group.Members.ShouldHaveSingleItem().UserId.ShouldBe(ana.Id);
        response.Headers.Location!.ToString().ShouldBe($"/api/groups/{group.Id}");
    }

    [Fact]
    public async Task Unsupported_currencies_are_rejected()
    {
        var ana = await CreateUserAsync("Ana");

        var response = await ana.Client.PostAsJsonAsync(
            "/api/groups", new CreateGroupRequest("Ski trip", "XYZ"), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadValidationProblemAsync(response)).Errors["Currency"]
            .ShouldBe(["'XYZ' is not a supported currency."]);
    }

    [Fact]
    public async Task Members_can_be_added_by_email()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var group = await CreateGroupAsync(ana);

        var response = await ana.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/members", new AddMemberRequest(boris.Email.ToUpperInvariant()), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var details = await ReadAsync<GroupResponse>(
            await boris.Client.GetAsync($"/api/groups/{group.Id}", Cancellation));
        details.Members.Select(m => m.DisplayName).ShouldBe(["Ana", "Boris"]);
    }

    [Fact]
    public async Task Adding_someone_without_an_account_is_a_validation_error()
    {
        var ana = await CreateUserAsync("Ana");
        var group = await CreateGroupAsync(ana);

        var response = await ana.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/members", new AddMemberRequest("nobody@example.com"), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadValidationProblemAsync(response)).Errors.ShouldContainKey("Email");
    }

    [Fact]
    public async Task Adding_an_existing_member_is_a_conflict()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var group = await CreateGroupAsync(ana, boris);

        var response = await ana.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/members", new AddMemberRequest(boris.Email), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Users_only_see_their_own_groups()
    {
        var ana = await CreateUserAsync("Ana");
        var boris = await CreateUserAsync("Boris");
        var shared = await CreateGroupAsync(ana, boris);
        await CreateGroupAsync(ana);

        var borisGroups = await ReadAsync<List<GroupSummaryResponse>>(
            await boris.Client.GetAsync("/api/groups", Cancellation));

        var summary = borisGroups.ShouldHaveSingleItem();
        summary.Id.ShouldBe(shared.Id);
        summary.MemberCount.ShouldBe(2);
    }

    [Fact]
    public async Task Outsiders_get_not_found_for_someone_elses_group()
    {
        var ana = await CreateUserAsync("Ana");
        var mallory = await CreateUserAsync("Mallory");
        var group = await CreateGroupAsync(ana);

        var read = await mallory.Client.GetAsync($"/api/groups/{group.Id}", Cancellation);
        var join = await mallory.Client.PostAsJsonAsync(
            $"/api/groups/{group.Id}/members", new AddMemberRequest(mallory.Email), Cancellation);

        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        join.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Supported_currencies_are_listed()
    {
        var currencies = await ReadAsync<List<CurrencyResponse>>(
            await Factory.CreateClient().GetAsync("/api/currencies", Cancellation));

        currencies.ShouldContain(new CurrencyResponse("EUR", 2));
        currencies.ShouldContain(new CurrencyResponse("JPY", 0));
    }
}
