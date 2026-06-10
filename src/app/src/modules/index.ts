import { dashboardModule } from "./dashboard/module";
import { formsModule } from "./forms/module";
import { integrationsModule } from "./integrations/module";
import { notificationsModule } from "./notifications/module";
import { printingModule } from "./printing/module";
import { profileModule } from "./profile/module";
import { reportsModule } from "./reports/module";
import { settingsModule } from "./settings/module";
import { triggersModule } from "./triggers/module";
import { usersModule } from "./users/module";
import { workflowsModule } from "./workflows/module";

export const platformModules = [
  dashboardModule,
  formsModule,
  usersModule,
  reportsModule,
  printingModule,
  triggersModule,
  workflowsModule,
  integrationsModule,
  notificationsModule,
  settingsModule,
  profileModule
];
