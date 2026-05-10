import {
  sampleActivities,
  sampleComponentRows,
  sampleReports,
  sampleTrendHeights,
  sampleUsers,
  sampleWorkflowActions
} from "../lib/sampleData";

export const themeUsers = sampleUsers;

export const themeActivities = sampleActivities.map((activity) => activity.summary);

export const themeReports = sampleReports;

export const themeTrendHeights = sampleTrendHeights;

export const themeWorkflowActions = sampleWorkflowActions;

export const componentRows = sampleComponentRows;
