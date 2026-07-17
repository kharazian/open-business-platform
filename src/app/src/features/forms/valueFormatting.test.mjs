import assert from "node:assert/strict";
import { test } from "vitest";
import { formatAddressValue, formatFormRecordValue, normalizeAddressValue } from "./valueFormatting.ts";

test("address values have stable display formatting", () => {
  const value = { line1: "100 King Street West", city: "Toronto", region: "ON", postalCode: "M5X 1A9", country: "Canada" };
  assert.equal(formatAddressValue(value), "100 King Street West, Toronto, ON, M5X 1A9, Canada");
  assert.equal(formatFormRecordValue(value), "100 King Street West, Toronto, ON, M5X 1A9, Canada");
  assert.equal(formatAddressValue({ latitude: 43.648, longitude: -79.381 }), "43.648, -79.381");
  assert.deepEqual(normalizeAddressValue({ line1: "100 King", unknown: "discard" }), { line1: "100 King" });
});
