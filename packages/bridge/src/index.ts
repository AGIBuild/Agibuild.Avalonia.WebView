export interface BridgeReadyOptions {
  timeoutMs?: number;
  pollIntervalMs?: number;
}

export interface BridgeRpc {
  invoke(method: string, params?: Record<string, unknown>): Promise<unknown>;
  handle?(method: string, handler: (params: unknown) => unknown | Promise<unknown>): void;
}

export type BridgeServiceMethod<TParams = void, TResult = unknown> =
  [TParams] extends [void]
    ? () => Promise<TResult>
    : (params: TParams) => Promise<TResult>;

export type BridgeServiceContract<TService extends object> = {
  [K in keyof TService]: TService[K] extends (...args: any[]) => Promise<any>
    ? TService[K]
    : never;
};

export interface BridgeClient {
  ready(options?: BridgeReadyOptions): Promise<void>;
  invoke<T>(method: string, params?: Record<string, unknown>): Promise<T>;
  getService<TService extends object>(serviceName: string): BridgeServiceContract<TService>;
}

type BridgeRoot = {
  agWebView?: {
    rpc?: BridgeRpc;
  };
};

function getRpcFromWindow(win: Window & typeof globalThis): BridgeRpc | null {
  const root = win as unknown as BridgeRoot;
  return root.agWebView?.rpc ?? null;
}

function validateServiceParams(params: unknown): Record<string, unknown> | undefined {
  if (params === undefined) {
    return undefined;
  }

  if (params === null || Array.isArray(params) || typeof params !== "object") {
    throw new Error("Bridge service methods accept only object params.");
  }

  return params as Record<string, unknown>;
}

export function createBridgeClient(
  resolveRpc: () => BridgeRpc | null = () => getRpcFromWindow(window)
): BridgeClient {
  async function ready(options: BridgeReadyOptions = {}): Promise<void> {
    const timeoutMs = options.timeoutMs ?? 3000;
    const pollIntervalMs = options.pollIntervalMs ?? 50;
    const start = Date.now();
    while (!resolveRpc()) {
      if (Date.now() - start >= timeoutMs) {
        throw new Error("Bridge not available within timeout.");
      }

      await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
    }
  }

  async function invoke<T>(method: string, params?: Record<string, unknown>): Promise<T> {
    const rpc = resolveRpc();
    if (!rpc) {
      throw new Error("Bridge not available.");
    }

    return (await rpc.invoke(method, params)) as T;
  }

  function getService<TService extends object>(serviceName: string): BridgeServiceContract<TService> {
    return new Proxy(
      {},
      {
        get(_target, prop) {
          if (typeof prop !== "string") {
            return undefined;
          }

          return (params?: unknown) => invoke(`${serviceName}.${prop}`, validateServiceParams(params));
        },
      }
    ) as BridgeServiceContract<TService>;
  }

  return {
    ready,
    invoke,
    getService,
  };
}

export const bridgeClient = createBridgeClient();
