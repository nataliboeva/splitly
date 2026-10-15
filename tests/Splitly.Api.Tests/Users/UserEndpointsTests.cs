using System.Net;
using System.Net.Http.Json;
using Splitly.Api.Features.Users;
using Splitly.Api.Tests.Infrastructure;

namespace Splitly.Api.Tests.Users;

public class UserEndpointsTests(SplitlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Registered_users_can_read_their_profile()
    {
        var ana = await CreateUserAsync("Ana");

        var profile = await ReadAsync<UserProfileResponse>(await ana.Client.GetAsync("/api/users/me", Cancellation));

        profile.Id.ShouldBe(ana.Id);
        profile.Email.ShouldBe(ana.Email);
        profile.DisplayName.ShouldBe("Ana");
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var response = await Factory.CreateClient().GetAsync("/api/users/me", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Display_name_is_required()
    {
        var ana = await CreateUserAsync("Ana");

        var response = await ana.Client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest(" "), Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemAsync(response);
        problem.Errors.ShouldContainKey("DisplayName");
    }

    [Fact]
    public async Task Health_check_reports_healthy()
    {
        var response = await Factory.CreateClient().GetAsync("/health", Cancellation);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Cancellation)).ShouldBe("Healthy");
    }
}
