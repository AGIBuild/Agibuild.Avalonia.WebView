import { useState, useEffect } from 'react';
import { Cpu, MemoryStick, Monitor, Clock, Server, Layers, Globe, Hash } from 'lucide-react';
import { systemInfoService, type SystemInfo, type RuntimeMetrics } from '../bridge/services';

export function Dashboard() {
  const [info, setInfo] = useState<SystemInfo | null>(null);
  const [metrics, setMetrics] = useState<RuntimeMetrics | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    systemInfoService.getSystemInfo()
      .then(setInfo)
      .catch((e: Error) => setError(e.message));
  }, []);

  // Auto-refresh runtime metrics every 2 seconds
  useEffect(() => {
    const refresh = () => {
      systemInfoService.getRuntimeMetrics().then(setMetrics).catch(() => {});
    };
    refresh();
    const interval = setInterval(refresh, 2000);
    return () => clearInterval(interval);
  }, []);

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-800 rounded-lg p-4 text-red-600 dark:text-red-400">
          Failed to load system info: {error}
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          System information from C# via <code className="text-xs bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">[JsExport] ISystemInfoService</code>
        </p>
      </div>

      {/* Live metrics cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          icon={<MemoryStick className="w-5 h-5" />}
          label="Working Set"
          value={metrics ? `${metrics.workingSetMb} MB` : '—'}
          color="blue"
        />
        <MetricCard
          icon={<Layers className="w-5 h-5" />}
          label="GC Memory"
          value={metrics ? `${metrics.gcTotalMemoryMb} MB` : '—'}
          color="purple"
        />
        <MetricCard
          icon={<Cpu className="w-5 h-5" />}
          label="Threads"
          value={metrics ? `${metrics.threadCount}` : '—'}
          color="green"
        />
        <MetricCard
          icon={<Clock className="w-5 h-5" />}
          label="Uptime"
          value={metrics ? formatUptime(metrics.uptimeSeconds) : '—'}
          color="amber"
        />
      </div>

      {/* System info table */}
      {info && (
        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-200 dark:border-gray-800">
            <h2 className="font-semibold">Platform Details</h2>
          </div>
          <div className="divide-y divide-gray-100 dark:divide-gray-800">
            <InfoRow icon={<Monitor className="w-4 h-4" />} label="Operating System" value={`${info.osName} — ${info.osVersion}`} />
            <InfoRow icon={<Server className="w-4 h-4" />} label=".NET Runtime" value={info.dotnetVersion} />
            <InfoRow icon={<Layers className="w-4 h-4" />} label="Avalonia" value={info.avaloniaVersion} />
            <InfoRow icon={<Globe className="w-4 h-4" />} label="WebView Engine" value={info.webViewEngine} />
            <InfoRow icon={<Cpu className="w-4 h-4" />} label="Machine" value={`${info.machineName} (${info.processorCount} cores)`} />
            <InfoRow icon={<Hash className="w-4 h-4" />} label="Total Memory" value={`${info.totalMemoryMb} MB`} />
          </div>
        </div>
      )}

      {/* Bridge call explanation */}
      <div className="bg-blue-50 dark:bg-blue-500/5 border border-blue-200 dark:border-blue-800 rounded-xl p-5 text-sm">
        <p className="font-medium text-blue-700 dark:text-blue-300">How this works</p>
        <p className="mt-1 text-blue-600 dark:text-blue-400">
          The metrics above are fetched from C# via <code className="bg-blue-100 dark:bg-blue-500/20 px-1 rounded">SystemInfoService.getRuntimeMetrics()</code> — a JSON-RPC call over the WebView bridge, refreshing every 2 seconds. Data like OS info and .NET version are impossible to obtain from JavaScript alone.
        </p>
      </div>
    </div>
  );
}

// ─── Sub-components ─────────────────────────────────────────────────────────

function MetricCard({ icon, label, value, color }: {
  icon: React.ReactNode;
  label: string;
  value: string;
  color: string;
}) {
  const colorMap: Record<string, string> = {
    blue: 'bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400',
    purple: 'bg-purple-50 dark:bg-purple-500/10 text-purple-600 dark:text-purple-400',
    green: 'bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400',
    amber: 'bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400',
  };

  return (
    <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
      <div className={`inline-flex p-2 rounded-lg ${colorMap[color]}`}>{icon}</div>
      <p className="mt-3 text-2xl font-bold">{value}</p>
      <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{label}</p>
    </div>
  );
}

function InfoRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center px-5 py-3">
      <span className="text-gray-400 dark:text-gray-500 mr-3">{icon}</span>
      <span className="text-sm text-gray-500 dark:text-gray-400 w-40">{label}</span>
      <span className="text-sm font-medium">{value}</span>
    </div>
  );
}

function formatUptime(seconds: number): string {
  if (seconds < 60) return `${Math.round(seconds)}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${Math.round(seconds % 60)}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}
