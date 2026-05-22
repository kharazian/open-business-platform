import assert from "node:assert/strict";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { test } from "vitest";
import { PageHeader } from "./PageHeader.tsx";

test("PageHeader renders as compact page chrome rather than a card hero", () => {
  const markup = renderToStaticMarkup(
    React.createElement(PageHeader, {
      eyebrow: "Forms",
      title: "Form drafts",
      description: "Create and manage forms.",
      actions: React.createElement("button", null, "Create form")
    })
  );

  assert.equal(markup.includes("surface"), false, "PageHeader should not render as a large surface card.");
  assert.equal(markup.includes("sm:text-4xl"), false, "PageHeader title should not use hero-scale responsive type.");
  assert.equal(markup.includes("mt-4"), false, "PageHeader should avoid large vertical title offsets.");
  assert.equal(markup.includes("border-b"), true, "PageHeader should separate content with a compact bottom rule.");
});
