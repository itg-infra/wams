namespace WAMS.Infrastructure.Reminders;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Enums;

public class BudgetPlanReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BudgetPlanReminderOptions> options,
    ILogger<BudgetPlanReminderBackgroundService> logger) : BackgroundService
{
    private const string ReminderNotificationType = "budget_plan_approval_reminder";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;

        if (!opts.Enabled)
        {
            logger.LogInformation("[BPReminder] Disabled - skipping scheduler startup");
            return;
        }

        var tz = ResolveTimeZone(opts.TimeZoneId);

        logger.LogInformation(
            "[BPReminder] Scheduler started. Interval={Interval}min Threshold={Threshold}h Cooldown={Cooldown}h Window={Start:D2}:00–{End:D2}:00 ({TZ})",
            opts.IntervalMinutes, opts.ThresholdHours, opts.CooldownHours,
            opts.ActiveWindowStartHour, opts.ActiveWindowEndHour, tz.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = ComputeNextRunUtc(DateTime.UtcNow, opts, tz);
            var delay = nextRun - DateTime.UtcNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            logger.LogInformation(
                "[BPReminder] Next run at {NextRun:yyyy-MM-dd HH:mm} UTC (in {Delay})",
                nextRun, delay);

            await Task.Delay(delay, stoppingToken);
            await RunAsync(opts, stoppingToken);
        }
    }

    internal static DateTime ComputeNextRunUtc(DateTime utcNow, BudgetPlanReminderOptions opts, TimeZoneInfo tz)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var intervalMin = opts.IntervalMinutes;

        var minutesSinceMidnight = local.TimeOfDay.TotalMinutes;
        var nextBoundaryMinutes = (Math.Floor(minutesSinceMidnight / intervalMin) + 1) * intervalMin;
        var candidate = local.Date.AddMinutes(nextBoundaryMinutes);

        if (candidate.Hour >= opts.ActiveWindowStartHour && candidate.Hour < opts.ActiveWindowEndHour)
            return TimeZoneInfo.ConvertTimeToUtc(candidate, tz);

        if (candidate.Hour < opts.ActiveWindowStartHour)
        {
            var windowStart = candidate.Date.AddHours(opts.ActiveWindowStartHour);
            return TimeZoneInfo.ConvertTimeToUtc(windowStart, tz);
        }

        var tomorrowWindowStart = candidate.Date.AddDays(1).AddHours(opts.ActiveWindowStartHour);
        return TimeZoneInfo.ConvertTimeToUtc(tomorrowWindowStart, tz);
    }

    private async Task RunAsync(BudgetPlanReminderOptions opts, CancellationToken ct)
    {
        logger.LogInformation("[BPReminder] Run started at {Time:u}", DateTime.UtcNow);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var budgetPlanRepo = scope.ServiceProvider.GetRequiredService<IBudgetPlanRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var cutoff = DateTime.UtcNow.AddHours(-opts.ThresholdHours);
            var overdue = await budgetPlanRepo.GetOverdueForReminderAsync(cutoff, ct);

            if (overdue.Count == 0)
            {
                logger.LogInformation("[BPReminder] No overdue budget plans found");
                return;
            }

            logger.LogInformation("[BPReminder] Found {Count} overdue budget plan(s)", overdue.Count);

            var approverMap = await BuildApproverMapAsync(overdue, userRepo, ct);

            if (approverMap.Count == 0)
            {
                logger.LogWarning("[BPReminder] Overdue BPs found but no approvers resolved - check role/warehouse assignments");
                return;
            }

            var cooldownSince = DateTime.UtcNow.AddHours(-opts.CooldownHours);

            foreach (var (approver, stageOrder, stageName, plans) in approverMap.Values)
            {
                if (ct.IsCancellationRequested) break;

                var alreadyNotified = await notificationRepo.ExistsByTypeAndRecipientAsync(
                    ReminderNotificationType, approver.Id, cooldownSince, ct);

                if (alreadyNotified)
                {
                    logger.LogDebug(
                        "[BPReminder] Approver {UserId} already notified within cooldown window - skipping",
                        approver.Id);
                    continue;
                }

                await NotifyApproverAsync(approver, stageOrder, stageName, plans, notificationService, emailService, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[BPReminder] Unhandled exception during run");
        }
    }

    // Groups overdue BPs by (approverUserId, stageOrder) - queries approvers once per (company, warehouse, roles) group.
    private async Task<Dictionary<(long UserId, int StageOrder), (User Approver, int StageOrder, string StageName, List<BudgetPlan> Plans)>>
        BuildApproverMapAsync(List<BudgetPlan> overdue, IUserRepository userRepo, CancellationToken ct)
    {
        Dictionary<(long UserId, int StageOrder), (User Approver, int StageOrder, string StageName, List<BudgetPlan> Plans)> result = new();

        // Group by (company, warehouse, stageOrder, approverRoles key) to batch approver queries
        var groups = overdue
            .Select(b =>
            {
                var pendingStage = b.WorkflowInstance?.Stages
                    .OrderBy(s => s.StageOrder)
                    .FirstOrDefault(s => s.Status == WorkflowStageStatus.Pending);
                return (Plan: b, Stage: pendingStage);
            })
            .Where(x => x.Stage is not null)
            .GroupBy(x => (
                x.Plan.CompanyId,
                x.Plan.WarehouseShadowId,
                x.Stage!.StageOrder,
                RolesKey: string.Join(",", x.Stage!.ApproverRoles.OrderBy(r => r))));

        foreach (var group in groups)
        {
            var anyStage = group.First().Stage!;
            var approverRoles = new HashSet<string>(anyStage.ApproverRoles, StringComparer.OrdinalIgnoreCase);

            var approvers = await userRepo.GetUsersByRolesAndWarehouseAsync(
                group.Key.CompanyId, group.Key.WarehouseShadowId, approverRoles, ct);

            if (approvers.Count == 0)
            {
                logger.LogWarning(
                    "[BPReminder] No stage {Stage} approvers for company {Company} warehouse {Warehouse} roles [{Roles}]",
                    group.Key.StageOrder, group.Key.CompanyId, group.Key.WarehouseShadowId,
                    string.Join(", ", anyStage.ApproverRoles));
                continue;
            }

            foreach (var approver in approvers)
            {
                var key = (approver.Id, group.Key.StageOrder);
                if (!result.ContainsKey(key))
                    result[key] = (approver, group.Key.StageOrder, anyStage.StageName, []);

                result[key].Plans.AddRange(group.Select(x => x.Plan));
            }
        }

        return result;
    }

    private async Task NotifyApproverAsync(
        User approver,
        int stageOrder,
        string stageName,
        List<BudgetPlan> plans,
        INotificationService notificationService,
        IEmailService emailService,
        CancellationToken ct)
    {
        var count = plans.Count;

        var title = count == 1
            ? $"Budget Plan Pending Approval - Stage {stageOrder}: {stageName}"
            : $"{count} Budget Plans Pending Approval - Stage {stageOrder}";

        var message = count == 1
            ? $"Budget plan {plans[0].Code} is waiting for your stage {stageOrder} approval ({stageName})."
            : $"You have {count} budget plans waiting for your stage {stageOrder} approval ({stageName}).";

        var notification = new NotificationCreateRequest(
            plans[0].CompanyId,
            approver.Id,
            null,
            ReminderNotificationType,
            title,
            message,
            count == 1 ? "budget_plan" : "budget_plan_batch",
            count == 1 ? plans[0].Id.ToString() : $"stage_{stageOrder}");

        try
        {
            await notificationService.PublishAsync([notification], ct);
            logger.LogInformation(
                "[BPReminder] Notified approver {UserId} - stage {Stage}, {Count} overdue BP(s): {Codes}",
                approver.Id, stageOrder, count, string.Join(", ", plans.Select(p => p.Code)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[BPReminder] Failed to send in-app notification to approver {UserId}", approver.Id);
        }

        if (string.IsNullOrWhiteSpace(approver.Email)) return;

        try
        {
            await emailService.SendAsync(new EmailMessage(
                approver.Email,
                approver.Fullname,
                count == 1
                    ? $"[WAMS] Budget Plan {plans[0].Code} Awaiting Your Approval (Stage {stageOrder}: {stageName})"
                    : $"[WAMS] {count} Budget Plans Awaiting Your Approval (Stage {stageOrder})",
                BuildEmailBody(approver, stageOrder, stageName, plans)), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[BPReminder] Failed to send email to approver {UserId}", approver.Id);
        }
    }

    private static string BuildEmailBody(User approver, int stageOrder, string stageName, List<BudgetPlan> plans)
    {
        var rows = string.Join("\n", plans.Select(p =>
        {
            var pendingSince = GetPendingSince(p, stageOrder);
            var hours = pendingSince.HasValue ? (int)(DateTime.UtcNow - pendingSince.Value).TotalHours : 0;
            return $"<tr><td style='padding:4px 8px'>{p.Code}</td><td style='padding:4px 8px'>{hours}h pending</td></tr>";
        }));

        return $"""
            <p>Dear {approver.Fullname},</p>
            <p>The following budget plan(s) are awaiting your <strong>stage {stageOrder} approval ({stageName})</strong>:</p>
            <table border="1" cellspacing="0" cellpadding="0" style="border-collapse:collapse;font-family:sans-serif;font-size:14px">
              <thead><tr style="background:#f0f0f0"><th style="padding:4px 8px">Budget Plan</th><th style="padding:4px 8px">Waiting</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p>Please log in to WAMS to review and take action.</p>
            <hr/>
            <p style="color:#888;font-size:12px;">This is an automated reminder from WAMS. Do not reply to this email.</p>
            """;
    }

    private static DateTime? GetPendingSince(BudgetPlan plan, int stageOrder)
    {
        if (stageOrder == 1) return plan.SubmittedAt;

        return plan.WorkflowInstance?.Stages
            .Where(s => s.StageOrder < stageOrder && s.Status == WorkflowStageStatus.Approved)
            .OrderByDescending(s => s.StageOrder)
            .Select(s => s.ApprovedAt)
            .FirstOrDefault();
    }

    private TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException)
        {
            logger.LogWarning(
                "[BPReminder] TimeZone '{Id}' not found - falling back to UTC. Check BudgetPlanReminder:TimeZoneId.", id);
            return TimeZoneInfo.Utc;
        }
    }
}
