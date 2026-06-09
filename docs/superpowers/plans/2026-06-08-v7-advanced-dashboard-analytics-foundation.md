# V7 Advanced Dashboard Analytics Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a typed, permission-checked dashboard analytics execution contract for summary, breakdown, trend, and table widgets without replacing the existing V2 chart preview and saved dashboard flows.

**Architecture:** Add a `DashboardAnalyticsService` inside the existing `Dashboard` backend module. The service validates a new dashboard-owned request contract, resolves source form/report permissions, filters records through `PermissionService`, sanitizes hidden fields, and maps V7 analytics requests onto the existing `ChartAggregationEngine` for the first slice. Frontend work is limited to API types/helper coverage.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core/Npgsql read queries, existing permission/report/form modules, React + TypeScript API helper tests, current executable backend test harness.

---

## File Structure

- Create `src/api/Modules/Dashboard/DashboardAnalyticsContracts.cs`
  - V7 request, source, metric, response, validation error, error response, and exception records.
- Create `src/api/Modules/Dashboard/DashboardAnalyticsRequestValidator.cs`
  - Pure validation for widget type, metric type, metric field, grouping field, date field, table columns, and limit.
- Create `src/api/Modules/Dashboard/DashboardAnalyticsService.cs`
  - Source form/report resolution, permission checks, hidden-field handling, record-scope filtering, and aggregation execution.
- Modify `src/api/Modules/Dashboard/DashboardEndpoints.cs`
  - Add `POST /api/dashboard/analytics/run`.
- Modify `src/api/Program.cs`
  - Register `DashboardAnalyticsService`.
- Modify `src/api.Tests/Program.cs`
  - Add contract, validator, service shape, and regression assertions.
- Modify `src/app/src/features/dashboards/types.ts`
  - Add dashboard analytics request/response types.
- Modify `src/app/src/features/dashboards/api.ts`
  - Add `runDashboardAnalytics`.
- Modify `src/app/src/features/dashboards/chartApi.test.mjs`
  - Cover the new API helper.
- Update docs/task files after implementation:
  - `docs/API_SPEC.md`
  - `docs/MASTER_PRD_FOR_AI.md`
  - `docs/REPORTS_AND_PRINTING.md`
  - `docs/ROADMAP.md`
  - `tasks/v7/001-advanced-dashboard-analytics-foundation.md`

---

### Task 1: Backend Contract And Validator RED

**Files:**

- Modify: `src/api.Tests/Program.cs`

- [ ] **Step 1: Add failing contract and validator assertions**

Add this block near the existing chart/dashboard assertions in `src/api.Tests/Program.cs`, after `chartResult` and before `tableChartResult`:

```csharp
var analyticsBreakdownRequest = new DashboardAnalyticsRequest(
    DashboardAnalyticsWidgetTypes.Breakdown,
    new DashboardAnalyticsSourceDefinition(sampleDepartmentId),
    new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Count),
    GroupByFieldId: "department",
    DateFieldId: null,
    Columns: Array.Empty<string>(),
    Limit: 5);
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsBreakdownRequest).Valid,
    "Dashboard analytics should validate grouped breakdown requests.");

var analyticsSummaryRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Summary,
    Metric = new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Sum, "salary"),
    GroupByFieldId = null
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsSummaryRequest).Valid,
    "Dashboard analytics should validate numeric summary metrics.");

var analyticsTrendRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Trend,
    DateFieldId = ReportableSystemFields.CreatedAt,
    GroupByFieldId = null
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsTrendRequest).Valid,
    "Dashboard analytics should validate date trend requests.");

var analyticsTableRequest = analyticsBreakdownRequest with
{
    WidgetType = DashboardAnalyticsWidgetTypes.Table,
    GroupByFieldId = null,
    Columns = new[] { "employee_name", ReportableSystemFields.Status }
};
AssertTrue(
    DashboardAnalyticsRequestValidator.Validate(reportingSchema, analyticsTableRequest).Valid,
    "Dashboard analytics should validate table slice requests.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsSummaryRequest with { Metric = new DashboardAnalyticsMetricDefinition(DashboardAnalyticsMetricTypes.Average, "department") }).Valid,
    "Dashboard analytics should reject non-numeric average metrics.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsBreakdownRequest with { GroupByFieldId = "salary" }).Valid,
    "Dashboard analytics should reject non-choice grouping fields.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsTrendRequest with { DateFieldId = "department" }).Valid,
    "Dashboard analytics should reject non-date trend fields.");

AssertFalse(
    DashboardAnalyticsRequestValidator.Validate(
        reportingSchema,
        analyticsTableRequest with { Columns = new[] { "missing_field" } }).Valid,
    "Dashboard analytics should reject unknown table columns.");

AssertTypeAssignable<object, DashboardAnalyticsService>();
```

