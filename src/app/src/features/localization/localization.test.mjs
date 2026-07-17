import assert from "node:assert/strict";
import { test } from "vitest";
import { saveUserLocalization, saveWorkspaceLocalization } from "./api.ts";

const settings = { workspace: { defaultLocale: "en-CA", defaultTimeZone: "UTC", firstDayOfWeek: 1, concurrencyStamp: "workspace-stamp" }, user: { locale: null, timeZone: null, concurrencyStamp: null }, effectiveLocale: "en-CA", effectiveTimeZone: "UTC" };

test("workspace and user localization writes remain separate", async () => {
  const calls = [];
  const fetcher = async (input, init) => { calls.push({ input, body: JSON.parse(String(init.body)) }); return new Response(JSON.stringify(settings), { status: 200 }); };
  await saveWorkspaceLocalization(settings.workspace, fetcher);
  await saveUserLocalization(settings.user, fetcher);
  assert.equal(calls[0].input, "/api/localization/workspace");
  assert.equal(calls[0].body.firstDayOfWeek, 1);
  assert.equal(calls[1].input, "/api/localization/me");
  assert.equal(calls[1].body.locale, null);
});
