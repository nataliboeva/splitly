using FluentValidation;

namespace Splitly.Api.Features.Users;

public sealed record UserProfileResponse(Guid Id, string Email, string DisplayName)
{
    public static UserProfileResponse From(AppUser user) => new(user.Id, user.Email ?? string.Empty, user.Name);
}

public sealed record UpdateProfileRequest(string DisplayName);

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(r => r.DisplayName).NotEmpty().MaximumLength(AppUser.DisplayNameMaxLength);
    }
}
