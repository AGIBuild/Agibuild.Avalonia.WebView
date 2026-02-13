import { useState, useEffect } from 'react';
import { Save, Check } from 'lucide-react';
import { settingsService, type AppSettings } from '../bridge/services';

export function Settings() {
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    settingsService.getSettings().then(setSettings).catch(() => {});
  }, []);

  const handleSave = async () => {
    if (!settings) return;
    setSaving(true);
    try {
      const updated = await settingsService.updateSettings(settings);
      setSettings(updated);
      setSaved(true);

      // Apply theme
      if (updated.theme === 'dark') {
        document.documentElement.classList.add('dark');
      } else if (updated.theme === 'light') {
        document.documentElement.classList.remove('dark');
      }

      setTimeout(() => setSaved(false), 2000);
    } finally {
      setSaving(false);
    }
  };

  if (!settings) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="w-6 h-6 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          Preferences persisted via <code className="text-xs bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">[JsExport] ISettingsService</code>
        </p>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 divide-y divide-gray-100 dark:divide-gray-800">
        {/* Theme */}
        <SettingRow label="Theme" description="Choose light, dark, or follow system preference.">
          <select
            value={settings.theme}
            onChange={(e) => setSettings({ ...settings, theme: e.target.value })}
            className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
          >
            <option value="system">System</option>
            <option value="light">Light</option>
            <option value="dark">Dark</option>
          </select>
        </SettingRow>

        {/* Language */}
        <SettingRow label="Language" description="Interface language preference.">
          <select
            value={settings.language}
            onChange={(e) => setSettings({ ...settings, language: e.target.value })}
            className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
          >
            <option value="en">English</option>
            <option value="zh">Chinese</option>
            <option value="ja">Japanese</option>
            <option value="ko">Korean</option>
          </select>
        </SettingRow>

        {/* Font Size */}
        <SettingRow label="Font Size" description={`Current: ${settings.fontSize}px`}>
          <input
            type="range"
            min={12}
            max={20}
            value={settings.fontSize}
            onChange={(e) => setSettings({ ...settings, fontSize: Number(e.target.value) })}
            className="w-32 accent-blue-500"
          />
        </SettingRow>

        {/* Sidebar Collapsed */}
        <SettingRow label="Compact Sidebar" description="Start with sidebar collapsed.">
          <button
            onClick={() => setSettings({ ...settings, sidebarCollapsed: !settings.sidebarCollapsed })}
            className={`relative w-10 h-6 rounded-full transition-colors ${
              settings.sidebarCollapsed ? 'bg-blue-500' : 'bg-gray-200 dark:bg-gray-700'
            }`}
          >
            <div
              className={`absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform ${
                settings.sidebarCollapsed ? 'translate-x-4.5' : 'translate-x-0.5'
              }`}
            />
          </button>
        </SettingRow>
      </div>

      {/* Save button */}
      <button
        onClick={() => { void handleSave(); }}
        disabled={saving}
        className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-500 text-white text-sm font-medium hover:bg-blue-600 disabled:opacity-50 transition-colors"
      >
        {saved ? <Check className="w-4 h-4" /> : <Save className="w-4 h-4" />}
        {saved ? 'Saved!' : saving ? 'Saving...' : 'Save Settings'}
      </button>

      {/* How it works */}
      <div className="bg-blue-50 dark:bg-blue-500/5 border border-blue-200 dark:border-blue-800 rounded-xl p-5 text-sm">
        <p className="font-medium text-blue-700 dark:text-blue-300">How this works</p>
        <p className="mt-1 text-blue-600 dark:text-blue-400">
          Settings are read/written via the C# <code className="bg-blue-100 dark:bg-blue-500/20 px-1 rounded">SettingsService</code>, which persists them as JSON in the user's app data directory. The <code className="bg-blue-100 dark:bg-blue-500/20 px-1 rounded">IThemeService [JsImport]</code> allows C# to push theme changes to React.
        </p>
      </div>
    </div>
  );
}

function SettingRow({ label, description, children }: {
  label: string;
  description: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between px-5 py-4">
      <div>
        <p className="text-sm font-medium">{label}</p>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{description}</p>
      </div>
      {children}
    </div>
  );
}
