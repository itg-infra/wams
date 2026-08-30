using MimeDetective;
using MimeDetective.Definitions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Dashboard;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Interfaces.FinanceReports;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Rca;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Rfba;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Application.Interfaces.TransportOrders;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Application.Services.ActivityTypes;
using WAMS.Application.Services.RateCards;
using WAMS.Application.Services.Rbac;
using WAMS.Application.Services.TaxTypes;
using WAMS.Application.Services.Uoms;
using WAMS.Application.Services.Warehouses;
using WAMS.Application.Services.WorkflowTemplates;
using WAMS.Infrastructure.Caching.ActivityTypes;
using WAMS.Infrastructure.Caching.Common;
using WAMS.Infrastructure.Caching.RateCards;
using WAMS.Infrastructure.Caching.Rbac;
using WAMS.Infrastructure.Caching.TaxTypes;
using WAMS.Infrastructure.Caching.Uoms;
using WAMS.Infrastructure.Caching.Warehouses;
using WAMS.Infrastructure.Caching.WorkflowTemplates;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Export;
using WAMS.Infrastructure.Extensions;
using WAMS.Infrastructure.ExternalSap;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Item;
using WAMS.Infrastructure.ExternalSync.Ppn;
using WAMS.Infrastructure.ExternalSync.Pph;
using WAMS.Infrastructure.ExternalSync.Scheduler;
using WAMS.Infrastructure.ExternalSync.Spk;
using WAMS.Infrastructure.ExternalSync.TransportOrder;
using WAMS.Infrastructure.ExternalSync.Vendor;
using WAMS.Infrastructure.ExternalSync.Warehouse;
using WAMS.Infrastructure.Reminders;
using WAMS.Infrastructure.Repositories.AccountPayables;
using WAMS.Infrastructure.Repositories.ActivityTypes;
using WAMS.Infrastructure.Repositories.AuditLogs;
using WAMS.Infrastructure.Repositories.Auth;
using WAMS.Infrastructure.Repositories.BudgetPlans;
using WAMS.Infrastructure.Repositories.BudgetTemplates;
using WAMS.Infrastructure.Repositories.Common;
using WAMS.Infrastructure.Repositories.Companies;
using WAMS.Infrastructure.Repositories.Dashboard;
using WAMS.Infrastructure.Repositories.Files;
using WAMS.Infrastructure.Repositories.FinanceReports;
using WAMS.Infrastructure.Repositories.Items;
using WAMS.Infrastructure.Repositories.Notifications;
using WAMS.Infrastructure.Repositories.PurchaseOrders;
using WAMS.Infrastructure.Repositories.RateCards;
using WAMS.Infrastructure.Repositories.Rbac;
using WAMS.Infrastructure.Repositories.Rca;
using WAMS.Infrastructure.Repositories.RecapWorkOrders;
using WAMS.Infrastructure.Repositories.Spk;
using WAMS.Infrastructure.Repositories.SyncLogs;
using WAMS.Infrastructure.Repositories.TaxTypes;
using WAMS.Infrastructure.Repositories.TransportOrders;
using WAMS.Infrastructure.Repositories.Uoms;
using WAMS.Infrastructure.Repositories.Users;
using WAMS.Infrastructure.Repositories.Vendors;
using WAMS.Infrastructure.Repositories.Warehouses;
using WAMS.Infrastructure.Repositories.WorkflowTemplates;
using WAMS.Infrastructure.Repositories.WorkOrders;
using WAMS.Infrastructure.Services.Common;
using WAMS.Infrastructure.Services.WorkOrders;
using WAMS.Infrastructure.Services.Files;
using WAMS.Infrastructure.Services.Notifications;
using WAMS.Infrastructure.Services.AuditLogs;
using WAMS.Infrastructure.Services.Auth;

