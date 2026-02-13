import { NavLink } from 'react-router';
import { useState, useEffect, useCallback } from 'react';
import {
  LayoutDashboard, MessageSquare, FolderOpen, Settings as SettingsIcon,
  Moon, Sun, PanelLeftClose, PanelLeft,
} from 'lucide-react';
import type { PageDefinition, AppSettings } from '../bridge/services';
import { appShellService, settingsService } from '../bridge/services';
import { applySettings } from '../applySettings';
import { useI18n } from '../i18n/I18nContext';
import type { TranslationKey } from '../i18n/translations';

/** Map icon names (from C#) to Lucide icon components. */
const ICONS: Record<string, React.ComponentType<{ className?: string }>> = {
  LayoutDashboard,
  MessageSquare,
  FolderOpen,
  Settings: SettingsIcon,
};

interface LayoutProps {
  pages: PageDefinition[];
  children: React.ReactNode;
}

export function Layout({ pages, children }: LayoutProps) {
  const { t } = useI18n();
  const [collapsed, setCollapsed] = useState(false);
  const [dark, setDark] = useState(false);
  const [appName, setAppName] = useState('Hybrid Demo');

  // Load persisted settings on mount and apply them.
  useEffect(() => {
    settingsService.getSettings().then((s) => {
      setCollapsed(s.sidebarCollapsed);
      applySettings(s);
      // Sync local dark state with applied theme.
      setDark(document.documentElement.classList.contains('dark'));
    }).catch(() => {
      // Fallback: use system preference if settings unavailable.
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      setDark(prefersDark);
      if (prefersDark) document.documentElement.classList.add('dark');
    });
  }, []);

  // Listen for settings changes dispatched by the Settings page.
  useEffect(() => {
    const handler = (e: Event) => {
      const s = (e as CustomEvent<AppSettings>).detail;
      setCollapsed(s.sidebarCollapsed);
      setDark(document.documentElement.classList.contains('dark'));
    };
    window.addEventListener('app-settings-changed', handler);
    return () => window.removeEventListener('app-settings-changed', handler);
  }, []);

  useEffect(() => {
    appShellService.getAppInfo().then((info) => setAppName(info.name)).catch(() => {});
  }, []);

  // Register JsImport handlers for C# → JS callbacks
  useEffect(() => {
    const w = window as unknown as {
      agWebView?: { rpc?: { handle: (method: string, handler: (params: unknown) => unknown) => void } };
    };
    const rpc = w.agWebView?.rpc;
    if (!rpc) return;

    // IUiNotificationService.ShowNotification
    rpc.handle('UiNotificationService.showNotification', (params: unknown) => {
      const p = params as { message?: string; type?: string };
      showToast(p.message ?? 'Notification', p.type ?? 'info');
      return undefined;
    });

    // IThemeService.SetTheme
    rpc.handle('ThemeService.setTheme', (params: unknown) => {
      const p = params as { theme?: string };
      const isDark = p.theme === 'dark';
      setDark(isDark);
      document.documentElement.classList.toggle('dark', isDark);
      return undefined;
    });
  }, []);

  const toggleCollapsed = useCallback(() => {
    setCollapsed((prev) => {
      const next = !prev;
      // Persist the change.
      settingsService.getSettings().then((s) => {
        settingsService.updateSettings({ ...s, sidebarCollapsed: next }).catch(() => {});
      }).catch(() => {});
      return next;
    });
  }, []);

  const toggleDark = useCallback(() => {
    setDark((prev) => {
      const next = !prev;
      document.documentElement.classList.toggle('dark', next);
      // Persist the theme choice.
      settingsService.getSettings().then((s) => {
        settingsService.updateSettings({ ...s, theme: next ? 'dark' : 'light' }).catch(() => {});
      }).catch(() => {});
      return next;
    });
  }, []);

  return (
    <div className="flex flex-col md:flex-row h-screen overflow-hidden bg-gray-50 dark:bg-gray-950">

      {/* ══════ Mobile: Top Header ══════ */}
      <header className="flex md:hidden items-center justify-between px-4 h-12 border-b border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 shrink-0">
        <div className="flex items-center gap-2">
          <div className="w-6 h-6 rounded-md bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-[10px] font-bold shrink-0">
            A
          </div>
          <span className="text-sm font-semibold truncate">{appName}</span>
        </div>
        <button
          onClick={toggleDark}
          className="p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          {dark ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </button>
      </header>

      {/* ══════ Desktop: Sidebar (hidden on mobile) ══════ */}
      <aside
        className={`hidden md:flex flex-col border-r border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 transition-all duration-200 ${
          collapsed ? 'w-16' : 'w-56'
        }`}
      >
        {/* Logo area */}
        <div className="flex items-center gap-2 px-4 h-14 border-b border-gray-200 dark:border-gray-800">
          <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
            A
          </div>
          {!collapsed && (
            <span className="text-sm font-semibold truncate">{appName}</span>
          )}
        </div>

        {/* Navigation */}
        <nav className="flex-1 py-2 space-y-0.5 px-2">
          {pages.map((page) => {
            const Icon = ICONS[page.icon] ?? LayoutDashboard;
            return (
              <NavLink
                key={page.id}
                to={page.route}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                    isActive
                      ? 'bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400 font-medium'
                      : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800'
                  }`
                }
              >
                <Icon className="w-5 h-5 shrink-0" />
                {!collapsed && <span className="truncate">{t(`page.${page.id}` as TranslationKey)}</span>}
              </NavLink>
            );
          })}
        </nav>

        {/* Bottom actions */}
        <div className="border-t border-gray-200 dark:border-gray-800 p-2 space-y-0.5">
          <button
            onClick={toggleDark}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 w-full"
          >
            {dark ? <Sun className="w-5 h-5 shrink-0" /> : <Moon className="w-5 h-5 shrink-0" />}
            {!collapsed && <span>{dark ? t('layout.lightMode') : t('layout.darkMode')}</span>}
          </button>
          <button
            onClick={toggleCollapsed}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 w-full"
          >
            {collapsed ? (
              <PanelLeft className="w-5 h-5 shrink-0" />
            ) : (
              <PanelLeftClose className="w-5 h-5 shrink-0" />
            )}
            {!collapsed && <span>{t('layout.collapse')}</span>}
          </button>
        </div>
      </aside>

      {/* ══════ Main content ══════ */}
      <main className="flex-1 overflow-auto pb-14 md:pb-0">{children}</main>

      {/* ══════ Mobile: Bottom Tab Bar (hidden on desktop) ══════ */}
      <nav className="flex md:hidden items-stretch border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 h-14 shrink-0">
        {pages.map((page) => {
          const Icon = ICONS[page.icon] ?? LayoutDashboard;
          return (
            <NavLink
              key={page.id}
              to={page.route}
              className={({ isActive }) =>
                `flex-1 flex flex-col items-center justify-center gap-0.5 text-[10px] transition-colors ${
                  isActive
                    ? 'text-blue-600 dark:text-blue-400'
                    : 'text-gray-400 dark:text-gray-500'
                }`
              }
            >
              <Icon className="w-5 h-5" />
              <span>{t(`page.${page.id}` as TranslationKey)}</span>
            </NavLink>
          );
        })}
      </nav>

      {/* Toast container — offset above bottom tab on mobile */}
      <div id="toast-container" className="fixed bottom-18 md:bottom-4 right-4 space-y-2 z-50" />
    </div>
  );
}

// ─── Toast helper ─────────────────────────────────────────────────────────────

function showToast(message: string, type: string) {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const colors: Record<string, string> = {
    info: 'bg-blue-500',
    success: 'bg-green-500',
    warning: 'bg-amber-500',
    error: 'bg-red-500',
  };

  const toast = document.createElement('div');
  toast.className = `${colors[type] ?? colors['info']} text-white px-4 py-2 rounded-lg shadow-lg text-sm transform transition-all duration-300 translate-y-2 opacity-0`;
  toast.textContent = message;
  container.appendChild(toast);

  requestAnimationFrame(() => {
    toast.classList.remove('translate-y-2', 'opacity-0');
  });

  setTimeout(() => {
    toast.classList.add('translate-y-2', 'opacity-0');
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}
