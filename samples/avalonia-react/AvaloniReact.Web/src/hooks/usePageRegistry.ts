import { useState, useEffect } from 'react';
import { appShellService, type PageDefinition } from '../bridge/services';

/** Fetches the page list from C# AppShellService once bridge is ready. */
export function usePageRegistry(bridgeReady: boolean) {
  const [pages, setPages] = useState<PageDefinition[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!bridgeReady) return;

    appShellService.getPages()
      .then(setPages)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [bridgeReady]);

  return { pages, loading };
}
