import { services } from "./client";
import { installBridgeMock } from "./generated/bridge.mock";

describe("Fulora React template browser mode", () => {
  it("calls the greeter service through the mock bridge", async () => {
    installBridgeMock();

    await expect(services.greeter.greet({ name: "Browser" })).resolves.toContain("Browser");
  });
});
