interface PagePlaceholderProps {
  title: string;
  description?: string;
}

export function PagePlaceholder({ title, description }: PagePlaceholderProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-4 py-24 text-center">
      <h1 className="text-xl font-medium text-luna-black">{title}</h1>
      <p className="text-sm text-luna-charcoal/70">{description ?? 'Cette page sera bientôt disponible.'}</p>
    </div>
  );
}
