using Microsoft.AspNetCore.Identity;

namespace Splitly.Api.Features.Users;

public sealed class AppUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 50;

    public string? DisplayName { get; set; }

    /// <summary>The name shown to other people: the display name, or the email until one is set.</summary>
    public string Name => string.IsNullOrWhiteSpace(DisplayName) ? Email ?? string.Empty : DisplayName;
}
