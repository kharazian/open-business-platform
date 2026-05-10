import {
  sampleActivities,
  sampleComponentRows,
  sampleAuditLogs,
  samplePermissions,
  sampleReports,
  sampleRoles,
  sampleTrendHeights,
  sampleUsers,
  sampleWorkflowActions
} from "../lib/sampleData";

export const themeUsers = sampleUsers;

export const themeActivities = sampleActivities.map((activity) => activity.summary);

export const themeReports = sampleReports;

export const themeRoles = sampleRoles;

export const themePermissions = samplePermissions;

export const themeAuditLogs = sampleAuditLogs;

export const themeTrendHeights = sampleTrendHeights;

export const themeWorkflowActions = sampleWorkflowActions;

export const componentRows = sampleComponentRows;
