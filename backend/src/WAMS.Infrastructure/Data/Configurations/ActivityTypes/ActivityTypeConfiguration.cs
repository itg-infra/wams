namespace WAMS.Infrastructure.Data.Configurations.ActivityTypes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.ActivityTypes;

public class ActivityTypeConfiguration : IEntityTypeConfiguration<ActivityType>
{
    public void Configure(EntityTypeBuilder<ActivityType> builder)
    {
        builder.ToTable("activity_types");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseSerialColumn();

        builder.Property(a => a.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(a => a.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(a => a.IsActive).HasColumnName("is_active");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(a => a.Code)
            .IsUnique()
            .HasDatabaseName("ix_activity_types_code");
    }
}
