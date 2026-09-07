import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import fr from './fr.json';
import ar from './ar.json';

export const SUPPORTED_LANGUAGES = ['fr', 'ar'] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

const STORAGE_KEY = 'luna-language';
const RTL_LANGUAGES: readonly SupportedLanguage[] = ['ar'];

export function isRtl(language: string): boolean {
  return RTL_LANGUAGES.includes(language as SupportedLanguage);
}

function getInitialLanguage(): SupportedLanguage {
  const stored = localStorage.getItem(STORAGE_KEY);
  return stored === 'ar' ? 'ar' : 'fr';
}

export function applyDocumentDirection(language: string): void {
  document.documentElement.lang = language;
  document.documentElement.dir = isRtl(language) ? 'rtl' : 'ltr';
}

const initialLanguage = getInitialLanguage();

i18n.use(initReactI18next).init({
  resources: {
    fr: { translation: fr },
    ar: { translation: ar },
  },
  lng: initialLanguage,
  fallbackLng: 'fr',
  interpolation: { escapeValue: false },
});

applyDocumentDirection(initialLanguage);

i18n.on('languageChanged', (language) => {
  localStorage.setItem(STORAGE_KEY, language);
  applyDocumentDirection(language);
});

export default i18n;
