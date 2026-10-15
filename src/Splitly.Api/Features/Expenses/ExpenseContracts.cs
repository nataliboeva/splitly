using FluentValidation;
using Splitly.Domain.Expenses;
using Splitly.Domain.Splits;

namespace Splitly.Api.Features.Expenses;

/// <summary>
/// One person in a split. <see cref="Value"/> is ignored for equal splits and is an amount, a percentage or
/// a share count for the other split methods.
/// </summary>
public sealed record SplitRequest(Guid UserId, decimal? Value = null);

public sealed record ExpenseRequest(
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid PaidById,
    SplitMethod SplitMethod,
    IReadOnlyList<SplitRequest> Splits)
{
    public ExpenseDetails ToDetails() =>
        new(PaidById, Amount, Description, Date, SplitMethod, [.. Splits.Select(s => new SplitInput(s.UserId, s.Value))]);
}

public sealed record ExpenseShareResponse(Guid UserId, decimal Amount, decimal? Value);

public sealed record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid PaidById,
    SplitMethod SplitMethod,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<ExpenseShareResponse> Shares)
{
    public static ExpenseResponse From(Expense expense) => new(
        expense.Id,
        expense.Description,
        expense.Amount,
        expense.Date,
        expense.PaidById,
        expense.SplitMethod,
        expense.CreatedById,
        expense.CreatedAt,
        expense.UpdatedAt,
        [.. expense.Shares.Select(s => new ExpenseShareResponse(s.UserId, s.Amount, s.Value))]);
}

/// <summary>
/// Checks the shape of the request. Rules that need the group (membership, currency precision,
/// splits adding up) are enforced by the domain model.
/// </summary>
public sealed class ExpenseRequestValidator : AbstractValidator<ExpenseRequest>
{
    public const decimal MaxAmount = 1_000_000_000m;
    public const int MaxParticipants = 50;

    public ExpenseRequestValidator()
    {
        RuleFor(r => r.Description).NotEmpty().MaximumLength(Expense.DescriptionMaxLength);
        RuleFor(r => r.Amount).GreaterThan(0).LessThanOrEqualTo(MaxAmount);
        RuleFor(r => r.Date).NotEmpty();
        RuleFor(r => r.PaidById).NotEmpty();
        RuleFor(r => r.SplitMethod).IsInEnum();
        RuleFor(r => r.Splits)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Choose at least one person to split with.")
            .Must(splits => splits.Count <= MaxParticipants)
            .WithMessage($"An expense can be split between at most {MaxParticipants} people.");
        RuleForEach(r => r.Splits).ChildRules(split => split.RuleFor(s => s.UserId).NotEmpty());
    }
}
