import { createFuloraClient, services } from "./client";

describe("sample client facade", () => {
  it("exposes the sample services facade", () => {
    expect(createFuloraClient()).toHaveProperty("aiChat");
    expect(services).toHaveProperty("aiChat");
  });
});