- [ ] **Step 2: Run backend harness to verify RED**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: fails because `DashboardAnalyticsRequest`, `DashboardAnalyticsWidgetTypes`, `DashboardAnalyticsMetricTypes`, `DashboardAnalyticsRequestValidator`, and `DashboardAnalyticsService` do not exist.

---

### Task 2: Backend Contracts And Validator GREEN

**Files:**

- Create: `src/api/Modules/Dashboard/DashboardAnalyticsContracts.cs`
- Create: `src/api/Modules/Dashboard/DashboardAnalyticsRequestValidator.cs`
- Modify: `src/api.Tests/Program.cs`

- [ ] **Step 1: Add dashboard analytics contracts**

Create `src/api/Modules/Dashboard/DashboardAnalyticsContracts.cs`:

```csharp
namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardAnalyticsWidgetTypes
{
    public const string Summary = "summary";
    public const string Breakdown = "breakdown";
    public const string Trend = "trend";
    public const string Table = "table";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Summary,
        Breakdown,
        Trend,
        Table
    };
}

public static class DashboardAnalyticsMetricTypes
{
    public const string Count = "count";
    public const string Sum = "sum";
    public const string Average = "average";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Count,
        Sum,
        Average
    };
}

public sealed record DashboardAnalyticsSourceDefinition(Guid FormId, Guid? ReportId = null);

public sealed record DashboardAnalyticsMetricDefinition(string Type, string? FieldId = null);

public sealed record DashboardAnalyticsRequest(
    string WidgetType,
    DashboardAnalyticsSourceDefinition Source,
    DashboardAnalyticsMetricDefinition Metric,
    string? GroupByFieldId = null,
    string? DateFieldId = null,
    IReadOnlyList<string>? Columns = null,
    int? Limit = null);

public sealed record DashboardAnalyticsResponse(
    Guid FormId,
    string FormName,
    Guid? ReportId,
    string WidgetType,
    DashboardAnalyticsMetricDefinition Metric,
    IReadOnlyList<ChartSeriesPointDto> Series,
    IReadOnlyList<ChartTableColumnDto> Columns,
    IReadOnlyList<ChartTableRowDto> Rows,
    long TotalCount);

public sealed record DashboardAnalyticsValidationError(string Path, string Code, string Message);

public sealed record DashboardAnalyticsValidationResult(IReadOnlyList<DashboardAnalyticsValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record DashboardAnalyticsErrorResponse(
    string Message,
    IReadOnlyList<DashboardAnalyticsValidationError>? Errors = null);

public sealed class DashboardAnalyticsException : Exception
{
    public DashboardAnalyticsException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<DashboardAnalyticsValidationError>();
    }

    public DashboardAnalyticsException(int statusCode, string message, IReadOnlyList<DashboardAnalyticsValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<DashboardAnalyticsValidationError> Errors { get; }
}
```

- [ ] **Step 2: Add request validator**

Create `src/api/Modules/Dashboard/DashboardAnalyticsRequestValidator.cs`:

