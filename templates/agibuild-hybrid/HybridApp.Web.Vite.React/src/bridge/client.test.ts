import { createFuloraClient, services } from "./client";

describe("Fulora React template client", () => {
  it("exposes the generated services facade", () => {
    expect(createFuloraClient()).toHaveProperty("greeter");
    expect(services).toHaveProperty("greeter");
  });
});
