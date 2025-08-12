using Books.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Books.Infrastructure.Configurations.ValueObjects;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(e => e.TenantId);
        builder.Property(e => e.TenantId).HasDefaultValue(Guid.NewGuid().ToString());
        builder.Property(e => e.CompanyName).HasMaxLength(Tenant.MaxCompanyNameLength);
    }
}