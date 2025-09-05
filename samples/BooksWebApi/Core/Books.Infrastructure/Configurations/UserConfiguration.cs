using Books.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Books.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(User.MaxNameLength);
        builder.Property(e => e.PasswordHash).IsRequired();
        builder.Navigation(e => e.Role).IsRequired().AutoInclude();
        builder.Navigation(e => e.Tenant).IsRequired().AutoInclude();
    }
}