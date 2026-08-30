namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderFumigationDetailConfiguration : IEntityTypeConfiguration<WorkOrderFumigationDetail>
{
    public void Configure(EntityTypeBuilder<WorkOrderFumigationDetail> builder)
    {
        builder.ToTable("work_order_fumigation_details");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseSerialColumn();

        builder.HasIndex(d => d.WorkOrderId).IsUnique();

        builder.Property(d => d.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(d => d.FumiId).HasColumnName("fumi_id").HasMaxLength(100);
        builder.Property(d => d.TotalDuration).HasColumnName("total_duration").HasMaxLength(50);
        builder.Property(d => d.BlNumber).HasColumnName("bl_number").HasMaxLength(100);
        builder.Property(d => d.MvName).HasColumnName("mv_name").HasMaxLength(200);
        builder.Property(d => d.InitialTemperature).HasColumnName("initial_temperature").HasPrecision(10, 2);
        builder.Property(d => d.FinalTemperature).HasColumnName("final_temperature").HasPrecision(10, 2);
        builder.Property(d => d.FumigationType).HasColumnName("fumigation_type").HasMaxLength(100);
        builder.Property(d => d.MethylBromideDosage).HasColumnName("methyl_bromide_dosage").HasPrecision(11, 4);
        builder.Property(d => d.SulphurFluorideDosage).HasColumnName("sulphur_fluoride_dosage").HasPrecision(11, 4);
        builder.Property(d => d.PhosphineDosage).HasColumnName("phosphine_dosage").HasPrecision(11, 4);
        builder.Property(d => d.Result).HasColumnName("result").HasMaxLength(200);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
    }
}
