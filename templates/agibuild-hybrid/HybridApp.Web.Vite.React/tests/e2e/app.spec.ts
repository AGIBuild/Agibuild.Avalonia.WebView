import { expect, test } from "@playwright/test";

test("renders the React template in mock mode", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "HybridApp React Template" })).toBeVisible();
  await expect(page.getByText("connected")).toBeVisible();

  await page.getByPlaceholder("Enter your name...").fill("Playwright");
  await page.getByRole("button", { name: "Greet from C#" }).click();

  await expect(page.getByText("Hello, Playwright! (from Fulora mock)")).toBeVisible();
});
