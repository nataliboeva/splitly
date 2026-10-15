using FluentValidation;
using Splitly.Domain;
using Splitly.Domain.Groups;

namespace Splitly.Api.Features.Groups;

public sealed record CreateGroupRequest(string Name, string Currency);

public sealed record AddMemberRequest(string Email);

public sealed record GroupSummaryResponse(Guid Id, string Name, string Currency, int MemberCount, DateTimeOffset CreatedAt);

public sealed record GroupMemberResponse(Guid UserId, string DisplayName, string Email);

public sealed record GroupResponse(
    Guid Id, string Name, string Currency, DateTimeOffset CreatedAt, IReadOnlyList<GroupMemberResponse> Members);

public sealed record CurrencyResponse(string Code, int MinorUnits);

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(Group.NameMaxLength);
        RuleFor(r => r.Currency)
            .NotEmpty()
            .Must(code => Currency.TryFromCode(code, out _))
            .WithMessage("'{PropertyValue}' is not a supported currency.");
    }
}

public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress();
    }
}
