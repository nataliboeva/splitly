using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Features.Users;
using Splitly.Domain.Expenses;
using Splitly.Domain.Groups;

namespace Splitly.Api.Data.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(Expense.DescriptionMaxLength);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.SplitMethod).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => new { e.GroupId, e.Date });

        builder.HasOne<Group>().WithMany().HasForeignKey(e => e.GroupId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.PaidById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.Shares).WithOne().HasForeignKey(s => s.ExpenseId);
    }
}

internal sealed class ExpenseShareConfiguration : IEntityTypeConfiguration<ExpenseShare>
{
    public void Configure(EntityTypeBuilder<ExpenseShare> builder)
    {
        builder.ToTable("ExpenseShares");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Amount).HasPrecision(18, 2);
        builder.Property(s => s.Value).HasPrecision(18, 2);
        builder.HasIndex(s => new { s.ExpenseId, s.UserId }).IsUnique();
        builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
