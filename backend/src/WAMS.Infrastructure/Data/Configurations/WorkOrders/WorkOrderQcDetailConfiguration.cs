namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderQcDetailConfiguration : IEntityTypeConfiguration<WorkOrderQcDetail>
{
    public void Configure(EntityTypeBuilder<WorkOrderQcDetail> builder)
    {
        builder.ToTable("work_order_qc_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseSerialColumn();

        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.Property(d => d.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(d => d.MoisturePercent).HasColumnName("moisture_percent").HasPrecision(5, 2);
        builder.Property(d => d.JamurPercent).HasColumnName("jamur_percent").HasPrecision(5, 2);
        builder.Property(d => d.BauPercent).HasColumnName("bau_percent").HasPrecision(5, 2);
        builder.Property(d => d.QualityStatus).HasColumnName("quality_status").HasMaxLength(50);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
    }
}
