import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  timeout: 90_000,
  expect: { timeout: 10_000 },
  reporter: [["list"]],
  use: {
    baseURL: "http://127.0.0.1:5174",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure"
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: "cd ../api && dotnet run",
      url: "http://127.0.0.1:5080/health",
      timeout: 120_000,
      reuseExistingServer: !process.env.CI
    },
    {
      command: "npm run dev",
      url: "http://127.0.0.1:5174",
      timeout: 60_000,
      reuseExistingServer: !process.env.CI
    }
  ]
});
