using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Features.Users;
using Splitly.Domain.Activity;
using Splitly.Domain.Groups;

namespace Splitly.Api.Data.Configurations;

internal sealed class ActivityEntryConfiguration : IEntityTypeConfiguration<ActivityEntry>
{
    public void Configure(EntityTypeBuilder<ActivityEntry> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Message).HasMaxLength(500);
        builder.HasIndex(a => new { a.GroupId, a.CreatedAt });

        builder.HasOne<Group>().WithMany().HasForeignKey(a => a.GroupId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
