using Microsoft.AspNetCore.Identity;
using Splitly.Api.Features.Users;
using Splitly.Domain;
using Splitly.Domain.Activity;
using Splitly.Domain.Expenses;
using Splitly.Domain.Groups;
using Splitly.Domain.Settlements;
using Splitly.Domain.Splits;

namespace Splitly.Api.Data;

/// <summary>
/// Creates a demo account with a couple of groups so the app has something to show right away.
/// Runs in Development only and does nothing if the demo user already exists.
/// </summary>
public sealed class DemoDataSeeder(
    AppDbContext db,
    UserManager<AppUser> userManager,
    TimeProvider clock,
    ILogger<DemoDataSeeder> logger)
{
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "Demo123!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
        {
            return;
        }

        var demo = await CreateUserAsync(DemoEmail, "Demo User");
        var alex = await CreateUserAsync("alex@example.com", "Alex");
        var maria = await CreateUserAsync("maria@example.com", "Maria");
        var ivan = await CreateUserAsync("ivan@example.com", "Ivan");

        // Spread the history over the last ten days so the activity feed looks natural.
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var step = 0;
        DateTimeOffset Later() => now.AddDays(-10).AddHours(++step * 5);

        var euro = Currency.FromCode("EUR");
        var trip = CreateGroup("Weekend in Bansko", euro, demo, Later(), alex, maria, ivan);
        SplitInput[] everyone = [new(demo.Id), new(alex.Id), new(maria.Id), new(ivan.Id)];

        AddExpense(trip, demo, Later(), new ExpenseDetails(
            demo.Id, 480m, "Chalet booking", today.AddDays(-9), SplitMethod.Equal, everyone));
        AddExpense(trip, alex, Later(), new ExpenseDetails(
            alex.Id, 312m, "Ski passes", today.AddDays(-8), SplitMethod.Exact,
            [new(demo.Id, 82.5m), new(alex.Id, 82.5m), new(maria.Id, 73.5m), new(ivan.Id, 73.5m)]));
        AddExpense(trip, maria, Later(), new ExpenseDetails(
            maria.Id, 95.4m, "Groceries", today.AddDays(-8), SplitMethod.Shares,
            [new(demo.Id, 1m), new(alex.Id, 1m), new(maria.Id, 2m), new(ivan.Id, 1m)]));
        AddExpense(trip, ivan, Later(), new ExpenseDetails(
            ivan.Id, 186m, "Dinner at the mehana", today.AddDays(-7), SplitMethod.Percentage,
            [new(demo.Id, 25m), new(alex.Id, 25m), new(maria.Id, 30m), new(ivan.Id, 20m)]));
        AddExpense(trip, demo, Later(), new ExpenseDetails(
            demo.Id, 64m, "Fuel", today.AddDays(-6), SplitMethod.Equal, [new(demo.Id), new(alex.Id)]));
        AddSettlement(trip, from: ivan, to: demo, 50m, today.AddDays(-2), Later());

        var flat = CreateGroup("Flat 3B", euro, demo, Later(), maria);
        SplitInput[] flatmates = [new(demo.Id), new(maria.Id)];

        AddExpense(flat, maria, Later(), new ExpenseDetails(
            maria.Id, 40m, "Internet (October)", today.AddDays(-3), SplitMethod.Equal, flatmates));
        AddExpense(flat, demo, Later(), new ExpenseDetails(
            demo.Id, 23.7m, "Cleaning supplies", today.AddDays(-1), SplitMethod.Equal, flatmates));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo data. Sign in as {Email} to explore it", DemoEmail);
    }

    private async Task<AppUser> CreateUserAsync(string email, string displayName)
    {
        var user = new AppUser { UserName = email, Email = email, DisplayName = displayName, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create demo user {email}: {errors}");
        }

        return user;
    }

    private Group CreateGroup(
        string name, Currency currency, AppUser owner, DateTimeOffset at, params AppUser[] members)
    {
        var group = Group.Create(name, currency, owner.Id, at);
        db.Groups.Add(group);
        db.ActivityEntries.Add(ActivityEntry.GroupCreated(group));

        foreach (var (member, i) in members.Select((m, i) => (m, i)))
        {
            var joinedAt = at.AddMinutes(i + 1);
            group.AddMember(member.Id, joinedAt);
            db.ActivityEntries.Add(ActivityEntry.MemberAdded(group, member.Name, owner.Id, joinedAt));
        }

        return group;
    }

    private void AddExpense(Group group, AppUser addedBy, DateTimeOffset at, ExpenseDetails details)
    {
        var expense = Unwrap(Expense.Create(group, details, addedBy.Id, at));
        db.Expenses.Add(expense);
        db.ActivityEntries.Add(ActivityEntry.ExpenseAdded(group, expense, addedBy.Id, at));
    }

    private void AddSettlement(Group group, AppUser from, AppUser to, decimal amount, DateOnly date, DateTimeOffset at)
    {
        var settlement = Unwrap(Settlement.Create(group, from.Id, to.Id, amount, date, from.Id, at));
        db.Settlements.Add(settlement);
        db.ActivityEntries.Add(ActivityEntry.SettlementRecorded(group, settlement, from.Name, to.Name, from.Id, at));
    }

    private static T Unwrap<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidOperationException($"Invalid demo data: {result.Error}");
}
