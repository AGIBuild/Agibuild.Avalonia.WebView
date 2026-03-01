/**
 * Typed Bridge service proxies.
 * Each proxy maps to a C# [JsExport] service exposed via the Agibuild WebView Bridge.
 */

import { bridgeClient, type BridgeServiceMethod } from '@agibuild/bridge';

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

interface AppShellBridgeService {
  getPages: BridgeServiceMethod<void, PageDefinition[]>;
  getAppInfo: BridgeServiceMethod<void, AppInfo>;
}

interface SystemInfoBridgeService {
  getSystemInfo: BridgeServiceMethod<void, SystemInfo>;
  getRuntimeMetrics: BridgeServiceMethod<void, RuntimeMetrics>;
}

interface ChatBridgeService {
  sendMessage: BridgeServiceMethod<{ request: ChatRequest }, ChatResponse>;
  getHistory: BridgeServiceMethod<void, ChatMessage[]>;
  clearHistory: BridgeServiceMethod<void, void>;
}

interface FileBridgeService {
  listFiles: BridgeServiceMethod<{ path?: string }, FileEntry[]>;
  readTextFile: BridgeServiceMethod<{ path: string }, string>;
  getUserDocumentsPath: BridgeServiceMethod<void, string>;
}

interface SettingsBridgeService {
  getSettings: BridgeServiceMethod<void, AppSettings>;
  updateSettings: BridgeServiceMethod<{ settings: AppSettings }, AppSettings>;
}

const appShellRpc = bridgeClient.getService<AppShellBridgeService>('AppShellService');
const systemInfoRpc = bridgeClient.getService<SystemInfoBridgeService>('SystemInfoService');
const chatRpc = bridgeClient.getService<ChatBridgeService>('ChatService');
const fileRpc = bridgeClient.getService<FileBridgeService>('FileService');
const settingsRpc = bridgeClient.getService<SettingsBridgeService>('SettingsService');

// ─── Service proxies ────────────────────────────────────────────────────────

export const appShellService = {
  getPages: () => appShellRpc.getPages(),
  getAppInfo: () => appShellRpc.getAppInfo(),
};

export const systemInfoService = {
  getSystemInfo: () => systemInfoRpc.getSystemInfo(),
  getRuntimeMetrics: () => systemInfoRpc.getRuntimeMetrics(),
};

export const chatService = {
  sendMessage: (request: ChatRequest) => chatRpc.sendMessage({ request }),
  getHistory: () => chatRpc.getHistory(),
  clearHistory: () => chatRpc.clearHistory(),
};

export const fileService = {
  listFiles: (path?: string) => fileRpc.listFiles({ path }),
  readTextFile: (path: string) => fileRpc.readTextFile({ path }),
  getUserDocumentsPath: () => fileRpc.getUserDocumentsPath(),
};

export const settingsService = {
  getSettings: () => settingsRpc.getSettings(),
  updateSettings: (settings: AppSettings) => settingsRpc.updateSettings({ settings }),
};
