import { useTranslation } from 'react-i18next';
import { Languages } from 'lucide-react';

import { SUPPORTED_LANGUAGES, type SupportedLanguage } from './index';

const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  fr: 'FR',
  ar: 'عربي',
};

export function LanguageSwitcher({ className = '' }: { className?: string }) {
  const { i18n } = useTranslation();
  const current = (i18n.language === 'ar' ? 'ar' : 'fr') as SupportedLanguage;

  function toggle() {
    const next = current === 'fr' ? 'ar' : 'fr';
    i18n.changeLanguage(next);
  }

  return (
    <button
      type="button"
      onClick={toggle}
      className={`flex items-center gap-1.5 text-sm text-luna-black ${className}`}
      aria-label="Changer de langue / تغيير اللغة"
    >
      <Languages className="h-4 w-4" />
      <span className="flex items-center gap-1">
        {SUPPORTED_LANGUAGES.map((lang) => (
          <span key={lang} className={lang === current ? 'font-semibold' : 'text-luna-charcoal/40'}>
            {LANGUAGE_LABELS[lang]}
          </span>
        ))}
      </span>
    </button>
  );
}
