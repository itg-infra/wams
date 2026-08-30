using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Dashboard;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Interfaces.FinanceReports;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rca;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Application.Services.AccountPayables;
using WAMS.Application.Services.AuditLogs;
using WAMS.Application.Services.Auth;
using WAMS.Application.Services.BudgetPlans;
using WAMS.Application.Services.BudgetTemplates;
using WAMS.Application.Services.Companies;
using WAMS.Application.Services.Dashboard;
using WAMS.Application.Services.Files;
using WAMS.Application.Services.FinanceReports;
using WAMS.Application.Services.Notifications;
using WAMS.Application.Services.PurchaseOrders;
using WAMS.Application.Services.Rca;
using WAMS.Application.Services.RecapWorkOrders;
using WAMS.Application.Services.Spk;
using WAMS.Application.Services.SyncLogs;
using WAMS.Application.Services.Users;
using WAMS.Application.Services.WorkOrders;
using WAMS.Application.Validators.Auth;

namespace WAMS.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers business-logic services and FluentValidation validators. No caching decorators
    /// or infrastructure-backed dependencies here - those go through AddInfrastructureServices.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISyncLogService, SyncLogService>();
        services.AddScoped<ISpkService, SpkService>();
        services.AddScoped<IBudgetTemplateService, BudgetTemplateService>();
        services.AddScoped<IBudgetPlanService, BudgetPlanService>();
        services.AddScoped<IFileAttachmentService, FileAttachmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IRecapWorkOrderService, RecapWorkOrderService>();
        services.AddScoped<IAccountPayableService, AccountPayableService>();
        services.AddScoped<IFinanceReportService, FinanceReportService>();
        services.AddScoped<IRcaService, RcaService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
