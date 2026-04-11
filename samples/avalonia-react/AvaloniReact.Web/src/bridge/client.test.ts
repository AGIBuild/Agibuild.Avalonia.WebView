import { createFuloraClient, services } from "./client";

describe("sample client facade", () => {
  it("exposes the sample services facade", () => {
    expect(createFuloraClient()).toHaveProperty("appShell");
    expect(services).toHaveProperty("appShell");
  });
});
