import {
  sampleActivities,
  sampleCalendarEvents,
  sampleComponentRows,
  sampleAuditLogs,
  sampleFormFields,
  sampleInvoices,
  sampleNotifications,
  samplePermissions,
  sampleReports,
  sampleRoles,
  sampleTableViews,
  sampleTasks,
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

export const themeNotifications = sampleNotifications;

export const themeCalendarEvents = sampleCalendarEvents;

export const themeTasks = sampleTasks;

export const themeInvoices = sampleInvoices;

export const themeFormFields = sampleFormFields;

export const themeTableViews = sampleTableViews;

export const themeTrendHeights = sampleTrendHeights;

export const themeWorkflowActions = sampleWorkflowActions;

export const componentRows = sampleComponentRows;
