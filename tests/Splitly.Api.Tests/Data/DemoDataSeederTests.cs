using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using Splitly.Api.Data;
using Splitly.Api.Features.Balances;
using Splitly.Api.Features.Groups;
using Splitly.Api.Tests.Infrastructure;

namespace Splitly.Api.Tests.Data;

public class DemoDataSeederTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Demo_user_can_sign_in_and_see_consistent_balances()
    {
        // Seeding twice must be harmless because it runs on every Development startup.
        await SeedAsync();
        await SeedAsync();

        var client = await SignInAsDemoUserAsync();
        var groups = await ReadAsync<List<GroupSummaryResponse>>(await client.GetAsync("/api/groups", Cancellation));

        groups.Select(g => g.Name).ShouldBe(["Flat 3B", "Weekend in Bansko"], ignoreOrder: true);
        foreach (var group in groups)
        {
            var balances = await ReadAsync<GroupBalancesResponse>(
                await client.GetAsync($"/api/groups/{group.Id}/balances", Cancellation));
            balances.Balances.Sum(b => b.Amount).ShouldBe(0m);
            balances.SuggestedTransfers.ShouldNotBeEmpty();
        }
    }

    private async Task SeedAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(Cancellation);
    }

    private async Task<HttpClient> SignInAsDemoUserAsync()
    {
        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = DemoDataSeeder.DemoEmail, Password = DemoDataSeeder.DemoPassword },
            Cancellation);
        var tokens = await ReadAsync<AccessTokenResponse>(login);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }
}
