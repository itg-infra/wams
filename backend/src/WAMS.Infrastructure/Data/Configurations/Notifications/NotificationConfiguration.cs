namespace WAMS.Infrastructure.Data.Configurations.Notifications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).UseSerialColumn();

        builder.Property(n => n.CompanyId).HasColumnName("company_id");
        builder.Property(n => n.RecipientUserId).HasColumnName("recipient_user_id");
        builder.Property(n => n.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(n => n.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(n => n.ReferenceType).HasColumnName("reference_type").HasMaxLength(100).IsRequired();
        builder.Property(n => n.ReferenceId).HasColumnName("reference_id").HasMaxLength(100).IsRequired();
        builder.Property(n => n.IsRead).HasColumnName("is_read");
        builder.Property(n => n.ReadAt).HasColumnName("read_at");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("ix_notifications_recipient_read_created_at");

        builder.HasIndex(n => new { n.CompanyId, n.ReferenceType, n.ReferenceId })
            .HasDatabaseName("ix_notifications_company_reference");

        // Supports ExistsByTypeAndRecipientAsync used by the BP reminder cooldown check
        builder.HasIndex(n => new { n.Type, n.RecipientUserId, n.CreatedAt })
            .HasDatabaseName("ix_notifications_type_recipient_created_at");

        builder.HasOne(n => n.Company)
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.ActorUser)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
