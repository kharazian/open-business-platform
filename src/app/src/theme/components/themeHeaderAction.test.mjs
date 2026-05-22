import assert from "node:assert/strict";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { test } from "vitest";
import { ThemeHeaderAction } from "./ThemeHeaderAction.tsx";

function TestIcon(props) {
  return React.createElement("svg", props);
}

test("ThemeHeaderAction renders prominent decorative action icons", () => {
  const markup = renderToStaticMarkup(
    React.createElement(
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
});
