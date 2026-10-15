namespace Splitly.Domain.Balances;

/// <summary>A suggested payment that helps settle the group's balances.</summary>
public sealed record Transfer(Guid FromUserId, Guid ToUserId, decimal Amount);
