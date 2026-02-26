import { bridgeClient, type BridgeServiceMethod } from "@agibuild/bridge";

export interface AppInfo {
  name: string;
  version: string;
  description: string;
}

interface AppShellBridgeService {
  getAppInfo: BridgeServiceMethod<void, AppInfo>;
}

const appShellService = bridgeClient.getService<AppShellBridgeService>("AppShellService");

export async function getAppInfo(): Promise<AppInfo> {
  return appShellService.getAppInfo();
}