```csharp
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboard;

public static class DashboardAnalyticsRequestValidator
{
    public static DashboardAnalyticsValidationResult Validate(FormSchemaDefinition schema, DashboardAnalyticsRequest? request)
    {
        var errors = new List<DashboardAnalyticsValidationError>();

        if (request is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("request", "dashboard.analytics.request.required", "Dashboard analytics request is required."));
            return new DashboardAnalyticsValidationResult(errors);
        }

        if (request.Source is null || request.Source.FormId == Guid.Empty)
        {
            errors.Add(new DashboardAnalyticsValidationError("source.formId", "dashboard.analytics.source.form_required", "Source form is required."));
        }

        var fieldsById = FormReportableFieldMetadata.GetReportableFieldsById(schema);
        var widgetType = Normalize(request.WidgetType);
        var metricType = Normalize(request.Metric?.Type);
        var limit = request.Limit ?? 10;

        if (!DashboardAnalyticsWidgetTypes.Supported.Contains(widgetType))
        {
            errors.Add(new DashboardAnalyticsValidationError("widgetType", "dashboard.analytics.widget_type.unsupported", "Choose a supported dashboard analytics widget type."));
        }

        if (!DashboardAnalyticsMetricTypes.Supported.Contains(metricType))
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.type", "dashboard.analytics.metric.unsupported", "Choose a supported dashboard analytics metric."));
        }

        if (limit is < 1 or > 50)
        {
            errors.Add(new DashboardAnalyticsValidationError("limit", "dashboard.analytics.limit.range", "Limit must be between 1 and 50."));
        }

        ValidateMetricField(request, fieldsById, errors);
        ValidateWidgetFields(request, fieldsById, errors);

        return new DashboardAnalyticsValidationResult(errors);
    }

    private static void ValidateMetricField(
        DashboardAnalyticsRequest request,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var metricType = Normalize(request.Metric?.Type);
        var fieldId = NormalizeOptional(request.Metric?.FieldId);

        if (metricType == DashboardAnalyticsMetricTypes.Count)
        {
            return;
        }

        if (fieldId is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.fieldId", "dashboard.analytics.metric.field_required", "Sum and average metrics require a numeric field."));
            return;
        }

        if (!fieldsById.TryGetValue(fieldId, out var field) || !field.SupportsAggregation)
        {
            errors.Add(new DashboardAnalyticsValidationError("metric.fieldId", "dashboard.analytics.metric.field_invalid", "Metric field must be a reportable numeric field."));
        }
    }

    private static void ValidateWidgetFields(
        DashboardAnalyticsRequest request,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        switch (Normalize(request.WidgetType))
        {
            case DashboardAnalyticsWidgetTypes.Breakdown:
                ValidateGroupField(request.GroupByFieldId, fieldsById, errors);
                break;
            case DashboardAnalyticsWidgetTypes.Trend:
                ValidateDateField(request.DateFieldId, fieldsById, errors);
                break;
            case DashboardAnalyticsWidgetTypes.Table:
                ValidateColumns(request.Columns, fieldsById, errors);
                break;
        }
    }

    private static void ValidateGroupField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("groupByFieldId", "dashboard.analytics.group.field_required", "Breakdown widgets require a grouping field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || !field.SupportsChoiceGrouping)
        {
            errors.Add(new DashboardAnalyticsValidationError("groupByFieldId", "dashboard.analytics.group.field_invalid", "Grouping field must be a status or choice field."));
        }
    }

    private static void ValidateDateField(
        string? fieldId,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        var normalized = NormalizeOptional(fieldId);

        if (normalized is null)
        {
            errors.Add(new DashboardAnalyticsValidationError("dateFieldId", "dashboard.analytics.date.field_required", "Trend widgets require a date field."));
            return;
        }

        if (!fieldsById.TryGetValue(normalized, out var field) || field.Type is not (FormFieldTypes.Date or "datetime"))
        {
            errors.Add(new DashboardAnalyticsValidationError("dateFieldId", "dashboard.analytics.date.field_invalid", "Trend field must be a date field."));
        }
    }

    private static void ValidateColumns(
        IReadOnlyList<string>? columns,
        IReadOnlyDictionary<string, ReportableFieldMetadata> fieldsById,
        ICollection<DashboardAnalyticsValidationError> errors)
    {
        foreach (var fieldId in columns ?? Array.Empty<string>())
        {
            if (!fieldsById.ContainsKey(fieldId.Trim()))
            {
                errors.Add(new DashboardAnalyticsValidationError("columns", "dashboard.analytics.columns.field_invalid", "Table columns must use reportable fields."));
            }
        }
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
```

- [ ] **Step 3: Run backend harness to verify validator GREEN**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: still fails only because `DashboardAnalyticsService` is missing.

---

### Task 3: Backend Analytics Service And Endpoint

**Files:**

- Create: `src/api/Modules/Dashboard/DashboardAnalyticsService.cs`
- Modify: `src/api/Modules/Dashboard/DashboardEndpoints.cs`
- Modify: `src/api/Program.cs`
- Modify: `src/api.Tests/Program.cs`

- [ ] **Step 1: Add service shape assertions**

Add these assertions after `AssertTypeAssignable<object, DashboardAnalyticsService>();` in `src/api.Tests/Program.cs`:

