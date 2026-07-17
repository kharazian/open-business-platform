import assert from "node:assert/strict";
import { test } from "vitest";
import { getPublicBranding, saveCurrentBranding } from "./api.ts";

const branding = { appName: "Acme", logoText: "AC", logoDataUrl: null, primaryColor: "#123456", loginMessage: "Welcome", concurrencyStamp: "stamp" };

test("public branding lookup encodes tenant and workspace slugs", async () => {
  let requested = "";
  const result = await getPublicBranding("acme tenant", "main/workspace", async (input) => {
    requested = input;
    return new Response(JSON.stringify(branding), { status: 200 });
  });
  assert.equal(requested, "/api/branding/public?tenant=acme+tenant&workspace=main%2Fworkspace");
  assert.equal(result.appName, "Acme");
});

test("branding updates send concurrency state and expose API errors", async () => {
  let body = "";
  await saveCurrentBranding(branding, async (_input, init) => {
    body = String(init?.body);
    return new Response(JSON.stringify(branding), { status: 200 });
  });
  assert.equal(JSON.parse(body).concurrencyStamp, "stamp");

  await assert.rejects(
    () => saveCurrentBranding(branding, async () => new Response(JSON.stringify({ message: "Conflict" }), { status: 409 })),
    /Conflict/
  );
});
