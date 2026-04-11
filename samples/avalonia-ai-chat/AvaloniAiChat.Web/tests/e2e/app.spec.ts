import { expect, test } from "@playwright/test";

test("renders the AI chat sample in mock mode", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Fulora AI Chat" })).toBeVisible();
  await expect(page.getByPlaceholder("Type a message...")).toBeVisible();
});
