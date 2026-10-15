using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Splitly.Api.Common;

namespace Splitly.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Register, login and refresh come from ASP.NET Core Identity.
        app.MapGroup("/api/auth").WithTags("Auth").MapIdentityApi<AppUser>();

        var me = app.MapGroup("/api/users/me").RequireAuthorization().WithTags("Users");
        me.MapGet("/", GetProfile);
        me.MapPut("/", UpdateProfile).WithValidation<UpdateProfileRequest>();

        return app;
    }

    private static async Task<Results<Ok<UserProfileResponse>, NotFound>> GetProfile(
        ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal);

        return user is null ? TypedResults.NotFound() : TypedResults.Ok(UserProfileResponse.From(user));
    }

    private static async Task<Results<Ok<UserProfileResponse>, NotFound, ValidationProblem>> UpdateProfile(
        UpdateProfileRequest request, ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        user.DisplayName = request.DisplayName.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return TypedResults.ValidationProblem(
                result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        }

        return TypedResults.Ok(UserProfileResponse.From(user));
    }
}