```csharp
AssertNotNull(
    typeof(DashboardAnalyticsService).GetMethod(nameof(DashboardAnalyticsService.RunAsync))?.GetParameters().FirstOrDefault(parameter => parameter.ParameterType == typeof(ClaimsPrincipal)),
    "Dashboard analytics execution should receive the current principal.");
var analyticsSourceReportConfig = typeof(DashboardAnalyticsService).GetMethod(
    "GetSourceReportConfigAsync",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
AssertNotNull(analyticsSourceReportConfig, "Dashboard analytics should resolve source report configs through a dedicated helper.");
var analyticsSourceReportConfigParameters = analyticsSourceReportConfig!.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
AssertTrue(
    analyticsSourceReportConfigParameters.Contains(typeof(ClaimsPrincipal))
        && analyticsSourceReportConfigParameters.Contains(typeof(PermissionService)),
    "Dashboard analytics source report configs should receive the current principal and permission service for report-level checks.");
```

- [ ] **Step 2: Create dashboard analytics service**

Create `src/api/Modules/Dashboard/DashboardAnalyticsService.cs`. Use the existing `ChartAggregationService` as the permission/report-resolution pattern. The service should:

- Load form and schema.
- Check form `view` access.
- Validate with `DashboardAnalyticsRequestValidator`.
- Check hidden direct references.
- Resolve optional source report config with report `view` access.
- Apply record access through `PermissionService.ApplyRecordAccessAsync`.
- Map the V7 request to `ChartWidgetConfigDefinition`.
- Return `DashboardAnalyticsResponse`.

Keep these method names stable because the harness asserts them:

```csharp
public Task<DashboardAnalyticsResponse> RunAsync(
    ClaimsPrincipal principal,
    DashboardAnalyticsRequest request,
    PermissionService permissionService,
    CancellationToken cancellationToken)
```

```csharp
private Task<ListReportConfigDefinition?> GetSourceReportConfigAsync(
    ClaimsPrincipal principal,
    PermissionService permissionService,
    Guid formId,
    Guid? reportId,
    FormSchemaDefinition schema,
    IReadOnlySet<string> hiddenFieldIds,
    CancellationToken cancellationToken)
```

Map widget types as:

```csharp
private static string ToChartWidgetType(string widgetType)
{
    return widgetType.Trim() switch
    {
        DashboardAnalyticsWidgetTypes.Summary => ChartWidgetTypes.NumberCard,
        DashboardAnalyticsWidgetTypes.Breakdown => ChartWidgetTypes.ChoiceBreakdown,
        DashboardAnalyticsWidgetTypes.Trend => ChartWidgetTypes.DateTrend,
        DashboardAnalyticsWidgetTypes.Table => ChartWidgetTypes.Table,
        _ => widgetType
    };
}
```

Map metric types as:

```csharp
private static ChartMetricDefinition ToChartMetric(DashboardAnalyticsMetricDefinition metric)
{
    return new ChartMetricDefinition(metric.Type.Trim(), NormalizeOptional(metric.FieldId));
}
```

Build the response from the chart result:

```csharp
return new DashboardAnalyticsResponse(
    chartResult.FormId,
    chartResult.FormName,
    sanitizedRequest.Source.ReportId,
    sanitizedRequest.WidgetType.Trim(),
    sanitizedRequest.Metric,
    chartResult.Series,
    chartResult.Columns,
    chartResult.Rows,
    chartResult.TotalCount);
```

- [ ] **Step 3: Register service**

In `src/api/Program.cs`, add near the existing dashboard registrations:

```csharp
builder.Services.AddScoped<DashboardAnalyticsService>();
```

- [ ] **Step 4: Add endpoint**

In `src/api/Modules/Dashboard/DashboardEndpoints.cs`, add this endpoint inside the `/api/dashboard` group:

```csharp
group.MapPost("/analytics/run", async (
    DashboardAnalyticsRequest request,
    DashboardAnalyticsService dashboardAnalytics,
    PermissionService permissionService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!await permissionService.CanAsync(httpContext.User, PlatformPermissions.Menu.Dashboard, cancellationToken))
    {
        return Results.Forbid();
    }

    return await HandleDashboardAnalyticsRequestAsync(async () =>
    {
        var result = await dashboardAnalytics.RunAsync(httpContext.User, request, permissionService, cancellationToken);
        return Results.Ok(result);
    });
});
```

Add a handler beside `HandleChartRequestAsync`:

```csharp
private static async Task<IResult> HandleDashboardAnalyticsRequestAsync(Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (DashboardAnalyticsException exception)
    {
        var errors = exception.Errors.Count == 0 ? null : exception.Errors;
        return Results.Json(new DashboardAnalyticsErrorResponse(exception.Message, errors), statusCode: exception.StatusCode);
    }
}
```

- [ ] **Step 5: Run backend harness**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: PASS for contract/service shape and existing chart/dashboard assertions.

