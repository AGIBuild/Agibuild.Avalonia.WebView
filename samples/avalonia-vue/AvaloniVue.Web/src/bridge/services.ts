export interface AppInfo {
  name: string;
  version: string;
  description: string;
}

type BridgeRpc = {
  invoke(method: string, params?: Record<string, unknown>): Promise<unknown>;
};

function getRpc(): BridgeRpc {
  const root = window as unknown as { agWebView?: { rpc?: BridgeRpc } };
  if (!root.agWebView?.rpc) {
    throw new Error("Bridge not available.");
  }

  return root.agWebView.rpc;
}

export async function getAppInfo(): Promise<AppInfo> {
  return (await getRpc().invoke("AppShellService.getAppInfo")) as AppInfo;
}
