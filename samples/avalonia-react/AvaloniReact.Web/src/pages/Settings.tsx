import { useState, useEffect } from 'react';
import { Save, Check } from 'lucide-react';
import { settingsService, type AppSettings } from '../bridge/services';
import { applySettings, dispatchSettingsChanged } from '../applySettings';
import { useI18n } from '../i18n/I18nContext';

export function Settings() {
  const { t, setLocale } = useI18n();
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    settingsService.getSettings().then((s) => {
      setSettings(s);
      applySettings(s);
    }).catch(() => {});
  }, []);

  const handleSave = async () => {
    if (!settings) return;
    setSaving(true);
    try {
      const updated = await settingsService.updateSettings(settings);
      setSettings(updated);
      applySettings(updated);
      dispatchSettingsChanged(updated);
      setSaved(true);
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
        <h1 className="text-2xl font-bold">{t('settings.title')}</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {t('settings.subtitle')} <code className="text-xs bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">[JsExport] ISettingsService</code>
        </p>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 divide-y divide-gray-100 dark:divide-gray-800">
        {/* Theme */}
        <SettingRow label={t('settings.theme')} description={t('settings.themeDesc')}>
          <select
            value={settings.theme}
            onChange={(e) => setSettings({ ...settings, theme: e.target.value })}
            className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
          >
            <option value="system">{t('settings.themeSystem')}</option>
            <option value="light">{t('settings.themeLight')}</option>
            <option value="dark">{t('settings.themeDark')}</option>
          </select>
        </SettingRow>

        {/* Language */}
        <SettingRow label={t('settings.language')} description={t('settings.languageDesc')}>
          <select
            value={settings.language}
            onChange={(e) => {
              const lang = e.target.value;
              setSettings({ ...settings, language: lang });
              setLocale(lang);
            }}
            className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
          >
            <option value="en">English</option>
            <option value="zh">中文</option>
            <option value="ja">日本語</option>
            <option value="ko">한국어</option>
          </select>
        </SettingRow>

        {/* Font Size */}
        <SettingRow label={t('settings.fontSize')} description={t('settings.fontSizeDesc', { size: settings.fontSize })}>
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
        <SettingRow label={t('settings.sidebar')} description={t('settings.sidebarDesc')}>
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
        {saved ? t('settings.saved') : saving ? t('settings.saving') : t('settings.save')}
      </button>

      {/* How it works */}
      <div className="bg-blue-50 dark:bg-blue-500/5 border border-blue-200 dark:border-blue-800 rounded-xl p-5 text-sm">
        <p className="font-medium text-blue-700 dark:text-blue-300">{t('settings.howTitle')}</p>
        <p className="mt-1 text-blue-600 dark:text-blue-400">
          {t('settings.howBody')}
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
