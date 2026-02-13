import { createContext, useContext, useState, useCallback, useMemo, useEffect } from 'react';
import { getTranslations, type TranslationKey } from './translations';

interface I18nContextValue {
  locale: string;
  setLocale: (locale: string) => void;
  t: (key: TranslationKey, params?: Record<string, string | number>) => string;
}

const I18nCtx = createContext<I18nContextValue | null>(null);

export function I18nProvider({ initialLocale, children }: { initialLocale: string; children: React.ReactNode }) {
  const [locale, setLocaleState] = useState(initialLocale);

  // Sync when parent changes initialLocale (e.g. settings loaded).
  useEffect(() => {
    setLocaleState(initialLocale);
  }, [initialLocale]);

  const setLocale = useCallback((l: string) => {
    setLocaleState(l);
    document.documentElement.lang = l;
  }, []);

  const t = useCallback((key: TranslationKey, params?: Record<string, string | number>) => {
    const dict = getTranslations(locale);
    let text = dict[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        text = text.replace(`{${k}}`, String(v));
      }
    }
    return text;
  }, [locale]);

  const value = useMemo(() => ({ locale, setLocale, t }), [locale, setLocale, t]);

  return <I18nCtx.Provider value={value}>{children}</I18nCtx.Provider>;
}

export function useI18n(): I18nContextValue {
  const ctx = useContext(I18nCtx);
  if (!ctx) throw new Error('useI18n must be used within I18nProvider');
  return ctx;
}
