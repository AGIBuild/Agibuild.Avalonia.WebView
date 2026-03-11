import { useEffect, useState } from "react";
import { ready } from "@agibuild/bridge";

export function useBridgeReady(timeoutMs = 3000): {
  ready: boolean;
  error: string | null;
} {
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    ready({ timeoutMs })
      .then(() => {
        if (!cancelled) setReady(true);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [timeoutMs]);

  return { ready, error };
}
