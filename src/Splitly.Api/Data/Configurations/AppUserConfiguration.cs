using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Features.Users;

namespace Splitly.Api.Data.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength);
        builder.Ignore(u => u.Name);
    }
}
