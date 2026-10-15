using FluentValidation;
using Splitly.Api.Features.Expenses;
using Splitly.Domain.Settlements;

namespace Splitly.Api.Features.Settlements;

public sealed record RecordSettlementRequest(Guid FromUserId, Guid ToUserId, decimal Amount, DateOnly Date);

public sealed record SettlementResponse(
    Guid Id, Guid FromUserId, Guid ToUserId, decimal Amount, DateOnly Date, Guid CreatedById, DateTimeOffset CreatedAt)
{
    public static SettlementResponse From(Settlement settlement) => new(
        settlement.Id,
        settlement.FromUserId,
        settlement.ToUserId,
        settlement.Amount,
        settlement.Date,
        settlement.CreatedById,
        settlement.CreatedAt);
}

public sealed class RecordSettlementRequestValidator : AbstractValidator<RecordSettlementRequest>
{
    public RecordSettlementRequestValidator()
    {
        RuleFor(r => r.FromUserId).NotEmpty();
        RuleFor(r => r.ToUserId).NotEmpty();
        RuleFor(r => r.Amount).GreaterThan(0).LessThanOrEqualTo(ExpenseRequestValidator.MaxAmount);
        RuleFor(r => r.Date).NotEmpty();
    }
}