---

### Task 4: Backend Behavior Tests

**Files:**

- Modify: `src/api.Tests/Program.cs`
- Modify: `src/api/Modules/Dashboard/DashboardAnalyticsService.cs`
- Modify: `src/api/Modules/Dashboard/DashboardAnalyticsRequestValidator.cs`

- [ ] **Step 1: Add engine-level analytics response assertions**

Add a pure engine mapping assertion after `tableChartResult` in `src/api.Tests/Program.cs`:

```csharp
var analyticsChartConfig = new ChartWidgetConfigDefinition(
    ChartWidgetTypes.DateTrend,
    new ChartMetricDefinition(ChartMetricTypes.Count),
    GroupByFieldId: null,
    DateFieldId: ReportableSystemFields.CreatedAt,
    Columns: Array.Empty<string>(),
    Limit: 5,
    ReportId: null);
var analyticsTrendPreview = ChartAggregationEngine.Execute(
    sampleDepartmentId,
    "Employee information",
    analyticsChartConfig,
    reportingSchema,
    executionRecords.Where(record => record.Status == RecordStatuses.Active).ToArray());
AssertEqual(2, analyticsTrendPreview.TotalCount, "Dashboard analytics trend execution should count permitted active records.");
AssertTrue(analyticsTrendPreview.Series.Count > 0, "Dashboard analytics trend execution should return date series points.");
```

- [ ] **Step 2: Add validation error code assertions**

Add assertions that error codes remain stable:

```csharp
var invalidAnalytics = DashboardAnalyticsRequestValidator.Validate(
    reportingSchema,
    analyticsBreakdownRequest with { Limit = 100 });
AssertEqual(
    "dashboard.analytics.limit.range",
    invalidAnalytics.Errors.Single().Code,
    "Dashboard analytics limit errors should have stable structured codes.");
```

- [ ] **Step 3: Ensure hidden-field handling is explicit**

In `DashboardAnalyticsService`, implement a private `EnsureVisibleRequest` helper that throws `DashboardAnalyticsException` with `StatusCodes.Status403Forbidden` when a direct metric, group, date, or table column field is hidden. Use this message:

```txt
Dashboard analytics request references a hidden field.
```

Direct hidden table columns must be rejected, because a table request explicitly asks for those fields. Saved source report sanitization remains separate: hidden fields are removed from source report columns, filters, and sorts before execution.

- [ ] **Step 4: Run backend harness**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: PASS.

---

### Task 5: Frontend API Types And Helper

**Files:**

- Modify: `src/app/src/features/dashboards/types.ts`
- Modify: `src/app/src/features/dashboards/api.ts`
- Modify: `src/app/src/features/dashboards/chartApi.test.mjs`

- [ ] **Step 1: Add frontend analytics types**

In `types.ts`, add after the chart preview types:

```ts
export const dashboardAnalyticsWidgetTypes = ["summary", "breakdown", "trend", "table"] as const;
export const dashboardAnalyticsMetricTypes = ["count", "sum", "average"] as const;

export type DashboardAnalyticsWidgetType = (typeof dashboardAnalyticsWidgetTypes)[number];
export type DashboardAnalyticsMetricType = (typeof dashboardAnalyticsMetricTypes)[number];

export type DashboardAnalyticsSource = {
  formId: EntityId;
  reportId?: EntityId | null;
};

export type DashboardAnalyticsMetric = {
  type: DashboardAnalyticsMetricType;
  fieldId?: string | null;
};

export type DashboardAnalyticsRequest = {
  widgetType: DashboardAnalyticsWidgetType;
  source: DashboardAnalyticsSource;
  metric: DashboardAnalyticsMetric;
  groupByFieldId?: string | null;
  dateFieldId?: string | null;
  columns?: string[] | null;
  limit?: number | null;
};

export type DashboardAnalyticsResponse = {
  formId: EntityId;
  formName: string;
  reportId?: EntityId | null;
  widgetType: DashboardAnalyticsWidgetType;
  metric: DashboardAnalyticsMetric;
  series: ChartSeriesPoint[];
  columns: ChartTableColumn[];
  rows: ChartTableRow[];
  totalCount: number;
};
```

- [ ] **Step 2: Add API helper**

In `api.ts`, import the new types and add:

```ts
export async function runDashboardAnalytics(
  request: DashboardAnalyticsRequest,
  fetcher: DashboardFetcher = defaultFetcher
): Promise<DashboardAnalyticsResponse> {
  return requestJson<DashboardAnalyticsResponse>(
    "/api/dashboard/analytics/run",
    {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    },
    fetcher
  );
}
```

