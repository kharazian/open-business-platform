import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, rmSync, symlinkSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-theme-header-action-test";

rmSync(outDir, { recursive: true, force: true });
mkdirSync(outDir, { recursive: true });
symlinkSync(process.cwd() + "/node_modules", `${outDir}/node_modules`, "dir");
execFileSync(
  "npx",
  [
    "tsc",
    "src/theme/components/ThemeHeaderAction.tsx",
    "src/components/ui/Button.tsx",
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
const emittedActionPath = existsSync(`${outDir}/theme/components/ThemeHeaderAction.js`)
  ? `${outDir}/theme/components/ThemeHeaderAction.js`
  : `${outDir}/ThemeHeaderAction.js`;
const { ThemeHeaderAction } = require(emittedActionPath);

function TestIcon(props) {
  return react.createElement("svg", props);
}

const markup = renderToStaticMarkup(
  react.createElement(
    ThemeHeaderAction,
    {
      icon: TestIcon
    },
    "Create item"
  )
);

assert.equal(markup.includes("Create item"), true, "ThemeHeaderAction should render the action label.");
assert.equal(markup.includes("size-5"), true, "ThemeHeaderAction should render larger header icons than compact buttons.");
assert.equal(markup.includes("aria-hidden=\"true\""), true, "ThemeHeaderAction icons should be hidden from screen readers.");
