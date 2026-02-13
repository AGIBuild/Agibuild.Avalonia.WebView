import { useState, useEffect } from 'react';

/** Returns true once the Agibuild WebView Bridge is ready. */
export function useBridgeReady(): boolean {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const check = () => {
      const w = window as unknown as { agWebView?: { rpc?: unknown } };
      if (w.agWebView?.rpc) {
        setReady(true);
        return;
      }
      setTimeout(check, 50);
    };
    check();
  }, []);

  return ready;
}
