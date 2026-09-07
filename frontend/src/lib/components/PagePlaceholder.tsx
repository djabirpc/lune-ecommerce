import { useTranslation } from 'react-i18next';

interface PagePlaceholderProps {
  title: string;
  description?: string;
}

export function PagePlaceholder({ title, description }: PagePlaceholderProps) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-4 py-24 text-center">
      <h1 className="text-xl font-medium text-luna-black">{title}</h1>
      <p className="text-sm text-luna-charcoal/70">{description ?? t('common.pageComingSoon')}</p>
    </div>
  );
}
