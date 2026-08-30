namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Users;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseSerialColumn();

        builder.Property(u => u.CompanyId).IsRequired();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Fullname).IsRequired().HasMaxLength(100);
        builder.Property(u => u.EmployeeId).HasMaxLength(50);

        // Partial unique index: email must be unique among non-deleted users
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL")
            .HasDatabaseName("idx_users_email");

        // Partial index for active users
        builder.HasIndex(u => new { u.IsActive, u.DeletedAt })
            .HasFilter("\"is_active\" = true AND \"deleted_at\" IS NULL")
            .HasDatabaseName("idx_users_is_active");

        builder.HasIndex(u => u.CompanyId)
            .HasDatabaseName("idx_users_company_id");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("idx_users_created_at");

        // Global query filter: automatically exclude soft-deleted users
        builder.HasQueryFilter(u => u.DeletedAt == null);

        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserWarehouses)
            .WithOne(uw => uw.User)
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map property names to snake_case column names (PostgreSQL convention)
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
        builder.Property(u => u.EmployeeId).HasColumnName("employee_id");
        builder.Property(u => u.CreatedBy).HasColumnName("created_by");
    }
}
