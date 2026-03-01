<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import {
  Cpu, MemoryStick, Monitor, Clock, Server, Layers, Globe, Hash,
} from 'lucide-vue-next';
import { systemInfoService, type SystemInfo, type RuntimeMetrics } from '@/bridge/services';
import { useI18n } from '@/composables/useI18n';

const { t } = useI18n();
const info = ref<SystemInfo | null>(null);
const metrics = ref<RuntimeMetrics | null>(null);
const error = ref<string | null>(null);

let metricsInterval: ReturnType<typeof setInterval> | undefined;

onMounted(async () => {
  try {
    info.value = await systemInfoService.getSystemInfo();
  } catch (e: unknown) {
    error.value = (e as Error).message;
  }

  const refreshMetrics = () => {
    systemInfoService.getRuntimeMetrics().then((m) => { metrics.value = m; }).catch(() => {});
  };
  refreshMetrics();
  metricsInterval = setInterval(refreshMetrics, 2000);
});

onUnmounted(() => {
  if (metricsInterval) clearInterval(metricsInterval);
});

function formatUptime(seconds: number): string {
  if (seconds < 60) return `${Math.round(seconds)}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${Math.round(seconds % 60)}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}
</script>

<template>
  <div class="p-6 space-y-6">
    <div v-if="error" class="bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-800 rounded-lg p-4 text-red-600 dark:text-red-400">
      Failed to load system info: {{ error }}
    </div>

    <template v-else>
      <div>
        <h1 class="text-2xl font-bold">{{ t('dashboard.title') }}</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ t('dashboard.subtitle') }}
          <code class="text-xs bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">[JsExport] ISystemInfoService</code>
        </p>
      </div>

      <!-- Live metrics cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
          <div class="inline-flex p-2 rounded-lg bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400">
            <MemoryStick class="w-5 h-5" />
          </div>
          <p class="mt-3 text-2xl font-bold">{{ metrics ? `${metrics.workingSetMb} MB` : '—' }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('dashboard.workingSet') }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
          <div class="inline-flex p-2 rounded-lg bg-purple-50 dark:bg-purple-500/10 text-purple-600 dark:text-purple-400">
            <Layers class="w-5 h-5" />
          </div>
          <p class="mt-3 text-2xl font-bold">{{ metrics ? `${metrics.gcTotalMemoryMb} MB` : '—' }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('dashboard.gcMemory') }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
          <div class="inline-flex p-2 rounded-lg bg-green-50 dark:bg-green-500/10 text-green-600 dark:text-green-400">
            <Cpu class="w-5 h-5" />
          </div>
          <p class="mt-3 text-2xl font-bold">{{ metrics ? `${metrics.threadCount}` : '—' }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('dashboard.threads') }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
          <div class="inline-flex p-2 rounded-lg bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400">
            <Clock class="w-5 h-5" />
          </div>
          <p class="mt-3 text-2xl font-bold">{{ metrics ? formatUptime(metrics.uptimeSeconds) : '—' }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('dashboard.uptime') }}</p>
        </div>
      </div>

      <!-- System info table -->
      <div v-if="info" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 overflow-hidden">
        <div class="px-5 py-3 border-b border-gray-200 dark:border-gray-800">
          <h2 class="font-semibold">{{ t('dashboard.platformDetails') }}</h2>
        </div>
        <div class="divide-y divide-gray-100 dark:divide-gray-800">
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Monitor class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.os') }}</span>
            <span class="text-sm font-medium">{{ info.osName }} — {{ info.osVersion }}</span>
          </div>
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Server class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.dotnet') }}</span>
            <span class="text-sm font-medium">{{ info.dotnetVersion }}</span>
          </div>
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Layers class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.avalonia') }}</span>
            <span class="text-sm font-medium">{{ info.avaloniaVersion }}</span>
          </div>
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Globe class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.webviewEngine') }}</span>
            <span class="text-sm font-medium">{{ info.webViewEngine }}</span>
          </div>
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Cpu class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.machine') }}</span>
            <span class="text-sm font-medium">{{ info.machineName }} ({{ info.processorCount }} cores)</span>
          </div>
          <div class="flex items-center px-5 py-3">
            <span class="text-gray-400 dark:text-gray-500 mr-3"><Hash class="w-4 h-4" /></span>
            <span class="text-sm text-gray-500 dark:text-gray-400 w-40">{{ t('dashboard.totalMemory') }}</span>
            <span class="text-sm font-medium">{{ info.totalMemoryMb }} MB</span>
          </div>
        </div>
      </div>

      <!-- How it works -->
      <div class="bg-blue-50 dark:bg-blue-500/5 border border-blue-200 dark:border-blue-800 rounded-xl p-5 text-sm">
        <p class="font-medium text-blue-700 dark:text-blue-300">{{ t('dashboard.howTitle') }}</p>
        <p class="mt-1 text-blue-600 dark:text-blue-400">{{ t('dashboard.howBody') }}</p>
      </div>
    </template>
  </div>
</template>
