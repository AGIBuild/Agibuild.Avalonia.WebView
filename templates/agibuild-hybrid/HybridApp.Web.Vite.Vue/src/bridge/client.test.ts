import { createFuloraClient, services } from "./client";

describe("Fulora Vue template client", () => {
  it("exposes the generated services facade", () => {
    expect(createFuloraClient()).toHaveProperty("greeter");
    expect(services).toHaveProperty("greeter");
  });
});
