using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Features.Groups;
using Splitly.Api.Features.Users;

namespace Splitly.Api.Tests.Infrastructure;

[Collection(nameof(ApiCollection))]
public abstract class ApiTestBase(SplitlyApiFactory factory) : IAsyncLifetime
{
    public const string Password = "Passw0rd!";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    protected SplitlyApiFactory Factory { get; } = factory;

    protected static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(
            Factory.DockerUnavailableReason is not null,
            $"Integration tests need Docker to run PostgreSQL: {Factory.DockerUnavailableReason}");

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Registers a brand-new user, signs them in and returns a client that sends their token.</summary>
    protected async Task<TestUser> CreateUserAsync(string displayName)
    {
        var email = $"{displayName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var anonymous = Factory.CreateClient();

        var register = await anonymous.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest { Email = email, Password = Password }, Cancellation);
        register.EnsureSuccessStatusCode();

        var login = await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest { Email = email, Password = Password }, Cancellation);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AccessTokenResponse>(Json, Cancellation);

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var profile = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest(displayName), Cancellation);
        var user = await ReadAsync<UserProfileResponse>(profile);

        return new TestUser(client, user.Id, email, displayName);
    }

    /// <summary>Creates a group owned by <paramref name="owner"/> and adds the other users as members.</summary>
    protected static async Task<GroupResponse> CreateGroupAsync(TestUser owner, params TestUser[] members) =>
        await CreateGroupAsync(owner, "EUR", members);

    protected static async Task<GroupResponse> CreateGroupAsync(TestUser owner, string currency, params TestUser[] members)
    {
        var created = await owner.Client.PostAsJsonAsync(
            "/api/groups", new CreateGroupRequest("Trip", currency), Cancellation);
        var group = await ReadAsync<GroupResponse>(created);

        foreach (var member in members)
        {
            var added = await owner.Client.PostAsJsonAsync(
                $"/api/groups/{group.Id}/members", new AddMemberRequest(member.Email), Cancellation);
            added.EnsureSuccessStatusCode();
        }

        return group;
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(Cancellation);
            Assert.Fail($"Expected a successful response but got {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<T>(Json, Cancellation))!;
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, Cancellation))!;

    protected static async Task<HttpValidationProblemDetails> ReadValidationProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Json, Cancellation))!;
}

public sealed record TestUser(HttpClient Client, Guid Id, string Email, string DisplayName);
