namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Users;

public class UserWarehouseConfiguration : IEntityTypeConfiguration<UserWarehouse>
{
    public void Configure(EntityTypeBuilder<UserWarehouse> builder)
    {
        builder.ToTable("user_warehouses");

        builder.HasKey(uw => new { uw.UserId, uw.WarehouseId });

        builder.Property(uw => uw.UserId).HasColumnName("user_id");
        builder.Property(uw => uw.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(uw => uw.IsPrimary).HasColumnName("is_primary");

        // Unique partial index: each user can have at most one primary warehouse
        builder.HasIndex(uw => new { uw.UserId, uw.IsPrimary })
            .HasFilter("\"is_primary\" = true")
            .IsUnique()
            .HasDatabaseName("idx_user_warehouses_primary");

        // Declare only the Warehouse side; User side is declared in UserConfiguration
        builder.HasOne(uw => uw.Warehouse)
            .WithMany(w => w.UserWarehouses)
            .HasForeignKey(uw => uw.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