- [ ] **Step 3: Add frontend API test**

Append to `chartApi.test.mjs`:

```js
test("dashboard analytics API client maps run requests and errors", async () => {
  const calls = [];
  const request = {
    widgetType: "breakdown",
    source: { formId: "form-2", reportId: null },
    metric: { type: "count", fieldId: null },
    groupByFieldId: "status",
    dateFieldId: null,
    columns: [],
    limit: 10
  };
  const fetcher = async (input, init = {}) => {
    calls.push({ input, init });

    if (input === "/api/dashboard/analytics/run" && init.method === "POST") {
      return {
        ok: true,
        json: async () => ({
          formId: "form-2",
          formName: "Employee information",
          reportId: null,
          widgetType: "breakdown",
          metric: request.metric,
          columns: [],
          series: [{ key: "active", label: "Active", value: 2 }],
          rows: [],
          totalCount: 2
        })
      };
    }

    return { ok: false, json: async () => ({ message: "Unexpected request." }) };
  };

  const result = await api.runDashboardAnalytics(request, fetcher);

  assert.equal(result.widgetType, "breakdown");
  assert.equal(result.series[0].label, "Active");
  assert.equal(calls[0].input, "/api/dashboard/analytics/run");
  assert.equal(calls[0].init.method, "POST");
  assert.equal(calls[0].init.credentials, "include");
  assert.equal(calls[0].init.headers["Content-Type"], "application/json");
  assert.deepEqual(JSON.parse(calls[0].init.body), request);

  await assert.rejects(
    () =>
      api.runDashboardAnalytics(request, async () => ({
        ok: false,
        json: async () => ({ message: "Dashboard analytics request is invalid." })
      })),
    (error) => {
      assert.equal(error.name, "DashboardApiError");
      assert.equal(error.message, "Dashboard analytics request is invalid.");
      return true;
    }
  );
});
```

- [ ] **Step 4: Run frontend tests**

Run:

```bash
cd src/app
npm test
```

Expected: PASS.

---

### Task 6: Documentation, Verification, And Commit

**Files:**

- Modify: `docs/API_SPEC.md`
- Modify: `docs/MASTER_PRD_FOR_AI.md`
- Modify: `docs/REPORTS_AND_PRINTING.md`
- Modify: `docs/ROADMAP.md`
- Modify: `tasks/v7/001-advanced-dashboard-analytics-foundation.md`

- [ ] **Step 1: Document the implemented endpoint**

In `docs/API_SPEC.md`, add a section after "Preview chart widget" with this content:

### Run dashboard analytics

`POST /api/dashboard/analytics/run`

Requires authentication, `menu.dashboard`, source form `view` access, and source report `view` access when `source.reportId` is supplied. Record rows are filtered through the user's V3 form record scope. Hidden fields cannot be directly selected and are removed from saved report source filters, sorts, and columns before execution.

Request:

```json
{
  "widgetType": "breakdown",
  "source": {
    "formId": "00000000-0000-0000-0000-000000000000",
    "reportId": null
  },
  "metric": {
    "type": "count",
    "fieldId": null
  },
  "groupByFieldId": "status",
  "dateFieldId": null,
  "columns": [],
  "limit": 10
}
```

Supported `widgetType` values are `summary`, `breakdown`, `trend`, and `table`. Supported metric types are `count`, `sum`, and `average`.

Response:

```json
{
  "formId": "00000000-0000-0000-0000-000000000000",
  "formName": "Employee information",
  "reportId": null,
  "widgetType": "breakdown",
  "metric": { "type": "count", "fieldId": null },
  "series": [
    { "key": "active", "label": "Active", "value": 10 }
  ],
  "columns": [],
  "rows": [],
  "totalCount": 10
}
```

- [ ] **Step 2: Mark task acceptance criteria**

In `tasks/v7/001-advanced-dashboard-analytics-foundation.md`, mark completed criteria with `[x]` only after verification passes.

- [ ] **Step 3: Run full verification**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
dotnet build src/api/OpenBusinessPlatform.Api.csproj
cd src/app
npm test
npm run build
git diff --check
```

Expected: all commands exit `0`.

- [ ] **Step 4: Commit**

Run:

```bash
git add src/api src/api.Tests src/app docs tasks/v7
git commit -m "feat: add dashboard analytics foundation"
```

Expected: commit succeeds with only V7 Task 001 files and docs.
