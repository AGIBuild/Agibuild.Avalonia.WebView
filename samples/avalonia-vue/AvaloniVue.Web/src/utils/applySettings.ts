import type { AppSettings } from '@/bridge/services';

/**
 * Applies all settings to the DOM.
 * Called when settings are loaded at startup and after each save.
 */
export function applySettings(settings: AppSettings): void {
  if (settings.theme === 'dark') {
    document.documentElement.classList.add('dark');
  } else if (settings.theme === 'light') {
    document.documentElement.classList.remove('dark');
  } else {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    document.documentElement.classList.toggle('dark', prefersDark);
  }

  document.documentElement.style.setProperty('--app-font-size', `${settings.fontSize}px`);
  document.documentElement.lang = settings.language;
}

/**
 * Dispatches a custom event so other components (e.g. Layout) can react
 * to settings changes without tight coupling.
 */
export function dispatchSettingsChanged(settings: AppSettings): void {
  window.dispatchEvent(new CustomEvent('app-settings-changed', { detail: settings }));
}
