import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, rmSync, symlinkSync } from "node:fs";
import { createRequire } from "node:module";

const outDir = "/tmp/obp-modal-test";

rmSync(outDir, { recursive: true, force: true });
mkdirSync(outDir, { recursive: true });
symlinkSync(process.cwd() + "/node_modules", `${outDir}/node_modules`, "dir");
execFileSync(
  "npx",
  [
    "tsc",
    "src/components/ui/Modal.tsx",
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
const emittedModalPath = existsSync(`${outDir}/components/ui/Modal.js`) ? `${outDir}/components/ui/Modal.js` : `${outDir}/Modal.js`;
const { Modal } = require(emittedModalPath);

const markup = renderToStaticMarkup(
  react.createElement(
    Modal,
    {
      open: true,
      title: "Create user",
      description: "Create a local account and assign one or more roles.",
      onClose: () => undefined
    },
    react.createElement("div", null, "Form fields")
  )
);

assert.equal(markup.includes("aria-label=\"Close modal\""), true, "Modal should expose a labeled close control.");
assert.equal(markup.includes("size-10 p-0"), true, "Modal close control should use the shared icon button size.");
assert.equal(markup.includes("size-5"), true, "Modal close icon should be large enough to read in the header.");
