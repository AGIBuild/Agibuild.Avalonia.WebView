import { defineConfig } from "vitest/config";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  test: {
    globals: true,
    include: ["src/**/*.test.ts"],
    exclude: ["src/**/*.browser.test.ts", "tests/e2e/**"],
  },
});
