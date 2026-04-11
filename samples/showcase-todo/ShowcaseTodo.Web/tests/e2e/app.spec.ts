import { expect, test } from "@playwright/test";

test("renders the todo sample in mock mode", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Todo" })).toBeVisible();
  await expect(page.getByPlaceholder("Add todo...")).toBeVisible();
});
