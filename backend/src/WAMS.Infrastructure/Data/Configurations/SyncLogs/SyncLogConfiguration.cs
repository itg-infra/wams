namespace WAMS.Infrastructure.Data.Configurations.SyncLogs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.SyncLogs;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.ToTable("sync_logs");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityAlwaysColumn();
        builder.Property(s => s.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.CompanyCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Outcome).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.AbortReason).HasMaxLength(2000);
        builder.HasIndex(s => new { s.ServiceName, s.StartedAt });
        builder.HasIndex(s => new { s.Outcome, s.ServiceName, s.FinishedAt });
    }
}
