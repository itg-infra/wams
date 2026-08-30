namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderUnbaggingDetailConfiguration : IEntityTypeConfiguration<WorkOrderUnbaggingDetail>
{
    public void Configure(EntityTypeBuilder<WorkOrderUnbaggingDetail> builder)
    {
        builder.ToTable("work_order_unbagging_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseSerialColumn();

        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.Property(d => d.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(d => d.NoVehicle).HasColumnName("no_vehicle").HasMaxLength(100);
        builder.Property(d => d.NoContainer).HasColumnName("no_container").HasMaxLength(100);
        builder.Property(d => d.NoSeal).HasColumnName("no_seal").HasMaxLength(100);
        builder.Property(d => d.InitialWeight).HasColumnName("initial_weight").HasPrecision(18, 4);
        builder.Property(d => d.FinalWeight).HasColumnName("final_weight").HasPrecision(18, 4);
        builder.Property(d => d.UnitWeight).HasColumnName("unit_weight").HasPrecision(18, 4);
        builder.Property(d => d.TotalWeight).HasColumnName("total_weight").HasPrecision(18, 4);
        builder.Property(d => d.TotalBag).HasColumnName("total_bag");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
    }
}
