namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Users;

public class UserCompanyWarehouseConfiguration : IEntityTypeConfiguration<UserCompanyWarehouse>
{
    public void Configure(EntityTypeBuilder<UserCompanyWarehouse> builder)
    {
        builder.ToTable("user_company_warehouses");
        builder.HasKey(ucw => new { ucw.UserCompanyId, ucw.WarehouseId });
        builder.Property(ucw => ucw.UserCompanyId).HasColumnName("user_company_id");
        builder.Property(ucw => ucw.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(ucw => ucw.IsPrimary).HasColumnName("is_primary");

        builder.HasIndex(ucw => new { ucw.UserCompanyId, ucw.IsPrimary })
            .HasFilter("is_primary = true")
            .IsUnique()
            .HasDatabaseName("idx_user_company_warehouses_primary");

        builder.HasOne(ucw => ucw.UserCompany)
            .WithMany(uc => uc.Warehouses)
            .HasForeignKey(ucw => ucw.UserCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucw => ucw.Warehouse)
            .WithMany(w => w.UserCompanyWarehouses)
            .HasForeignKey(ucw => ucw.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
