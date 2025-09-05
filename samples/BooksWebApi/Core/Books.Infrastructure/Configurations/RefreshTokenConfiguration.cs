using Books.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MitMediator.AppAuthorize.Domain;

namespace Books.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        var userIdConverter = new ValueConverter<string, int>(
            v => int.Parse(v),
            v => v.ToString()); 
        
        builder.HasKey(e => e.RefreshTokenKey);
        builder.Property(e => e.UserId)
            .HasConversion(userIdConverter)
            .IsRequired();
        
        builder.Property(e => e.Expired).IsRequired();
    }
}