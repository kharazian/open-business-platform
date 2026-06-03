import assert from "node:assert/strict";
import { test } from "vitest";
import { applyNotificationBadge, formatNotificationBadge } from "./navigationBadge.ts";

test("notification badge helper formats and applies notification nav badges", () => {
  assert.equal(formatNotificationBadge(0), null);
  assert.equal(formatNotificationBadge(9), "9");
  assert.equal(formatNotificationBadge(128), "99+");

  const navigation = [
    { label: "Dashboard", path: "/" },
    { label: "Notifications", path: "/notifications" },
    { label: "Settings", path: "/settings" }
  ];

  const hiddenNavigation = applyNotificationBadge(navigation, 4, false);
  assert.equal(hiddenNavigation[1].badge, undefined);

  const visibleNavigation = applyNotificationBadge(navigation, 4, true);
  assert.equal(visibleNavigation[1].badge, "4");
  assert.equal(visibleNavigation[0], navigation[0]);
  assert.notEqual(visibleNavigation[1], navigation[1]);

  const cappedNavigation = applyNotificationBadge(navigation, 128, true);
  assert.equal(cappedNavigation[1].badge, "99+");
});
