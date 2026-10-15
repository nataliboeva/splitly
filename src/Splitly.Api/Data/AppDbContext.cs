using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Features.Users;
using Splitly.Domain.Activity;
using Splitly.Domain.Expenses;
using Splitly.Domain.Groups;
using Splitly.Domain.Settlements;

namespace Splitly.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Settlement> Settlements => Set<Settlement>();

    public DbSet<ActivityEntry> ActivityEntries => Set<ActivityEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
