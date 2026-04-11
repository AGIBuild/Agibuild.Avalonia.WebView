import { services } from "./client";
import { installBridgeMock } from "./generated/bridge.mock";

describe("sample browser bridge", () => {
  it("calls services through the mock bridge", async () => {
    installBridgeMock();
    await expect(services.todo.getAll()).resolves.toEqual([]);
  });
});
