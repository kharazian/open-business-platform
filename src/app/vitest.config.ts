import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    include: ["src/**/*.test.{js,mjs,ts,tsx}", "*.test.{js,mjs,ts,tsx}"]
  }
});