namespace WAMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRbacRepository, RbacRepository>();
        services.AddScoped<IWarehouseShadowRepository, WarehouseShadowRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();
        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICodeCounterRepository, CodeCounterRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Audit log async writer
        services.AddSingleton<IAuditLogQueue, AuditLogQueue>();
        services.AddHostedService<AuditLogWorker>();

        // Infra-only services
        services.AddSingleton<IPasswordHasher, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IWarehouseContext, WarehouseContext>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddObjectStorage(configuration);

        // File attachment metadata and entity resolvers
        var mimeInspector = new ContentInspectorBuilder
        {
            Definitions = DefaultDefinitions.All()
        }.Build();

        services.AddSingleton(mimeInspector);
        services.AddSingleton<IFileMimeDetector, FileMimeDetector>();
        // Work orders are the only entity that takes attachments. An unregistered entityType resolves to null
        services.AddScoped<IFileAttachmentEntityHandler, WorkOrderFileAttachmentEntityHandler>();
        services.AddScoped<IFileAttachmentEntityResolver, FileAttachmentEntityResolver>();
        services.AddSingleton<INotificationRealtimeDispatcher, InMemoryNotificationRealtimeDispatcher>();

        // Cache decorators (Real impl lives in Application.Services, wrapped here)
        services.AddKeyedScoped<IRbacService, RbacService>(ServiceKeys.Real);
        services.AddScoped<IRbacService, CachedRbacService>();
        services.AddScoped<IUserPermissionInvalidator, HybridUserPermissionInvalidator>();
        services.AddKeyedScoped<IWarehouseShadowService, WarehouseShadowService>(ServiceKeys.Real);
        services.AddScoped<IWarehouseShadowService, CachedWarehouseShadowService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

        services.AddScoped<IVendorShadowRepository, VendorShadowRepository>();
        services.AddScoped<IItemShadowRepository, ItemShadowRepository>();
        services.AddScoped<ISpkShadowRepository, SpkShadowRepository>();
        services.AddScoped<IUomMasterRepository, UomMasterRepository>();
        services.AddScoped<ITaxTypeRepository, TaxTypeRepository>();
        services.AddScoped<IRateCardRepository, RateCardRepository>();
        services.AddKeyedScoped<IUomService, UomService>(ServiceKeys.Real);
        services.AddScoped<IUomService, CachedUomService>();
        services.AddKeyedScoped<ITaxTypeService, TaxTypeService>(ServiceKeys.Real);
        services.AddScoped<ITaxTypeService, CachedTaxTypeService>();
        services.AddKeyedScoped<IRateCardService, RateCardService>(ServiceKeys.Real);
        services.AddScoped<IRateCardService, CachedRateCardService>();
        services.AddScoped<IActivityTypeRepository, ActivityTypeRepository>();
        services.AddKeyedScoped<IActivityTypeService, ActivityTypeService>(ServiceKeys.Real);
        services.AddScoped<IActivityTypeService, CachedActivityTypeService>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IBudgetTemplateRepository, BudgetTemplateRepository>();
        services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddKeyedScoped<IWorkflowTemplateService, WorkflowTemplateService>(ServiceKeys.Real);
        services.AddScoped<IWorkflowTemplateService, CachedWorkflowTemplateService>();

        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IPdfMetadataResolver, PdfMetadataResolver>();

        // Purchase Orders / SAP
        var useMockSap = configuration.GetValue("ErpApi:UseMockSap", defaultValue: true);
        if (useMockSap)
        {
            services.AddScoped<ISapApiClient, MockSapApiClient>();
        }
        else
        {
            // SAP wrapper is the same service as ErpApi (same /WAMS host) - share its config
            // section entirely instead of maintaining a second one.
            var sapTimeoutSeconds = configuration.GetValue("ErpApi:TimeoutSeconds", 30);

            services.AddHttpClient<SapApiClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["ErpApi:BaseUrl"]
                        ?? throw new InvalidOperationException("ErpApi:BaseUrl is not configured"));

                client.Timeout = TimeSpan.FromSeconds(sapTimeoutSeconds);
            });

            // Deliberately no .AddStandardResilienceHandler(...) here, unlike ErpApiClient:
            // retrying a PO-create POST after a timeout risks creating a duplicate PO in SAP
            // if the first request actually succeeded server-side.
            services.AddScoped<ISapApiClient>(sp => sp.GetRequiredService<SapApiClient>());
        }

        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<ITransportOrderShadowRepository, TransportOrderShadowRepository>();
        services.AddScoped<IRecapWorkOrderRepository, RecapWorkOrderRepository>();
        services.AddScoped<IAccountPayableRepository, AccountPayableRepository>();
        services.AddScoped<IFinanceReportRepository, FinanceReportRepository>();
        services.AddScoped<IRcaRepository, RcaRepository>();
        services.AddScoped<IRcaPdfRenderer, RcaPdfRenderer>();
        services.AddScoped<IPurchaseOrderPdfRenderer, PurchaseOrderPdfRenderer>();
        services.AddScoped<IRfbaFormPdfRenderer, RfbaFormPdfRenderer>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // External Sync
        var erpTimeoutSeconds = configuration.GetValue("ErpApi:TimeoutSeconds", 30);
        var erpRetryCount = configuration.GetValue("ErpApi:RetryCount", 3);
        var erpCbBreakSeconds = configuration.GetValue("ErpApi:CircuitBreakerBreakSeconds", 30);

        var erpIgnoreSsl = configuration.GetValue("ErpApi:IgnoreSslErrors", false);
        if (erpIgnoreSsl)
            Log.Warning("ErpApiClient: SSL certificate validation is DISABLED (ErpApi:IgnoreSslErrors=true). Do not use in production.");

        services.AddHttpClient<ErpApiClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ErpApi:BaseUrl"]
                    ?? throw new InvalidOperationException("ErpApi:BaseUrl is not configured"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = erpIgnoreSsl
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null
        })
        .AddStandardResilienceHandler(options =>
        {
            // Per-attempt timeout
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(erpTimeoutSeconds);

            // Total timeout across all attempts (retries × per-attempt timeout + backoff headroom)
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(erpTimeoutSeconds * (erpRetryCount + 2));

            // Retry: exponential backoff starting at 2s with jitter; only on transient errors / 5xx
            options.Retry.MaxRetryAttempts = erpRetryCount;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.UseJitter = true;

            // Circuit breaker: open when 50% of calls fail over the sampling window (min 5 calls).
            // SamplingDuration must be >= 2× AttemptTimeout (Polly constraint).
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(erpCbBreakSeconds, erpTimeoutSeconds * 2));
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(erpCbBreakSeconds);
        });

        services.AddScoped<IExternalSyncService, WarehouseSyncService>();
        services.AddScoped<IExternalSyncService, VendorSyncService>();
        services.AddScoped<IExternalSyncService, ItemSyncService>();
        services.AddScoped<IExternalSyncService, SpkSyncService>();
        services.AddScoped<IExternalSyncService, ToSyncService>();
        services.AddScoped<IExternalSyncService, PpnSyncService>();
        services.AddScoped<IPphLookupService, PphLookupService>();

        var externalSyncEnabled = configuration.GetValue("ErpApi:SyncEnabled", true);
        if (externalSyncEnabled)
        {
            services.AddHostedService<MasterDataSyncBackgroundService>();
            Log.Information("External master data sync scheduler is ENABLED");
        }
        else
        {
            Log.Warning("External master data sync scheduler is DISABLED by configuration (ErpApi:SyncEnabled=false)");
        }

        // Email service
        var emailEnabled = configuration.GetValue("Email:Enabled", false);
        if (emailEnabled)
        {
            services.AddSingleton<IEmailService, SmtpEmailService>();
            Log.Information("Email service is ENABLED (SMTP)");
        }
        else
        {
            services.AddSingleton<IEmailService, NullEmailService>();
            Log.Information("Email service is DISABLED - using null sender");
        }

        // BP approval reminder scheduler
        var bpReminderEnabled = configuration.GetValue("BudgetPlanReminder:Enabled", true);
        if (bpReminderEnabled)
        {
            services.AddHostedService<BudgetPlanReminderBackgroundService>();
            Log.Information("Budget plan reminder scheduler is ENABLED");
        }
        else
        {
            Log.Warning("Budget plan reminder scheduler is DISABLED by configuration (BudgetPlanReminder:Enabled=false)");
        }

        // Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
