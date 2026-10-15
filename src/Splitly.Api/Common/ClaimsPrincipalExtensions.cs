using System.Security.Claims;

namespace Splitly.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The id of the signed-in user. Only call this on endpoints that require authorization.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The user is not authenticated.");

        return Guid.Parse(value);
    }
}
