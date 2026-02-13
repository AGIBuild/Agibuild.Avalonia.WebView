import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { useBridgeReady } from './hooks/useBridge';
import { usePageRegistry } from './hooks/usePageRegistry';
import { Layout } from './components/Layout';
import { Dashboard } from './pages/Dashboard';
import { Chat } from './pages/Chat';
import { Files } from './pages/Files';
import { Settings } from './pages/Settings';
import { I18nProvider } from './i18n/I18nContext';
import { settingsService } from './bridge/services';
import { applySettings } from './applySettings';

/** Map page IDs to React components. Add new pages here. */
const PAGE_COMPONENTS: Record<string, React.ComponentType> = {
  dashboard: Dashboard,
  chat: Chat,
  files: Files,
  settings: Settings,
};

export function App() {
  const bridgeReady = useBridgeReady();
  const { pages, loading } = usePageRegistry(bridgeReady);
  const [locale, setLocale] = useState('en');

  // Load persisted settings once bridge is ready.
  useEffect(() => {
    if (!bridgeReady) return;
    settingsService.getSettings().then((s) => {
      setLocale(s.language);
      applySettings(s);
    }).catch(() => {});
  }, [bridgeReady]);

  // Listen for settings changes from the Settings page.
  useEffect(() => {
    const handler = (e: Event) => {
      const s = (e as CustomEvent).detail;
      if (s?.language) setLocale(s.language);
    };
    window.addEventListener('app-settings-changed', handler);
    return () => window.removeEventListener('app-settings-changed', handler);
  }, []);

  if (!bridgeReady || loading) {
    return (
      <div className="flex items-center justify-center h-screen bg-gray-50 dark:bg-gray-950">
        <div className="text-center space-y-3">
          <div className="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {!bridgeReady ? 'Connecting to bridge...' : 'Loading pages...'}
          </p>
        </div>
      </div>
    );
  }

  return (
    <I18nProvider initialLocale={locale}>
      <BrowserRouter>
        <Layout pages={pages}>
          <Routes>
            {pages.map((page) => {
              const Component = PAGE_COMPONENTS[page.id];
              if (!Component) return null;
              return <Route key={page.id} path={page.route} element={<Component />} />;
            })}
            {pages.length > 0 && (
              <Route path="*" element={<Navigate to={pages[0]!.route} replace />} />
            )}
          </Routes>
        </Layout>
      </BrowserRouter>
    </I18nProvider>
  );
}
