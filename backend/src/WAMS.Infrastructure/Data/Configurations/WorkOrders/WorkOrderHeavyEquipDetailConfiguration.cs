namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderHeavyEquipDetailConfiguration : IEntityTypeConfiguration<WorkOrderHeavyEquipDetail>
{
    public void Configure(EntityTypeBuilder<WorkOrderHeavyEquipDetail> builder)
    {
        builder.ToTable("work_order_heavy_equip_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseSerialColumn();

        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.Property(d => d.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(d => d.BlNumber).HasColumnName("bl_number").HasMaxLength(100);
        builder.Property(d => d.StartTime).HasColumnName("start_time");
        builder.Property(d => d.EndTime).HasColumnName("end_time");
        builder.Property(d => d.StandbyDuration1).HasColumnName("standby_duration1").HasMaxLength(20);
        builder.Property(d => d.StandbyDuration2).HasColumnName("standby_duration2").HasMaxLength(20);
        builder.Property(d => d.MinimumDuration).HasColumnName("minimum_duration").HasMaxLength(20);
        builder.Property(d => d.CostPerHour).HasColumnName("cost_per_hour").HasPrecision(18, 2);
        builder.Property(d => d.TotalCost).HasColumnName("total_cost").HasPrecision(18, 2);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
    }
}
