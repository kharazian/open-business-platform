import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, rmSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-record-print-test";

rmSync(outDir, { recursive: true, force: true });
execFileSync(
  "npx",
  [
    "tsc",
    "src/features/forms/types.ts",
    "src/features/forms/api.ts",
    "src/features/records/recordPrint.ts",
    "--ignoreConfig",
    "--target",
    "ES2022",
    "--module",
    "CommonJS",
    "--moduleResolution",
    "Node",
    "--ignoreDeprecations",
    "6.0",
    "--outDir",
    outDir,
    "--skipLibCheck",
    "--strict"
  ],
  { stdio: "inherit" }
);

const emittedPath = existsSync(`${outDir}/features/records/recordPrint.js`)
  ? `${outDir}/features/records/recordPrint.js`
  : existsSync(`${outDir}/records/recordPrint.js`)
    ? `${outDir}/records/recordPrint.js`
    : `${outDir}/recordPrint.js`;
const require = createRequire(import.meta.url);
const print = require(emittedPath);

let printCalls = 0;
print.requestBrowserPrint(() => {
  printCalls += 1;
});

assert.equal(printCalls, 1);
assert.equal(print.getRecordListPrintDescription(12, 2, 3, "North plant"), "12 total records | Page 2 of 3 | Filter: North plant");
assert.equal(print.getRecordListPrintDescription(1, 1, 1, ""), "1 total record | Page 1 of 1");
assert.equal(print.getRecordDetailPrintDescription("May 19, 2026, 1:20 PM", "version-1"), "Submitted May 19, 2026, 1:20 PM | Version version-1");
