<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { Folder, File, ChevronRight, ArrowUp, Eye } from 'lucide-vue-next';
import { fileService, type FileEntry } from '@/bridge/services';
import { useI18n } from '@/composables/useI18n';

const { t } = useI18n();
const entries = ref<FileEntry[]>([]);
const currentPath = ref('');
const preview = ref<{ name: string; content: string } | null>(null);
const loading = ref(true);

const pathParts = computed(() => currentPath.value.split(/[/\\]/).filter(Boolean));

onMounted(async () => {
  try {
    const p = await fileService.getUserDocumentsPath();
    currentPath.value = p;
    await loadFiles(p);
  } catch {
    loading.value = false;
  }
});

async function loadFiles(path: string) {
  loading.value = true;
  preview.value = null;
  try {
    entries.value = await fileService.listFiles(path);
  } catch {
    entries.value = [];
  } finally {
    loading.value = false;
  }
}

function navigateTo(path: string) {
  currentPath.value = path;
  loadFiles(path);
}

function goUp() {
  const parent = currentPath.value.replace(/[/\\][^/\\]*$/, '');
  if (parent && parent !== currentPath.value) navigateTo(parent);
}

async function openPreview(entry: FileEntry) {
  if (entry.isDirectory) {
    navigateTo(entry.path);
    return;
  }
  const content = await fileService.readTextFile(entry.path);
  preview.value = { name: entry.name, content };
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  } catch {
    return iso;
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- Header -->
    <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-800 shrink-0">
      <h1 class="text-lg font-bold">{{ t('files.title') }}</h1>
      <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
        {{ t('files.subtitle') }} <code class="bg-gray-100 dark:bg-gray-800 px-1 rounded">[JsExport] IFileService</code>
      </p>
    </div>

    <!-- Breadcrumb -->
    <div class="flex items-center gap-1 px-6 py-2 text-xs border-b border-gray-100 dark:border-gray-800 overflow-x-auto shrink-0">
      <button @click="goUp" class="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-800">
        <ArrowUp class="w-3.5 h-3.5" />
      </button>
      <span v-for="(part, i) in pathParts" :key="i" class="flex items-center gap-1">
        <ChevronRight class="w-3 h-3 text-gray-300 dark:text-gray-600" />
        <button
          class="hover:text-blue-500 truncate max-w-32"
          @click="navigateTo('/' + pathParts.slice(0, i + 1).join('/'))"
        >
          {{ part }}
        </button>
      </span>
    </div>

    <div class="flex flex-1 overflow-hidden">
      <!-- File list -->
      <div class="overflow-auto" :class="preview ? 'w-1/2 border-r border-gray-200 dark:border-gray-800' : 'w-full'">
        <div v-if="loading" class="flex items-center justify-center py-16 text-gray-400">
          <div class="w-5 h-5 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin" />
        </div>
        <p v-else-if="entries.length === 0" class="text-sm text-gray-400 text-center py-16">{{ t('files.empty') }}</p>
        <table v-else class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 text-xs text-gray-500 dark:text-gray-400">
              <th class="text-left px-5 py-2 font-medium">{{ t('files.name') }}</th>
              <th class="text-right px-5 py-2 font-medium w-24">{{ t('files.size') }}</th>
              <th class="text-right px-5 py-2 font-medium w-40">{{ t('files.modified') }}</th>
              <th class="w-12" />
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="entry in entries"
              :key="entry.path"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
              @click="openPreview(entry)"
            >
              <td class="px-5 py-2 flex items-center gap-2">
                <Folder v-if="entry.isDirectory" class="w-4 h-4 text-blue-500 shrink-0" />
                <File v-else class="w-4 h-4 text-gray-400 shrink-0" />
                <span class="truncate">{{ entry.name }}</span>
              </td>
              <td class="text-right px-5 py-2 text-gray-500 dark:text-gray-400 tabular-nums">
                {{ entry.isDirectory ? '—' : formatSize(entry.size) }}
              </td>
              <td class="text-right px-5 py-2 text-gray-500 dark:text-gray-400">
                {{ formatDate(entry.lastModified) }}
              </td>
              <td class="px-2">
                <Eye v-if="!entry.isDirectory" class="w-3.5 h-3.5 text-gray-300 dark:text-gray-600" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Preview panel -->
      <div v-if="preview" class="w-1/2 flex flex-col overflow-hidden">
        <div class="flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-800 shrink-0">
          <span class="text-xs font-medium truncate">{{ preview.name }}</span>
          <button
            @click="preview = null"
            class="text-xs text-gray-400 hover:text-gray-600"
          >
            {{ t('files.close') }}
          </button>
        </div>
        <pre class="flex-1 overflow-auto p-4 text-xs font-mono text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900/50 whitespace-pre-wrap">{{ preview.content }}</pre>
      </div>
    </div>
  </div>
</template>
