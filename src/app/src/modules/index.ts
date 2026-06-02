import { dashboardModule } from "./dashboard/module";
import { formsModule } from "./forms/module";
import { profileModule } from "./profile/module";
import { reportsModule } from "./reports/module";
import { settingsModule } from "./settings/module";
import { triggersModule } from "./triggers/module";
import { usersModule } from "./users/module";

export const platformModules = [
  dashboardModule,
  formsModule,
  usersModule,
  reportsModule,
  triggersModule,
  settingsModule,
  profileModule
];
