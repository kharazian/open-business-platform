import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

const rootEnvDir = "../..";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, rootEnvDir, "");
  const apiBaseUrl = env.VITE_API_BASE_URL || "http://localhost:5080";
  const appHost = env.VITE_APP_HOST || "127.0.0.1";
  const appPort = Number.parseInt(env.VITE_APP_PORT || "5174", 10);

  return {
    envDir: rootEnvDir,
    envPrefix: ["VITE_", "BRAND_"],
    plugins: [tailwindcss(), react()],
    server: {
      host: appHost,
      port: Number.isNaN(appPort) ? 5174 : appPort,
      strictPort: true,
      proxy: {
        "/api": apiBaseUrl,
        "/health": apiBaseUrl
      }
    }
  };
});
