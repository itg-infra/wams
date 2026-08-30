namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).UseSerialColumn();

        builder.Property(w => w.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(w => w.CompanyId).HasColumnName("company_id");
        builder.Property(w => w.BudgetPlanId).HasColumnName("budget_plan_id");
        builder.Property(w => w.BudgetPlanItemId).HasColumnName("budget_plan_item_id");
        builder.Property(w => w.ItemShadowId).HasColumnName("item_shadow_id");
        builder.Property(w => w.ActivityTypeCode).HasColumnName("activity_type_code").IsRequired().HasMaxLength(30);
        builder.Property(w => w.WarehouseShadowId).HasColumnName("warehouse_shadow_id");
        builder.Property(w => w.TemplateCode).HasColumnName("template_code").IsRequired().HasMaxLength(20);
        builder.Property(w => w.CodeBlock).HasColumnName("code_block").HasMaxLength(50);
        builder.Property(w => w.PicUserId).HasColumnName("pic_user_id");
        builder.Property(w => w.StartDate).HasColumnName("start_date");
        builder.Property(w => w.EndDate).HasColumnName("end_date");
        builder.Property(w => w.IsRfba).HasColumnName("is_rfba");
        builder.Property(w => w.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => WorkOrderStatus.FromValue(s));
        builder.Property(w => w.Notes).HasColumnName("notes");

        builder.OwnsOne(w => w.GpsLocation, gps =>
        {
            gps.Property(g => g.Latitude).HasColumnName("gps_latitude").HasColumnType("decimal(10,7)");
            gps.Property(g => g.Longitude).HasColumnName("gps_longitude").HasColumnType("decimal(11,7)");
            gps.Property(g => g.Accuracy).HasColumnName("gps_accuracy").HasColumnType("decimal(8,2)");
            gps.Property(g => g.RecordedAt).HasColumnName("gps_recorded_at");
        });

        // Ensures GPS columns are either all present or all absent (accuracy is excluded - it is optional)
        builder.ToTable(t => t.HasCheckConstraint(
            "chk_work_orders_gps_coherence",
            "(gps_latitude IS NULL AND gps_longitude IS NULL AND gps_recorded_at IS NULL) OR " +
            "(gps_latitude IS NOT NULL AND gps_longitude IS NOT NULL AND gps_recorded_at IS NOT NULL)"));

        builder.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(w => w.SubmittedByUserId).HasColumnName("submitted_by_user_id");
        builder.Property(w => w.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at");
        builder.Property(w => w.CreatedAt).HasColumnName("created_at");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(w => w.Code)
            .IsUnique()
            .HasDatabaseName("ix_work_orders_code");

        builder.HasIndex(w => new { w.CompanyId, w.Status })
            .HasDatabaseName("idx_work_orders_company_status");

        builder.HasIndex(w => w.BudgetPlanId)
            .HasDatabaseName("idx_work_orders_budget_plan_id");

        builder.HasIndex(w => w.BudgetPlanItemId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND budget_plan_item_id IS NOT NULL")
            .HasDatabaseName("uix_work_orders_budget_plan_item_active");

        builder.HasIndex(w => new { w.CreatedAt, w.Id })
            .IsDescending(true, true)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_work_orders_active_created");

        builder.HasIndex(w => new { w.WarehouseShadowId, w.CreatedAt, w.Id })
            .IsDescending(false, true, true)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_work_orders_active_warehouse_created");

        builder.HasIndex(w => new { w.CreatedAt, w.WarehouseShadowId })
            .IsDescending(true, false)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_work_orders_active_created_date");

        builder.HasOne(w => w.Company)
            .WithMany()
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.BudgetPlan)
            .WithMany(b => b.WorkOrders)
            .HasForeignKey(w => w.BudgetPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.BudgetPlanItem)
            .WithMany()
            .HasForeignKey(w => w.BudgetPlanItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(w => w.Activity)
            .WithMany()
            .HasForeignKey(w => w.ItemShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.PicUser)
            .WithMany()
            .HasForeignKey(w => w.PicUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(w => w.CreatedBy)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.SubmittedBy)
            .WithMany()
            .HasForeignKey(w => w.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.UnloadingItems)
            .WithOne(i => i.WorkOrder)
            .HasForeignKey(i => i.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.LoadingItems)
            .WithOne(i => i.WorkOrder)
            .HasForeignKey(i => i.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.FumigationDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderFumigationDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.StorageDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderStorageDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.QcDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderQcDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.HeavyEquipDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderHeavyEquipDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.UnbaggingDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderUnbaggingDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.RebaggingDetail)
            .WithOne(d => d.WorkOrder)
            .HasForeignKey<WorkOrderRebaggingDetail>(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
