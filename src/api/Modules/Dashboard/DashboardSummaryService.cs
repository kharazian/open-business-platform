using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public sealed class DashboardSummaryService
{
    private readonly OpenBusinessPlatformDbContext dbContext;

    public DashboardSummaryService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var userCount = await dbContext.Users
            .AsNoTracking()
            .CountAsync(user => user.IsActive, cancellationToken);
        var formCount = await dbContext.Forms
            .AsNoTracking()
            .CountAsync(form => !form.IsDeleted, cancellationToken);
        var recordCount = await dbContext.Records
            .AsNoTracking()
            .CountAsync(record => !record.IsDeleted && record.Status != RecordStatuses.Deleted, cancellationToken);
        var reportCount = await dbContext.Reports
            .AsNoTracking()
            .CountAsync(report => !report.IsDeleted, cancellationToken);
        var auditLogCount = await dbContext.AuditLogs
            .AsNoTracking()
            .CountAsync(cancellationToken);
        var recentAuditLogs = await dbContext.AuditLogs
            .AsNoTracking()
            .Include(auditLog => auditLog.User)
            .OrderByDescending(auditLog => auditLog.CreatedAt)
            .Take(5)
            .ToArrayAsync(cancellationToken);

        return new DashboardSummaryResponse(
            "Open Business Platform",
            new[]
            {
                new DashboardMetric("users", "Users", userCount),
                new DashboardMetric("forms", "Forms", formCount),
                new DashboardMetric("records", "Records", recordCount),
                new DashboardMetric("reports", "Reports", reportCount),
                new DashboardMetric("audit_logs", "Audit logs", auditLogCount)
            },
            recentAuditLogs.Select(ToActivityItem).ToArray());
    }

    private static DashboardActivityItem ToActivityItem(AuditLogEntry auditLog)
    {
        return new DashboardActivityItem(
            auditLog.Id,
            HumanizeAction(auditLog.Action),
            GetActorName(auditLog),
            auditLog.CreatedAt,
            "Completed");
    }

    private static string GetActorName(AuditLogEntry auditLog)
    {
        if (!string.IsNullOrWhiteSpace(auditLog.User?.Name))
        {
            return auditLog.User.Name;
        }

        if (!string.IsNullOrWhiteSpace(auditLog.User?.Email))
        {
            return auditLog.User.Email;
        }

        return "System";
    }

    private static string HumanizeAction(string action)
    {
        var text = action.Replace('_', ' ').Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(text))
        {
            return "Activity recorded";
        }

        return string.Concat(char.ToUpperInvariant(text[0]).ToString(), text[1..]);
    }
}
