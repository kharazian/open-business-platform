import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

const demoPassword = "DemoUser!2026";

test("dashboard draft, publish, revision, permission, and cleanup lifecycle", async ({ browser, page }) => {
  test.setTimeout(120_000);
  const suffix = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  const slug = `e2e-dashboard-${suffix}`;
  const originalName = `E2E dashboard ${suffix}`;
  const draftName = `${originalName} draft change`;
  let dashboardId = "";

  await login(page, "admin.demo@company.test");
  await removeInterruptedTestDashboards(page.request);
  const formsResponse = await page.request.get("/api/forms");
  expect(formsResponse.ok()).toBeTruthy();
  const forms = (await formsResponse.json()).items as Array<{ id: string }>;
  expect(forms.length).toBeGreaterThan(0);
  const sharingOptionsResponse = await page.request.get("/api/dashboards/sharing-options");
  expect(sharingOptionsResponse.ok()).toBeTruthy();
  const viewerRole = ((await sharingOptionsResponse.json()).roles as Array<{ id: string; label: string }>).find((role) => role.label === "Viewer");
  expect(viewerRole).toBeTruthy();

  try {
    const createResponse = await page.request.post("/api/dashboards", {
      data: dashboardRequest(originalName, slug, forms[0].id, viewerRole!.id)
    });
    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    dashboardId = created.id;

    await page.goto(`/dashboard-builder/${dashboardId}`);
    await page.waitForLoadState("networkidle");
    await expect(page.getByText("Saved", { exact: true })).toBeVisible();
    await expect(page.getByRole("textbox", { name: "Name", exact: true })).toHaveValue(originalName);
    await expect(page.getByLabel("Audience")).toHaveValue("restricted");
    await expect(page.getByRole("group", { name: "Roles" }).getByRole("checkbox", { name: /^Viewer/ })).toBeChecked();

    await page.getByRole("button", { name: "Preview draft" }).click();
    await expect(page.getByText("This is not the live dashboard.")).toBeVisible();
    await page.getByRole("button", { name: "Close preview" }).click();

    await page.getByRole("textbox", { name: "Description", exact: true }).fill("Saved through the browser lifecycle test.");
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText("Dashboard saved.")).toBeVisible();
    expect((await page.request.get(`/api/dashboards/by-slug/${slug}`)).status()).toBe(404);

    page.once("dialog", (dialog) => dialog.accept());
    await page.getByRole("button", { name: "Publish dashboard" }).click();
    await expect(page.getByText("Draft published. The live dashboard now matches this version.")).toBeVisible();
    const firstLive = await expectDashboard(page.request, `/api/dashboards/by-slug/${slug}`, 200);
    expect(firstLive.name).toBe(originalName);
    const directoryPage = await page.context().newPage();
    await directoryPage.goto("/dashboards");
    await directoryPage.getByRole("textbox", { name: "Search dashboards" }).fill(originalName);
    await expect(directoryPage.getByRole("heading", { name: originalName })).toBeVisible();
    await directoryPage.getByRole("textbox", { name: "Search dashboards" }).fill("no-dashboard-has-this-name");
    await expect(directoryPage.getByText("No dashboards match your search")).toBeVisible();
    await directoryPage.close();

    await page.getByRole("textbox", { name: "Name", exact: true }).fill(draftName);
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText("Dashboard saved.")).toBeVisible();
    const liveDuringDraft = await expectDashboard(page.request, `/api/dashboards/by-slug/${slug}`, 200);
    expect(liveDuringDraft.name).toBe(originalName);

    const viewerContext = await browser.newContext({ baseURL: "http://127.0.0.1:5174" });
    try {
      const viewerPage = await viewerContext.newPage();
      await login(viewerPage, "viewer.demo@company.test");
      const viewerDetail = await expectDashboard(viewerPage.request, `/api/dashboards/${dashboardId}`, 200);
      expect(viewerDetail.name).toBe(originalName);
      const viewerListResponse = await viewerPage.request.get("/api/dashboards");
      expect(viewerListResponse.ok()).toBeTruthy();
      const viewerItem = ((await viewerListResponse.json()).items as Array<{ id: string; name: string }>).find((item) => item.id === dashboardId);
      expect(viewerItem?.name).toBe(originalName);
      expect((await viewerPage.request.get(`/api/dashboards/${dashboardId}/revisions`)).status()).toBe(403);
      expect((await viewerPage.request.get(`/api/dashboards/${dashboardId}/sharing`)).status()).toBe(403);
    } finally {
      await viewerContext.close();
    }

    const unrelatedContext = await browser.newContext({ baseURL: "http://127.0.0.1:5174" });
    try {
      const unrelatedPage = await unrelatedContext.newPage();
      await login(unrelatedPage, "user.demo@company.test");
      expect((await unrelatedPage.request.get(`/api/dashboards/by-slug/${slug}`)).status()).toBe(404);
      const unrelatedList = await unrelatedPage.request.get("/api/dashboards");
      expect(unrelatedList.ok()).toBeTruthy();
      expect(((await unrelatedList.json()).items as Array<{ id: string }>).some((item) => item.id === dashboardId)).toBeFalsy();
    } finally {
      await unrelatedContext.close();
    }

    page.once("dialog", (dialog) => dialog.accept());
    await page.getByRole("button", { name: "Publish changes" }).click();
    await expect(page.getByText("Draft published. The live dashboard now matches this version.")).toBeVisible();
    const secondLive = await expectDashboard(page.request, `/api/dashboards/by-slug/${slug}`, 200);
    expect(secondLive.name).toBe(draftName);

    const revisionsResponse = await page.request.get(`/api/dashboards/${dashboardId}/revisions`);
    expect(revisionsResponse.ok()).toBeTruthy();
    const revisions = (await revisionsResponse.json()).items as Array<{ id: string; revisionNumber: number; reason: string }>;
    expect(revisions.some((revision) => revision.reason === "created")).toBeTruthy();
    expect(revisions.filter((revision) => revision.reason === "published").length).toBe(2);
    const firstPublished = [...revisions].reverse().find((revision) => revision.reason === "published");
    expect(firstPublished).toBeTruthy();

    const revisionRow = page.getByText(`Revision ${firstPublished!.revisionNumber}`, { exact: true }).locator("xpath=../../..");
    page.once("dialog", (dialog) => dialog.accept());
    await revisionRow.getByRole("button", { name: "Restore draft" }).click();
    await expect(page.getByText(`Revision ${firstPublished!.revisionNumber} restored as a new draft revision.`)).toBeVisible();
    await expect(page.getByRole("textbox", { name: "Name", exact: true })).toHaveValue(originalName);
    const liveAfterRestore = await expectDashboard(page.request, `/api/dashboards/by-slug/${slug}`, 200);
    expect(liveAfterRestore.name).toBe(draftName);

    await page.getByRole("button", { name: "Unpublish" }).click();
    await expect(page.getByText("Dashboard unpublished. Its last published version remains in revision history.")).toBeVisible();
    expect((await page.request.get(`/api/dashboards/by-slug/${slug}`)).status()).toBe(404);

    page.once("dialog", (dialog) => dialog.accept());
    await page.getByRole("button", { name: "Duplicate", exact: true }).click();
    await expect(page.getByText("Independent dashboard draft created.")).toBeVisible();
    const duplicateId = page.url().split("/").at(-1)!;
    expect(duplicateId).not.toBe(dashboardId);
    expect((await page.request.get(`/api/dashboards/${duplicateId}`)).status()).toBe(200);
    page.once("dialog", (dialog) => dialog.accept());
    await page.getByRole("button", { name: "Archive" }).click();
    await expect(page.getByText(/archived/)).toBeVisible();
    expect((await page.request.get(`/api/dashboards/${duplicateId}`)).status()).toBe(404);
  } finally {
    if (dashboardId) {
      const detailResponse = await page.request.get(`/api/dashboards/${dashboardId}`);
      if (detailResponse.ok()) {
        const detail = await detailResponse.json();
        const deleteResponse = await page.request.delete(`/api/dashboards/${dashboardId}`, { data: { concurrencyStamp: detail.concurrencyStamp } });
        expect(deleteResponse.status()).toBe(204);
      }
    }
  }
});

