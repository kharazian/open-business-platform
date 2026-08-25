import assert from "node:assert/strict";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { test } from "vitest";
import { Modal } from "./Modal.tsx";

test("Modal exposes an accessible compact icon close button", () => {
  const markup = renderToStaticMarkup(
    React.createElement(
      Modal,
      {
        open: true,
        title: "Create user",
        description: "Create a local account and assign one or more roles.",
        onClose: () => undefined
      },
      React.createElement("div", null, "Form fields")
    )
  );

  assert.equal(markup.includes("aria-label=\"Close modal\""), true, "Modal should expose a labeled close control.");
  assert.equal(markup.includes("size-10 p-0"), true, "Modal close control should use the shared icon button size.");
  assert.equal(markup.includes("size-5"), true, "Modal close icon should be large enough to read in the header.");
});

test("Modal exposes its visible title as the dialog accessible name", () => {
  const markup = renderToStaticMarkup(
    React.createElement(
      Modal,
      { open: true, title: "Dashboard recycle bin", onClose: () => undefined },
      React.createElement("div", null, "Archived dashboards")
    )
  );
  const labelledBy = markup.match(/aria-labelledby="([^"]+)"/)?.[1];

  assert.equal(markup.includes("role=\"dialog\""), true, "Modal should retain dialog semantics.");
  assert.ok(labelledBy, "Dialog should reference its visible title.");
  assert.equal(new RegExp(`<h2[^>]*id="${labelledBy}"`).test(markup), true, "The referenced element should be the visible dialog heading.");
});
