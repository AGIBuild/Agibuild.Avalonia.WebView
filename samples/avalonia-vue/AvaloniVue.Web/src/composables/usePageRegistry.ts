import { ref, watch, type Ref } from 'vue';
import { appShellService, type PageDefinition } from '@/bridge/services';

/** Fetches the page list from C# AppShellService once bridge is ready. */
export function usePageRegistry(bridgeReady: Ref<boolean>) {
  const pages = ref<PageDefinition[]>([]);
  const loading = ref(true);

  watch(bridgeReady, async (isReady) => {
    if (!isReady) return;
    try {
      pages.value = await appShellService.getPages();
    } catch (e) {
      console.error(e);
    } finally {
      loading.value = false;
    }
  }, { immediate: true });

  return { pages, loading };
}
