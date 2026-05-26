import assert from "node:assert/strict";
import { test } from "vitest";
import { reportsModule } from "./reports/module.tsx";

test("reports module is labeled as a V2 preview in the finalized V1 app", () => {
  assert.equal(reportsModule.name, "Reports (V2 preview)");
  assert.equal(reportsModule.navigation[0].label, "Reports (V2 preview)");
});
