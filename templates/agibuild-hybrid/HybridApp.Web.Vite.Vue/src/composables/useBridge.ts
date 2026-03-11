import { ref, onMounted } from "vue";
import { ready } from "@agibuild/bridge";

export function useBridgeReady(timeoutMs = 3000) {
  const isReady = ref(false);
  const error = ref<string | null>(null);

  onMounted(() => {
    ready({ timeoutMs })
      .then(() => {
        isReady.value = true;
      })
      .catch((err: Error) => {
        error.value = err.message;
      });
  });

  return { ready: isReady, error };
}
