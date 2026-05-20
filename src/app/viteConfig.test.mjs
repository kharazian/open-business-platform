import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const viteConfig = readFileSync("vite.config.ts", "utf8");

assert.match(viteConfig, /VITE_APP_HOST/, "Vite config should read the local dev host from env.");
assert.match(viteConfig, /host:\s*appHost/, "Vite dev server should use the env-derived host.");
assert.match(viteConfig, /strictPort:\s*true/, "Vite dev server should fail clearly when the configured port is busy.");
