import { useState, useEffect } from 'react';
import { bridgeClient } from '@agibuild/bridge';

/** Returns true once the Agibuild WebView Bridge is ready. */
export function useBridgeReady(): boolean {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const check = async () => {
      try {
        await bridgeClient.ready({ timeoutMs: 10_000, pollIntervalMs: 50 });
        if (!cancelled) {
          setReady(true);
        }
      } catch {
        if (!cancelled) {
          setReady(false);
        }
      }
    };

    void check();
    return () => {
      cancelled = true;
    };
  }, []);

  return ready;
}
