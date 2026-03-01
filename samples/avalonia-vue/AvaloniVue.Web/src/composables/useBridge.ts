import { ref, onMounted } from 'vue';
import { bridgeClient } from '@agibuild/bridge';

/** Returns a reactive ref that becomes true once the Agibuild WebView Bridge is ready. */
export function useBridgeReady() {
  const ready = ref(false);

  onMounted(async () => {
    try {
      await bridgeClient.ready({ timeoutMs: 10_000, pollIntervalMs: 50 });
      ready.value = true;
    } catch {
      ready.value = false;
    }
  });

  return ready;
}
