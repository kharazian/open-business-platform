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
