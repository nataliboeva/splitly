using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Features.Users;
using Splitly.Domain.Groups;
using Splitly.Domain.Settlements;

namespace Splitly.Api.Data.Configurations;

internal sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Amount).HasPrecision(18, 2);
        builder.HasIndex(s => s.GroupId);

        builder.HasOne<Group>().WithMany().HasForeignKey(s => s.GroupId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.FromUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.ToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
