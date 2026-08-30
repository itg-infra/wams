namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderStorageDetailConfiguration : IEntityTypeConfiguration<WorkOrderStorageDetail>
{
    public void Configure(EntityTypeBuilder<WorkOrderStorageDetail> builder)
    {
        builder.ToTable("work_order_storage_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseSerialColumn();

        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.Property(d => d.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(d => d.HasPindahStapel).HasColumnName("has_pindah_stapel");
        builder.Property(d => d.HasPembersihan).HasColumnName("has_pembersihan");
        builder.Property(d => d.HasPerapihan).HasColumnName("has_perapihan");
        builder.Property(d => d.VolumeWeight).HasColumnName("volume_weight").HasPrecision(18, 4);
        builder.Property(d => d.WorkerOnDuty).HasColumnName("worker_on_duty");
        builder.Property(d => d.HasMask).HasColumnName("has_mask");
        builder.Property(d => d.HasSafetyGlasses).HasColumnName("has_safety_glasses");
        builder.Property(d => d.HasHandGloves).HasColumnName("has_hand_gloves");
        builder.Property(d => d.HasHelmet).HasColumnName("has_helmet");
        builder.Property(d => d.HasSafetyShoes).HasColumnName("has_safety_shoes");
        builder.Property(d => d.HasSafetyVest).HasColumnName("has_safety_vest");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
    }
}
