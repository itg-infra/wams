namespace WAMS.Infrastructure.Data.Configurations.Files;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Files;

public sealed class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("file_attachments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseSerialColumn();

        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
        builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.CompanyId, x.EntityType, x.EntityId })
            .HasDatabaseName("ix_file_attachments_company_entity");

        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName("ix_file_attachments_storage_key");

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
