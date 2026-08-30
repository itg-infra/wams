namespace WAMS.Infrastructure.Data.Configurations.Companies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Companies;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseSerialColumn();

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(255);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(255);
        builder.Property(c => c.LogoStorageKey).HasMaxLength(500);

        builder.HasIndex(c => c.Code).IsUnique();

        // Relationships
        builder.HasMany(c => c.Users)
            .WithOne(u => u.Company)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);  // do not cascade-delete users if company is deleted

        builder.HasMany(c => c.Warehouses)
            .WithOne(w => w.Company)
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}