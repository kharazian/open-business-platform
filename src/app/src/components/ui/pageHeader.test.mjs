import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, rmSync, symlinkSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-page-header-test";

rmSync(outDir, { recursive: true, force: true });
mkdirSync(outDir, { recursive: true });
symlinkSync(process.cwd() + "/node_modules", `${outDir}/node_modules`, "dir");
execFileSync(
  "npx",
  [
    "tsc",
    "src/components/ui/PageHeader.tsx",
    "src/components/ui/Badge.tsx",
    "src/lib/cn.ts",
    "src/context/useDesignTheme.ts",
    "src/context/AppThemeContext.tsx",
    "src/context/ThemeAppearanceContext.tsx",
    "src/config/themeLayoutModes.ts",
    "src/config/themePalettes.ts",
    "src/config/themeTokens.ts",
    "--ignoreConfig",
    "--jsx",
    "react-jsx",
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

const require = createRequire(import.meta.url);
const react = require("react");
const { renderToStaticMarkup } = require("react-dom/server");
const emittedHeaderPath = existsSync(`${outDir}/components/ui/PageHeader.js`)
  ? `${outDir}/components/ui/PageHeader.js`
  : `${outDir}/PageHeader.js`;
const { PageHeader } = require(emittedHeaderPath);

const markup = renderToStaticMarkup(
  react.createElement(PageHeader, {
    eyebrow: "Forms",
    title: "Form drafts",
    description: "Create and manage forms.",
    actions: react.createElement("button", null, "Create form")
  })
);

assert.equal(markup.includes("surface"), false, "PageHeader should not render as a large surface card.");
assert.equal(markup.includes("sm:text-4xl"), false, "PageHeader title should not use hero-scale responsive type.");
assert.equal(markup.includes("mt-4"), false, "PageHeader should avoid large vertical title offsets.");
assert.equal(markup.includes("border-b"), true, "PageHeader should separate content with a compact bottom rule.");
