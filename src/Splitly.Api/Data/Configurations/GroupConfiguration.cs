using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Features.Users;
using Splitly.Domain;
using Splitly.Domain.Groups;

namespace Splitly.Api.Data.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).HasMaxLength(Group.NameMaxLength);
        builder.Property(g => g.Currency)
            .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
            .HasMaxLength(3);

        builder.HasOne<AppUser>().WithMany().HasForeignKey(g => g.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(g => g.Members).WithOne().HasForeignKey(m => m.GroupId);
    }
}

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.HasKey(m => new { m.GroupId, m.UserId });
        builder.HasIndex(m => m.UserId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