async function login(page: Page, email: string) {
  const response = await page.request.post("/api/auth/login", { data: { email, password: demoPassword } });
  expect(response.ok()).toBeTruthy();
}

async function expectDashboard(request: APIRequestContext, path: string, status: number) {
  const response = await request.get(path);
  expect(response.status()).toBe(status);
  return status === 200 ? await response.json() : null;
}

async function removeInterruptedTestDashboards(request: APIRequestContext) {
  const response = await request.get("/api/dashboards");
  expect(response.ok()).toBeTruthy();
  const dashboards = (await response.json()).items as Array<{ id: string; name: string; concurrencyStamp: string }>;
  for (const dashboard of dashboards.filter((item) => item.name.startsWith("E2E dashboard "))) {
    const deleteResponse = await request.delete(`/api/dashboards/${dashboard.id}`, { data: { concurrencyStamp: dashboard.concurrencyStamp } });
    expect(deleteResponse.status()).toBe(204);
  }
}

function dashboardRequest(name: string, slug: string, formId: string, viewerRoleId: string) {
  return {
    name,
    description: "Isolated dashboard lifecycle test.",
    config: {
      schemaVersion: 1,
      sections: [{ id: "overview", title: "Overview", order: 0, icon: "gauge" }],
      widgets: [{
        id: "record-count",
        title: "Record count",
        sourceFormId: formId,
        sectionId: "overview",
        adapter: null,
        chart: {
          widgetType: "number_card",
          metric: { type: "count", fieldId: null },
          groupByFieldId: null,
          dateFieldId: null,
          columns: [],
          limit: 10,
          reportId: null,
          series: null,
          appearance: null
        }
      }],
      templateProvenance: null,
      filters: null
    },
    layout: { schemaVersion: 1, widgets: [{ id: "record-count", width: "small", order: 1 }] },
    settings: { visibility: "workspace", isDefault: false, viewerUserIds: [], viewerRoleIds: [viewerRoleId], viewerGroupIds: [] },
    publication: { status: "draft", slug, showInNavigation: false, menuLabel: null, menuIcon: "layout-dashboard", menuOrder: 0, viewPermission: null }
  };
}
