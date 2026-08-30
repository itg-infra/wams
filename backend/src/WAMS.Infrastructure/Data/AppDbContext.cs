namespace WAMS.Infrastructure.Data;

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.AuditLogs;
using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Files;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Notifications;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Entities.RecapWorkOrders;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Infrastructure.Services.AuditLogs;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IAuditLogQueue? _auditLogQueue;
    private bool _isSavingAuditLogs;
    private static readonly HashSet<string> AuditExcludedProperties = ["PasswordHash", "TokenHash"];
    private static readonly HashSet<string> FullSnapshotTables = ["users", "roles", "permissions", "budget_plans", "purchase_orders", "recap_work_orders"];

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext? tenantContext = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IAuditLogQueue? auditLogQueue = null) : base(options)
    {
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _auditLogQueue = auditLogQueue;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<WarehouseShadow> WarehouseShadows => Set<WarehouseShadow>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VendorShadow> VendorShadows => Set<VendorShadow>();
    public DbSet<ItemShadow> ItemShadows => Set<ItemShadow>();
    public DbSet<UomMaster> UomMasters => Set<UomMaster>();
    public DbSet<TaxType> TaxTypes => Set<TaxType>();
    public DbSet<VendorPphAssignment> VendorPphAssignments => Set<VendorPphAssignment>();
    public DbSet<RateCard> RateCards => Set<RateCard>();
    public DbSet<RateCardItem> RateCardItems => Set<RateCardItem>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<BudgetTemplate> BudgetTemplates => Set<BudgetTemplate>();
    public DbSet<BudgetTemplateItem> BudgetTemplateItems => Set<BudgetTemplateItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<BudgetPlanItem> BudgetPlanItems => Set<BudgetPlanItem>();
    public DbSet<SpkShadow> SpkShadows => Set<SpkShadow>();
    public DbSet<BudgetPlanSpkItem> BudgetPlanSpkItems => Set<BudgetPlanSpkItem>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<TransportOrderShadow> TransportOrderShadows => Set<TransportOrderShadow>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderTransportOrder> WorkOrderTransportOrders => Set<WorkOrderTransportOrder>();
    public DbSet<WorkOrderUnloadingItem> WorkOrderUnloadingItems => Set<WorkOrderUnloadingItem>();
    public DbSet<WorkOrderLoadingItem> WorkOrderLoadingItems => Set<WorkOrderLoadingItem>();
    public DbSet<WorkOrderFumigationDetail> WorkOrderFumigationDetails => Set<WorkOrderFumigationDetail>();
    public DbSet<WorkOrderStorageDetail> WorkOrderStorageDetails => Set<WorkOrderStorageDetail>();
    public DbSet<WorkOrderQcDetail> WorkOrderQcDetails => Set<WorkOrderQcDetail>();
    public DbSet<WorkOrderHeavyEquipDetail> WorkOrderHeavyEquipDetails => Set<WorkOrderHeavyEquipDetail>();
    public DbSet<WorkOrderUnbaggingDetail> WorkOrderUnbaggingDetails => Set<WorkOrderUnbaggingDetail>();
    public DbSet<WorkOrderRebaggingDetail> WorkOrderRebaggingDetails => Set<WorkOrderRebaggingDetail>();
    public DbSet<RecapWorkOrder> RecapWorkOrders => Set<RecapWorkOrder>();
    public DbSet<AccountPayable> AccountPayables => Set<AccountPayable>();
    public DbSet<AccountPayableItem> AccountPayableItems => Set<AccountPayableItem>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowInstanceStage> WorkflowInstanceStages => Set<WorkflowInstanceStage>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<ProvinceAlias> ProvinceAliases => Set<ProvinceAlias>();
    public DbSet<UserProvince> UserProvinces => Set<UserProvince>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Load all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tenant Query Filters
        // _tenantContext == null              → system/migrations/background (no filter)
        // !IsSet                              → unauthenticated request (no filter)
        // IsSet && !CompanyId.HasValue        → must never occur for an authenticated HTTP
        //                                        request; every authenticated request (including
        //                                        Super Admin) always sets a concrete CompanyId via
        //                                        SetCompanyId. This branch only exists for callers
        //                                        outside the HTTP pipeline (e.g. background jobs,
        //                                        migrations, seeding) where _tenantContext itself
        //                                        is left null/unset - it is not a "sees everything"
        //                                        mode for any authenticated user.
        // IsSet && CompanyId.HasValue         → normal tenant filter
        //
        // IMPORTANT: lambdas must capture '_tenantContext' (the instance field), NOT local
        // variables, so EF Core re-evaluates the filter against each DbContext instance.
        // Capturing local variables is evaluated once at model-build time and baked in as
        // a constant - that would permanently disable tenant isolation.

        modelBuilder.Entity<User>().HasQueryFilter(u =>
            u.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)u.CompanyId == _tenantContext.CompanyId));

        modelBuilder.Entity<WarehouseShadow>().HasQueryFilter(w =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)w.CompanyId == _tenantContext.CompanyId);

        // System roles (CompanyId == null) are always visible.
        // Custom roles are filtered by company.
        modelBuilder.Entity<Role>().HasQueryFilter(r =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            r.CompanyId == null || r.CompanyId == _tenantContext.CompanyId);

        // Shadow tables - tenant-scoped, same pattern as WarehouseShadow
        modelBuilder.Entity<VendorShadow>().HasQueryFilter(v =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)v.CompanyId == _tenantContext.CompanyId);

        modelBuilder.Entity<ItemShadow>().HasQueryFilter(i =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)i.CompanyId == _tenantContext.CompanyId);

        modelBuilder.Entity<SpkShadow>().HasQueryFilter(s =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)s.CompanyId == _tenantContext.CompanyId);

        modelBuilder.Entity<TransportOrderShadow>().HasQueryFilter(t =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)t.CompanyId == _tenantContext.CompanyId);

        // TaxType: now SAP-synced per company (PPn/PPh), same tenant-scoping shape
        modelBuilder.Entity<TaxType>().HasQueryFilter(t =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)t.CompanyId == _tenantContext.CompanyId);

        // UomMaster: soft-delete only - global, no tenant filter
        modelBuilder.Entity<UomMaster>().HasQueryFilter(u => u.DeletedAt == null);

        // RateCard: soft-delete + tenant
        modelBuilder.Entity<RateCard>().HasQueryFilter(r =>
            r.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)r.CompanyId == _tenantContext.CompanyId));

        modelBuilder.Entity<RateCard>()
            .HasIndex(r => new { r.VendorShadowId, r.Status, r.SubmittedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_rate_cards_vendor_status_submitted")
            .HasFilter("deleted_at IS NULL");

        // RateCardItem: no filter (scoped by RateCard FK)

        // ActivityType: global master, soft-delete only
        modelBuilder.Entity<ActivityType>()
            .HasQueryFilter(a => a.DeletedAt == null);

        // BudgetTemplate: soft-delete + tenant
        modelBuilder.Entity<BudgetTemplate>().HasQueryFilter(b =>
            b.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)b.CompanyId == _tenantContext.CompanyId));

        // BudgetTemplateItem: no filter (scoped by BudgetTemplate FK)

        // BudgetPlan: soft-delete + tenant
        modelBuilder.Entity<BudgetPlan>().HasQueryFilter(b =>
            b.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)b.CompanyId == _tenantContext.CompanyId));

        // BudgetPlanItem: no filter (scoped by BudgetPlan FK)

        // PurchaseOrder: soft-delete + tenant
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(p =>
            p.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)p.CompanyId == _tenantContext.CompanyId));

        // PurchaseOrderItem: no filter (scoped by PurchaseOrder FK)

        // WorkOrder: soft-delete + tenant
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(w =>
            w.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)w.CompanyId == _tenantContext.CompanyId));

        // WorkOrder detail/item tables: no filter (scoped by WorkOrder FK)

        // RecapWorkOrder: tenant-scoped
        modelBuilder.Entity<RecapWorkOrder>().HasQueryFilter(r =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)r.CompanyId == _tenantContext.CompanyId);

        // AccountPayable: soft-delete + tenant
        modelBuilder.Entity<AccountPayable>().HasQueryFilter(a =>
            a.DeletedAt == null &&
            (_tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
             (long?)a.CompanyId == _tenantContext.CompanyId));

        // AccountPayableItem: no filter (scoped by AccountPayable FK)

        modelBuilder.Entity<WorkflowTemplate>().HasQueryFilter(t =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)t.CompanyId == _tenantContext.CompanyId);

        modelBuilder.Entity<FileAttachment>().HasQueryFilter(f =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)f.CompanyId == _tenantContext.CompanyId);

        modelBuilder.Entity<Notification>().HasQueryFilter(n =>
            _tenantContext == null || !_tenantContext.IsSet || !_tenantContext.CompanyId.HasValue ||
            (long?)n.CompanyId == _tenantContext.CompanyId);
    }

    // Auto-set CompanyId on new entities
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isSavingAuditLogs)
            return await base.SaveChangesAsync(cancellationToken);

        ApplyTenantCompanyIds();

        var pendingAuditLogs = PrepareAuditLogs();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (pendingAuditLogs.Count > 0 && _auditLogQueue is not null)
        {
            var logs = MaterializeAuditLogs(pendingAuditLogs);
            _auditLogQueue.Enqueue(logs);
        }

        return result;
    }

    private List<AuditLog> MaterializeAuditLogs(List<PendingAuditLog> pending)
    {
        var logs = new List<AuditLog>(pending.Count);
        foreach (var p in pending)
        {
            logs.Add(new AuditLog
            {
                UserId = p.UserId,
                UserEmail = p.UserEmail,
                UserFullname = p.UserFullname,
                CompanyId = p.CompanyId,
                Action = p.Action,
                TableName = p.TableName,
                RecordId = TryGetRecordId(p.Entry),
                RecordKey = TryGetRecordKey(p.Entry),
                OldValues = p.OldValues,
                NewValues = p.Action == "CREATE"
                    ? SerializeValues(p.Entry, useOriginalValues: false, p.FullSnapshot)
                    : p.NewValues,
                RequestId = p.RequestId,
                RequestPath = p.RequestPath,
                HttpMethod = p.HttpMethod,
                IpAddress = p.IpAddress,
                UserAgent = p.UserAgent,
            });
        }
        return logs;
    }

    private void ApplyTenantCompanyIds()
    {
        if (_tenantContext is not { IsSet: true }) return;

        if (!_tenantContext.CompanyId.HasValue) return;

        var tenantCompanyId = _tenantContext.CompanyId.Value;

        foreach (var entry in ChangeTracker.Entries<User>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<WarehouseShadow>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<RateCard>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<BudgetTemplate>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<BudgetPlan>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<FileAttachment>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<Notification>()
                         .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<PurchaseOrder>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<WorkOrder>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<RecapWorkOrder>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }

        foreach (var entry in ChangeTracker.Entries<AccountPayable>()
                     .Where(e => e.State == EntityState.Added && e.Entity.CompanyId == 0))
        {
            entry.Entity.CompanyId = tenantCompanyId;
        }
    }

    private List<PendingAuditLog> PrepareAuditLogs()
    {
        ChangeTracker.DetectChanges();

        var httpContext = _httpContextAccessor?.HttpContext;
        var isSystem = httpContext is null;

        return ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog
                     and not RefreshToken
                     and not Notification
                     and not SyncLog
                     and not FileAttachment
                     and not WorkOrder
                     and not WorkOrderUnloadingItem
                     and not WorkOrderLoadingItem
                     and not WorkOrderTransportOrder)
            .Select(entry =>
            {
                var action = GetAuditAction(entry);
                if (action is null)
                    return null;

                var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
                var fullSnapshot = FullSnapshotTables.Contains(tableName);

                return new PendingAuditLog
                {
                    Entry = entry,
                    Action = action,
                    TableName = tableName,
                    FullSnapshot = fullSnapshot,
                    UserId = isSystem ? null : TryGetUserId(httpContext!),
                    UserEmail = isSystem ? "system@internal" : TryGetUserEmail(httpContext!),
                    UserFullname = isSystem ? "System" : TryGetUserFullname(httpContext!),
                    CompanyId = TryGetCompanyId(entry, httpContext),
                    RequestId = httpContext?.Items["RequestId"]?.ToString(),
                    RequestPath = isSystem ? "[SYSTEM]" : httpContext!.Request.Path.Value,
                    HttpMethod = isSystem ? "SYSTEM" : httpContext!.Request.Method,
                    IpAddress = isSystem ? null : TryGetIpAddress(httpContext!),
                    UserAgent = isSystem ? null : TryGetUserAgent(httpContext!),
                    OldValues = action == "CREATE" ? null : SerializeValues(entry, useOriginalValues: true, fullSnapshot),
                    NewValues = action == "DELETE" ? null : SerializeValues(entry, useOriginalValues: false, fullSnapshot)
                };
            })
            .Where(a => a is not null)
            .Select(a => a!)
            .ToList();
    }


    private static string? GetAuditAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return "CREATE";

        if (entry.State == EntityState.Deleted)
            return "DELETE";

        if (entry.State != EntityState.Modified)
            return null;

        var deletedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "DeletedAt");
        if (deletedAtProperty is not null &&
            deletedAtProperty.OriginalValue is null &&
            deletedAtProperty.CurrentValue is DateTime)
        {
            return "DELETE";
        }

        return entry.Properties.Any(p => p.IsModified) ? "UPDATE" : null;
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginalValues, bool fullSnapshot = false)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty() || AuditExcludedProperties.Contains(property.Metadata.Name))
                continue;

            if (!fullSnapshot && entry.State == EntityState.Modified && property.Metadata.Name != "DeletedAt")
            {
                if (!property.IsModified && !property.Metadata.IsPrimaryKey())
                    continue;
            }

            values[property.Metadata.Name] = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }

    private static long? TryGetRecordId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1)
            return null;

        var property = entry.Property(key.Properties[0].Name);

        return property.CurrentValue switch
        {
            long longValue => longValue,
            int intValue => intValue,
            _ => null
        };
    }

    private static string? TryGetRecordKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count <= 1)
            return null;

        var dict = key.Properties.ToDictionary(
            p => p.Name,
            p => entry.Property(p.Name).CurrentValue);
        return JsonSerializer.Serialize(dict);
    }

    public Task WriteAuditLogAsync(AuditLog log, CancellationToken ct = default)
    {
        if (_auditLogQueue is not null)
        {
            _auditLogQueue.Enqueue([log]);
            return Task.CompletedTask;
        }

        // Fallback: queue not registered (e.g. tests, EF tooling) - write synchronously.
        _isSavingAuditLogs = true;
        try
        {
            AuditLogs.Add(log);
            return base.SaveChangesAsync(ct);
        }
        finally
        {
            _isSavingAuditLogs = false;
        }
    }

    private static long? TryGetUserId(HttpContext httpContext)
    {
        var subClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return long.TryParse(subClaim, out var userId) ? userId : null;
    }

    private static string? TryGetUserEmail(HttpContext httpContext)
        => httpContext.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    private static string? TryGetUserFullname(HttpContext httpContext)
        => httpContext.User.FindFirst("fullname")?.Value;

    private static string? TryGetIpAddress(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? TryGetUserAgent(HttpContext httpContext)
    {
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }

    private static long? TryGetCompanyId(EntityEntry entry, HttpContext? httpContext)
    {
        // Direct CompanyId on entity - use OriginalValue for deletes
        var companyProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CompanyId");
        if (companyProp is not null)
        {
            var val = entry.State == EntityState.Deleted
                ? companyProp.OriginalValue
                : companyProp.CurrentValue;
            if (val is long cid && cid > 0)
                return cid;
        }

        // Junction tables: resolve via loaded navigation's CompanyId
        foreach (var reference in entry.References)
        {
            var related = reference.TargetEntry;
            if (related is null) continue;
            var relatedCompany = related.Properties.FirstOrDefault(p => p.Metadata.Name == "CompanyId");
            if (relatedCompany?.CurrentValue is long relCid && relCid > 0)
                return relCid;
        }

        // Fallback: JWT claim (Super Admin acting on tenant data)
        if (httpContext is null) return null;
        var claim = httpContext.User.FindFirst("company_id")?.Value;
        return long.TryParse(claim, out var jwtCid) ? jwtCid : null;
    }

    private sealed class PendingAuditLog
    {
        public EntityEntry Entry { get; init; } = null!;
        public string Action { get; init; } = string.Empty;
        public string TableName { get; init; } = string.Empty;
        public bool FullSnapshot { get; init; }
        public long? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? UserFullname { get; init; }
        public long? CompanyId { get; init; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? RequestId { get; init; }
        public string? RequestPath { get; init; }
        public string? HttpMethod { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
    }
}
