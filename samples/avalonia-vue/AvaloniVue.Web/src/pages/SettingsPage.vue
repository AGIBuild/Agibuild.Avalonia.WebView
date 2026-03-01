<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Save, Check } from 'lucide-vue-next';
import { settingsService, type AppSettings } from '@/bridge/services';
import { applySettings, dispatchSettingsChanged } from '@/utils/applySettings';
import { useI18n } from '@/composables/useI18n';

const { t, setLocale } = useI18n();
const settings = ref<AppSettings | null>(null);
const saving = ref(false);
const saved = ref(false);

onMounted(async () => {
  try {
    const s = await settingsService.getSettings();
    settings.value = s;
    applySettings(s);
  } catch { /* ignore */ }
});

async function handleSave() {
  if (!settings.value) return;
  saving.value = true;
  try {
    const updated = await settingsService.updateSettings(settings.value);
    settings.value = updated;
    applySettings(updated);
    dispatchSettingsChanged(updated);
    saved.value = true;
    setTimeout(() => { saved.value = false; }, 2000);
  } finally {
    saving.value = false;
  }
}

function onLanguageChange(lang: string) {
  if (!settings.value) return;
  settings.value = { ...settings.value, language: lang };
  setLocale(lang);
}
</script>

<template>
  <div v-if="!settings" class="flex items-center justify-center h-full">
    <div class="w-6 h-6 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin" />
  </div>

  <div v-else class="p-6 max-w-2xl space-y-6">
    <div>
      <h1 class="text-2xl font-bold">{{ t('settings.title') }}</h1>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
        {{ t('settings.subtitle') }}
        <code class="text-xs bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">[JsExport] ISettingsService</code>
      </p>
    </div>

    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 divide-y divide-gray-100 dark:divide-gray-800">
      <!-- Theme -->
      <div class="flex items-center justify-between px-5 py-4">
        <div>
          <p class="text-sm font-medium">{{ t('settings.theme') }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('settings.themeDesc') }}</p>
        </div>
        <select
          :value="settings.theme"
          @change="settings = { ...settings!, theme: ($event.target as HTMLSelectElement).value }"
          class="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
        >
          <option value="system">{{ t('settings.themeSystem') }}</option>
          <option value="light">{{ t('settings.themeLight') }}</option>
          <option value="dark">{{ t('settings.themeDark') }}</option>
        </select>
      </div>

      <!-- Language -->
      <div class="flex items-center justify-between px-5 py-4">
        <div>
          <p class="text-sm font-medium">{{ t('settings.language') }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('settings.languageDesc') }}</p>
        </div>
        <select
          :value="settings.language"
          @change="onLanguageChange(($event.target as HTMLSelectElement).value)"
          class="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
        >
          <option value="en">English</option>
          <option value="zh">中文</option>
          <option value="ja">日本語</option>
          <option value="ko">한국어</option>
        </select>
      </div>

      <!-- Font Size -->
      <div class="flex items-center justify-between px-5 py-4">
        <div>
          <p class="text-sm font-medium">{{ t('settings.fontSize') }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('settings.fontSizeDesc', { size: settings.fontSize }) }}</p>
        </div>
        <input
          type="range"
          :min="12"
          :max="20"
          :value="settings.fontSize"
          @input="settings = { ...settings!, fontSize: Number(($event.target as HTMLInputElement).value) }"
          class="w-32 accent-blue-500"
        />
      </div>

      <!-- Sidebar Collapsed -->
      <div class="flex items-center justify-between px-5 py-4">
        <div>
          <p class="text-sm font-medium">{{ t('settings.sidebar') }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ t('settings.sidebarDesc') }}</p>
        </div>
        <button
          @click="settings = { ...settings!, sidebarCollapsed: !settings!.sidebarCollapsed }"
          class="relative w-10 h-6 rounded-full transition-colors"
          :class="settings.sidebarCollapsed ? 'bg-blue-500' : 'bg-gray-200 dark:bg-gray-700'"
        >
          <div
            class="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform"
            :class="settings.sidebarCollapsed ? 'translate-x-4.5' : 'translate-x-0.5'"
          />
        </button>
      </div>
    </div>

    <!-- Save button -->
    <button
      @click="handleSave"
      :disabled="saving"
      class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-500 text-white text-sm font-medium hover:bg-blue-600 disabled:opacity-50 transition-colors"
    >
      <Check v-if="saved" class="w-4 h-4" />
      <Save v-else class="w-4 h-4" />
      {{ saved ? t('settings.saved') : saving ? t('settings.saving') : t('settings.save') }}
    </button>

    <!-- How it works -->
    <div class="bg-blue-50 dark:bg-blue-500/5 border border-blue-200 dark:border-blue-800 rounded-xl p-5 text-sm">
      <p class="font-medium text-blue-700 dark:text-blue-300">{{ t('settings.howTitle') }}</p>
      <p class="mt-1 text-blue-600 dark:text-blue-400">{{ t('settings.howBody') }}</p>
    </div>
  </div>
</template>
