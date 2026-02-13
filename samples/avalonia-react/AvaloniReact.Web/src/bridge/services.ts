/**
 * Typed Bridge service proxies.
 * Each proxy maps to a C# [JsExport] service exposed via the Agibuild WebView Bridge.
 */

// ─── Types mirroring C# models ──────────────────────────────────────────────

export interface PageDefinition {
  id: string;
  title: string;
  icon: string;
  route: string;
}

export interface AppInfo {
  name: string;
  version: string;
  description: string;
}

export interface SystemInfo {
  osName: string;
  osVersion: string;
  dotnetVersion: string;
  avaloniaVersion: string;
  machineName: string;
  processorCount: number;
  totalMemoryMb: number;
  webViewEngine: string;
}

export interface RuntimeMetrics {
  workingSetMb: number;
  gcTotalMemoryMb: number;
  threadCount: number;
  uptimeSeconds: number;
}

export interface ChatRequest {
  message: string;
}

export interface ChatResponse {
  id: string;
  message: string;
  timestamp: string;
}

export interface ChatMessage {
  id: string;
  role: string;
  content: string;
  timestamp: string;
}

export interface FileEntry {
  name: string;
  path: string;
  isDirectory: boolean;
  size: number;
  lastModified: string;
}

export interface AppSettings {
  theme: string;
  language: string;
  fontSize: number;
  sidebarCollapsed: boolean;
}

// ─── Bridge RPC helper ──────────────────────────────────────────────────────

function getRpc(): { invoke: (method: string, params?: Record<string, unknown>) => Promise<unknown> } {
  const w = window as unknown as {
    agWebView?: { rpc?: { invoke: (method: string, params?: Record<string, unknown>) => Promise<unknown> } };
  };
  if (w.agWebView?.rpc) return w.agWebView.rpc;
  throw new Error('Bridge not available');
}

async function invoke<T>(method: string, params?: Record<string, unknown>): Promise<T> {
  const rpc = getRpc();
  return (await rpc.invoke(method, params)) as T;
}

// ─── Service proxies ────────────────────────────────────────────────────────

export const appShellService = {
  getPages: () => invoke<PageDefinition[]>('AppShellService.getPages'),
  getAppInfo: () => invoke<AppInfo>('AppShellService.getAppInfo'),
};

export const systemInfoService = {
  getSystemInfo: () => invoke<SystemInfo>('SystemInfoService.getSystemInfo'),
  getRuntimeMetrics: () => invoke<RuntimeMetrics>('SystemInfoService.getRuntimeMetrics'),
};

export const chatService = {
  sendMessage: (request: ChatRequest) =>
    invoke<ChatResponse>('ChatService.sendMessage', { request }),
  getHistory: () => invoke<ChatMessage[]>('ChatService.getHistory'),
  clearHistory: () => invoke<void>('ChatService.clearHistory'),
};

export const fileService = {
  listFiles: (path?: string) => invoke<FileEntry[]>('FileService.listFiles', { path }),
  readTextFile: (path: string) => invoke<string>('FileService.readTextFile', { path }),
  getUserDocumentsPath: () => invoke<string>('FileService.getUserDocumentsPath'),
};

export const settingsService = {
  getSettings: () => invoke<AppSettings>('SettingsService.getSettings'),
  updateSettings: (settings: AppSettings) =>
    invoke<AppSettings>('SettingsService.updateSettings', { settings }),
};
