<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue';
import { useBridgeReady } from '@/composables/useBridge';
import { usePageRegistry } from '@/composables/usePageRegistry';
import { settingsService, type AppSettings } from '@/bridge/services';
import { applySettings } from '@/utils/applySettings';
import { setI18nLocale } from '@/composables/useI18n';
import AppLayout from '@/components/AppLayout.vue';

const bridgeReady = useBridgeReady();
const { pages, loading } = usePageRegistry(bridgeReady);

watch(bridgeReady, async (isReady) => {
  if (!isReady) return;
  try {
    const s = await settingsService.getSettings();
    setI18nLocale(s.language);
    applySettings(s);
  } catch { /* ignore */ }
});

const onSettingsChanged = (e: Event) => {
  const s = (e as CustomEvent<AppSettings>).detail;
  if (s?.language) setI18nLocale(s.language);
};

onMounted(() => window.addEventListener('app-settings-changed', onSettingsChanged));
onUnmounted(() => window.removeEventListener('app-settings-changed', onSettingsChanged));
</script>

<template>
  <div v-if="!bridgeReady || loading" class="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950">
    <div class="text-center space-y-3">
      <div class="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto" />
      <p class="text-sm text-gray-500 dark:text-gray-400">
        {{ !bridgeReady ? 'Connecting to bridge...' : 'Loading pages...' }}
      </p>
    </div>
  </div>
  <AppLayout v-else :pages="pages" />
</template>
