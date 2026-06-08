using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Printing;

public static class PrintTemplateAuthorization
{
    public static async Task<IReadOnlyCollection<PrintTemplateSummaryDto>> FilterViewableTemplatesAsync(
        IReadOnlyCollection<PrintTemplateSummaryDto> templates,
        Func<Guid, CancellationToken, Task<bool>> canViewReportAsync,
        CancellationToken cancellationToken)
    {
        var viewableTemplates = new List<PrintTemplateSummaryDto>();

        foreach (var template in templates)
        {
            if (!IsReportTemplate(template.Type))
            {
                viewableTemplates.Add(template);
                continue;
            }

            if (template.ReportId is Guid reportId && await canViewReportAsync(reportId, cancellationToken))
            {
                viewableTemplates.Add(template);
            }
        }

        return viewableTemplates;
    }

    public static async Task<bool> CanViewTemplateAsync(
        PrintTemplateDetailDto template,
        Func<Guid, CancellationToken, Task<bool>> canViewReportAsync,
        CancellationToken cancellationToken)
    {
        return !IsReportTemplate(template.Type)
            || template.ReportId is Guid reportId && await canViewReportAsync(reportId, cancellationToken);
    }

    public static async Task<bool> CanManageTemplateAsync(
        PrintTemplateDetailDto template,
        Func<Guid, CancellationToken, Task<bool>> canManageReportAsync,
        CancellationToken cancellationToken)
    {
        return !IsReportTemplate(template.Type)
            || template.ReportId is Guid reportId && await canManageReportAsync(reportId, cancellationToken);
    }

    public static async Task<bool> CanManageRequestedReportTemplateAsync(
        string? templateType,
        Guid? reportId,
        Func<Guid, CancellationToken, Task<bool>> canManageReportAsync,
        CancellationToken cancellationToken)
    {
        return !IsReportTemplate(templateType)
            || reportId is null
            || await canManageReportAsync(reportId.Value, cancellationToken);
    }

    private static bool IsReportTemplate(string? templateType)
    {
        return string.Equals(templateType?.Trim(), PrintTemplateTypes.Report, StringComparison.Ordinal);
    }
}
