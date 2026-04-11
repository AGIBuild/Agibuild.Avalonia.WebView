import { expect, test } from "@playwright/test";

test("boots the sample in mock mode", async ({ page }) => {
  await page.goto("/");
  await expect(page.locator("body")).not.toContainText("Connecting to bridge...");
});
