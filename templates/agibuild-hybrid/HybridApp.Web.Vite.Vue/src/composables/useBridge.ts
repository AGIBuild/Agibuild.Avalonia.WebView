import { ref, onMounted } from "vue";
import { bridge } from "../bridge/client";

export function useBridgeReady(timeoutMs = 3000) {
  const ready = ref(false);
  const error = ref<string | null>(null);

  onMounted(() => {
    bridge
      .ready({ timeoutMs })
      .then(() => {
        ready.value = true;
      })
      .catch((err: Error) => {
        error.value = err.message;
      });
  });

  return { ready, error };
}
