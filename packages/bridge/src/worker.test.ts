import { describe, it } from "node:test";
import assert from "node:assert/strict";
import {
  WorkerBridgeClient,
  createBridgeRelay,
  type WorkerBridgeMessage,
  type BridgeWithGetService,
} from "./worker.ts";

function createMockPortPair(): { mainPort: MessagePort; workerPort: MessagePort } {
  const channel = new MessageChannel();
  return { mainPort: channel.port1, workerPort: channel.port2 };
}

function createMockBridge(responses: Record<string, unknown>): BridgeWithGetService {
  return {
    getService(name: string) {
      return {
        ping: async () => responses[`${name}.ping`] ?? "pong",
        getData: async (_params?: unknown) => responses[`${name}.getData`] ?? [1, 2, 3],
        fail: async () => {
          throw new Error("service error");
        },
      } as Record<string, (params?: unknown) => Promise<unknown>>;
    },
  };
}

describe("WorkerBridgeClient and createBridgeRelay", () => {
  it("init/ready handshake completes", async () => {
    const { mainPort, workerPort } = createMockPortPair();
    const bridge = createMockBridge({});
    const stopRelay = createBridgeRelay(mainPort, bridge);
    const client = new WorkerBridgeClient(workerPort);
    try {
      // callService awaits ready internally; success implies handshake completed
      const result = await client.callService("TestService", "ping");
      assert.equal(result, "pong");
    } finally {
      client.dispose();
      stopRelay();
      mainPort.close();
    }
  });

  it("request/response roundtrip", async () => {
    const { mainPort, workerPort } = createMockPortPair();
    const bridge = createMockBridge({ "TestService.ping": "pong" });
    const stopRelay = createBridgeRelay(mainPort, bridge);
    const client = new WorkerBridgeClient(workerPort);
    try {
      const result = await client.callService("TestService", "ping");
      assert.equal(result, "pong");
    } finally {
      client.dispose();
      stopRelay();
      mainPort.close();
    }
  });

  it("request/response with params", async () => {
    const { mainPort, workerPort } = createMockPortPair();
    const bridge: BridgeWithGetService = {
      getService() {
        return {
          add: async (params?: unknown) => {
            const p = params as { a: number; b: number };
            return (p?.a ?? 0) + (p?.b ?? 0);
          },
        } as Record<string, (params?: unknown) => Promise<unknown>>;
      },
    };
    const stopRelay = createBridgeRelay(mainPort, bridge);
    const client = new WorkerBridgeClient(workerPort);
    try {
      const result = await client.callService("Math", "add", { a: 2, b: 3 });
      assert.equal(result, 5);
    } finally {
      client.dispose();
      stopRelay();
      mainPort.close();
    }
  });

  it("error propagation from service", async () => {
    const { mainPort, workerPort } = createMockPortPair();
    const bridge = createMockBridge({});
    const stopRelay = createBridgeRelay(mainPort, bridge);
    const client = new WorkerBridgeClient(workerPort);
    try {
      await assert.rejects(
        () => client.callService("TestService", "fail"),
        { message: "service error" }
      );
    } finally {
      client.dispose();
      stopRelay();
      mainPort.close();
    }
  });

  it("relay returns cleanup function that removes listener", () => {
    const { mainPort, workerPort } = createMockPortPair();
    const bridge = createMockBridge({});
    const dispose = createBridgeRelay(mainPort, bridge);
    assert.doesNotThrow(() => {
      dispose();
      mainPort.close();
      workerPort.close();
    });
  });
});

describe("WorkerBridgeMessage types", () => {
  it("supports all message types for type coverage", () => {
    const init: WorkerBridgeMessage = { type: "fulora:worker:init" };
    const ready: WorkerBridgeMessage = { type: "fulora:worker:ready" };
    const req: WorkerBridgeMessage = {
      type: "fulora:worker:request",
      id: "1",
      service: "Svc",
      method: "m",
      params: [],
    };
    const res: WorkerBridgeMessage = {
      type: "fulora:worker:response",
      id: "1",
      result: "ok",
    };
    const err: WorkerBridgeMessage = {
      type: "fulora:worker:response",
      id: "1",
      error: "failed",
    };
    assert.equal(init.type, "fulora:worker:init");
    assert.equal(ready.type, "fulora:worker:ready");
    assert.equal(req.type, "fulora:worker:request");
    assert.equal(res.result, "ok");
    assert.equal(err.error, "failed");
  });
});
