import assert from "node:assert/strict";
import { test } from "vitest";
import { defaultAppThemeSettings } from "./AppThemeContext.tsx";

test("new app users start in light mode with the sidebar collapsed", () => {
  assert.equal(defaultAppThemeSettings.colorMode, "light");
  assert.equal(defaultAppThemeSettings.layout, "collapsed-sidebar");
});
