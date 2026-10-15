using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Common;
using Splitly.Api.Data;
using Splitly.Api.Features.Groups;
using Splitly.Domain.Activity;
using Splitly.Domain.Expenses;

namespace Splitly.Api.Features.Expenses;

public static class ExpenseEndpoints
{
    private const string OnlyCreatorMayChange = "Only the person who added this expense can change it.";

    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var expenses = app.MapGroup("/api/groups/{groupId:guid}/expenses")
            .RequireAuthorization()
            .RequireGroupMembership()
            .WithTags("Expenses");

        expenses.MapGet("/", ListExpenses);
        expenses.MapPost("/", CreateExpense).WithValidation<ExpenseRequest>();
        expenses.MapPut("/{expenseId:guid}", UpdateExpense).WithValidation<ExpenseRequest>();
        expenses.MapDelete("/{expenseId:guid}", DeleteExpense);

        return app;
    }

    private static async Task<Ok<List<ExpenseResponse>>> ListExpenses(
        Guid groupId, AppDbContext db, CancellationToken cancellationToken)
    {
        var expenses = await db.Expenses
            .AsNoTracking()
            .Include(e => e.Shares)
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(expenses.Select(ExpenseResponse.From).ToList());
    }

    private static async Task<Results<Created<ExpenseResponse>, ProblemHttpResult>> CreateExpense(
        Guid groupId,
        ExpenseRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var group = await db.GetGroupWithMembersAsync(groupId, cancellationToken);
        var userId = principal.GetUserId();
        var now = clock.GetUtcNow();

        var result = Expense.Create(group, request.ToDetails(), userId, now);
        if (!result.IsSuccess)
        {
            return ApiProblems.BusinessRule(result.Error);
        }

        var expense = result.Value;
        db.Expenses.Add(expense);
        db.ActivityEntries.Add(ActivityEntry.ExpenseAdded(group, expense, userId, now));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/groups/{groupId}/expenses/{expense.Id}", ExpenseResponse.From(expense));
    }

    private static async Task<Results<Ok<ExpenseResponse>, ProblemHttpResult>> UpdateExpense(
        Guid groupId,
        Guid expenseId,
        ExpenseRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var expense = await FindExpenseAsync(db, groupId, expenseId, cancellationToken);
        if (expense is null)
        {
            return ApiProblems.NotFound("Expense not found.");
        }

        var userId = principal.GetUserId();
        if (!expense.CanBeChangedBy(userId))
        {
            return ApiProblems.Forbidden(OnlyCreatorMayChange);
        }

        var group = await db.GetGroupWithMembersAsync(groupId, cancellationToken);
        var now = clock.GetUtcNow();

        var result = expense.Update(group, request.ToDetails(), now);
        if (!result.IsSuccess)
        {
            return ApiProblems.BusinessRule(result.Error);
        }

        db.ActivityEntries.Add(ActivityEntry.ExpenseUpdated(group, expense, userId, now));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ExpenseResponse.From(expense));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteExpense(
        Guid groupId,
        Guid expenseId,
        ClaimsPrincipal principal,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var expense = await FindExpenseAsync(db, groupId, expenseId, cancellationToken);
        if (expense is null)
        {
            return ApiProblems.NotFound("Expense not found.");
        }

        var userId = principal.GetUserId();
        if (!expense.CanBeChangedBy(userId))
        {
            return ApiProblems.Forbidden(OnlyCreatorMayChange);
        }

        var group = await db.GetGroupWithMembersAsync(groupId, cancellationToken);

        db.Expenses.Remove(expense);
        db.ActivityEntries.Add(ActivityEntry.ExpenseDeleted(group, expense, userId, clock.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static Task<Expense?> FindExpenseAsync(
        AppDbContext db, Guid groupId, Guid expenseId, CancellationToken cancellationToken) =>
        db.Expenses
            .Include(e => e.Shares)
            .SingleOrDefaultAsync(e => e.Id == expenseId && e.GroupId == groupId, cancellationToken);
}
