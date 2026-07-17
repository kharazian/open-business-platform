import assert from "node:assert/strict";
import { test } from "vitest";
import { createCustomDomain, mutateCustomDomain } from "./api.ts";

const domain = { id: "domain-1", hostname: "app.example.com", status: "pending", isEnabled: false, verificationRecordName: "_obp-verification.app.example.com", verificationRecordValue: "obp-verification=test", verifiedAt: null, lastCheckedAt: null, lastFailure: null, concurrencyStamp: "stamp" };
test("custom-domain lifecycle requests carry hostname and concurrency state", async () => {
  const calls = []; const fetcher = async (input, init) => { calls.push({ input, body: JSON.parse(String(init.body)) }); return new Response(JSON.stringify(domain), { status: 200 }); };
  await createCustomDomain("app.example.com", fetcher); await mutateCustomDomain(domain, "check", fetcher);
  assert.equal(calls[0].body.hostname, "app.example.com"); assert.equal(calls[1].input, "/api/custom-domains/domain-1/check"); assert.equal(calls[1].body.concurrencyStamp, "stamp");
});
