import { dashboardModule } from "./dashboard/module";
import { profileModule } from "./profile/module";
import { reportsModule } from "./reports/module";
import { settingsModule } from "./settings/module";
import { usersModule } from "./users/module";

export const platformModules = [
  dashboardModule,
  usersModule,
  reportsModule,
  settingsModule,
  profileModule
];
