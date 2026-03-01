import { ref, computed, type Ref } from 'vue';
import { getTranslations, type TranslationKey } from '@/i18n/translations';

const locale = ref('en');

export function setI18nLocale(l: string) {
  locale.value = l;
  document.documentElement.lang = l;
}

export function useI18n() {
  const t = (key: TranslationKey, params?: Record<string, string | number>): string => {
    const dict = getTranslations(locale.value);
    let text = dict[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        text = text.replace(`{${k}}`, String(v));
      }
    }
    return text;
  };

  return {
    locale: locale as Ref<string>,
    setLocale: setI18nLocale,
    t,
  };
}
