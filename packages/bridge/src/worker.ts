// WorkerBridgeClient - enables bridge calls from Web Workers via main-thread relay

export interface WorkerBridgeMessage {
  type: "fulora:worker:request" | "fulora:worker:response" | "fulora:worker:init" | "fulora:worker:ready";
  id?: string;
  service?: string;
  method?: string;
  params?: unknown[];
  result?: unknown;
  error?: string;
}

export interface BridgeWithGetService {
  getService(name: string): Record<string, (params?: unknown) => Promise<unknown>>;
}

/**
 * Main-thread relay: connects a MessagePort from a worker to the bridge client.
 * Call this on the main thread to relay worker bridge calls.
 */
export function createBridgeRelay(port: MessagePort, bridge: BridgeWithGetService): () => void {
  const handler = async (event: MessageEvent<WorkerBridgeMessage>) => {
    const msg = event.data;
    if (msg.type === "fulora:worker:init") {
      port.postMessage({ type: "fulora:worker:ready" });
      return;
    }
    if (msg.type === "fulora:worker:request") {
      try {
        const service = bridge.getService(msg.service!);
        const method = service[msg.method!];
        if (typeof method !== "function") {
          throw new Error(`Method ${msg.service}.${msg.method} not found`);
        }
        const result = await method(...(msg.params ?? []));
        port.postMessage({ type: "fulora:worker:response", id: msg.id, result });
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        port.postMessage({ type: "fulora:worker:response", id: msg.id, error: errorMessage });
      }
    }
  };
  port.addEventListener("message", handler);
  port.start();
  return () => port.removeEventListener("message", handler);
}

/**
 * Worker-side bridge client. Use in a Web Worker to call bridge services.
 */
export class WorkerBridgeClient {
  private port: MessagePort;
  private pendingCalls = new Map<string, { resolve: (v: unknown) => void; reject: (e: unknown) => void }>();
  private nextId = 0;
  private ready: Promise<void>;
  private readonly messageHandler: (event: MessageEvent<WorkerBridgeMessage>) => void;
  private disposed = false;

  constructor(port: MessagePort) {
    this.port = port;
    this.messageHandler = this.onMessage.bind(this);
    this.ready = new Promise<void>((resolve) => {
      const handler = (event: MessageEvent<WorkerBridgeMessage>) => {
        if (event.data.type === "fulora:worker:ready") {
          this.port.removeEventListener("message", handler);
          resolve();
        }
      };
      this.port.addEventListener("message", handler);
    });
    this.port.addEventListener("message", this.messageHandler);
    this.port.start();
    this.port.postMessage({ type: "fulora:worker:init" });
  }

  private onMessage(event: MessageEvent<WorkerBridgeMessage>): void {
    const msg = event.data;
    if (msg.type === "fulora:worker:response" && msg.id) {
      const pending = this.pendingCalls.get(msg.id);
      if (pending) {
        this.pendingCalls.delete(msg.id);
        if (msg.error) {
          pending.reject(new Error(msg.error));
        } else {
          pending.resolve(msg.result);
        }
      }
    }
  }

  async callService(service: string, method: string, ...params: unknown[]): Promise<unknown> {
    if (this.disposed) {
      throw new Error("WorkerBridgeClient is disposed.");
    }
    await this.ready;
    const id = String(this.nextId++);
    return new Promise((resolve, reject) => {
      this.pendingCalls.set(id, { resolve, reject });
      this.port.postMessage({
        type: "fulora:worker:request",
        id,
        service,
        method,
        params,
      });
    });
  }

  dispose(): void {
    if (this.disposed) {
      return;
    }
    this.disposed = true;
    this.port.removeEventListener("message", this.messageHandler);
    for (const pending of this.pendingCalls.values()) {
      pending.reject(new Error("WorkerBridgeClient disposed."));
    }
    this.pendingCalls.clear();
    this.port.close();
  }
}
