import { readdir, readFile } from "node:fs/promises";
import { gzipSync } from "node:zlib";

const budgetBytes = 32 * 1024;
const assetDirectory = new URL("../dist/assets/", import.meta.url);
const dashboardChunks = (await readdir(assetDirectory)).filter((name) => /^DashboardsPage-.*\.js$/.test(name));

if (dashboardChunks.length !== 1) {
  throw new Error(`Expected one built DashboardsPage chunk, found ${dashboardChunks.length}. Run npm run build first.`);
}

const chunk = await readFile(new URL(dashboardChunks[0], assetDirectory));
const gzipBytes = gzipSync(chunk).byteLength;
const formattedSize = (gzipBytes / 1024).toFixed(2);

console.log(`Dashboard builder chunk: ${formattedSize} kB gzip (budget: 32.00 kB).`);
if (gzipBytes > budgetBytes) {
  throw new Error(`Dashboard builder bundle exceeded its 32 kB gzip budget by ${((gzipBytes - budgetBytes) / 1024).toFixed(2)} kB.`);
}
