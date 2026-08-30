namespace WAMS.Infrastructure.Data.Configurations.AuditLogs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.AuditLogs;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseSerialColumn();

        builder.Property(a => a.Action).IsRequired().HasMaxLength(50).HasColumnName("action");
        builder.Property(a => a.TableName).IsRequired().HasMaxLength(128).HasColumnName("table_name");
        builder.Property(a => a.RecordId).HasColumnName("record_id");
        builder.Property(a => a.RecordKey).HasMaxLength(255).HasColumnName("record_key");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.UserEmail).HasMaxLength(255).HasColumnName("user_email");
        builder.Property(a => a.UserFullname).HasMaxLength(255).HasColumnName("user_fullname");
        builder.Property(a => a.CompanyId).HasColumnName("company_id");
        builder.Property(a => a.OldValues).HasColumnType("jsonb").HasColumnName("old_values");
        builder.Property(a => a.NewValues).HasColumnType("jsonb").HasColumnName("new_values");
        builder.Property(a => a.RequestId).HasMaxLength(100).HasColumnName("request_id");
        builder.Property(a => a.RequestPath).HasMaxLength(255).HasColumnName("request_path");
        builder.Property(a => a.HttpMethod).HasMaxLength(10).HasColumnName("http_method");
        builder.Property(a => a.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
        builder.Property(a => a.UserAgent).HasMaxLength(512).HasColumnName("user_agent");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(a => a.TableName).HasDatabaseName("idx_audit_log_table_name");
        builder.HasIndex(a => a.RecordId).HasDatabaseName("idx_audit_log_record_id");
        builder.HasIndex(a => a.UserId).HasDatabaseName("idx_audit_log_user_id");
        builder.HasIndex(a => a.CompanyId).HasDatabaseName("idx_audit_log_company_id");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("idx_audit_log_created_at");
        builder.HasIndex(a => new { a.CompanyId, a.CreatedAt }).HasDatabaseName("idx_audit_log_company_created");
    }
}
