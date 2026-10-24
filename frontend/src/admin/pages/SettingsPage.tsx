import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { marketingApi } from '../../lib/api/marketing';
import { storeSettingsApi } from '../../lib/api/storeSettings';
import { ApiError } from '../../lib/api/client';
import type { PurchaseEventTrigger } from '../../lib/api/types';

function CredentialField({
  label,
  idLabel,
  idValue,
  onIdChange,
  isConfigured,
  tokenValue,
  onTokenChange,
  clearRequested,
  onClearToggle,
  tokenPlaceholder,
}: {
  label: string;
  idLabel: string;
  idValue: string;
  onIdChange: (value: string) => void;
  isConfigured: boolean;
  tokenValue: string;
  onTokenChange: (value: string) => void;
  clearRequested: boolean;
  onClearToggle: () => void;
  tokenPlaceholder: string;
}) {
  return (
    <div className="flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4">
      <h3 className="text-sm font-semibold">{label}</h3>
      <div>
        <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">{idLabel}</label>
        <input
          value={idValue}
          onChange={(e) => onIdChange(e.target.value)}
          className="w-full max-w-sm rounded border border-black/20 px-3 py-1.5 text-sm"
          placeholder="ex. 1234567890123456"
        />
      </div>
      <div>
        <div className="mb-1 flex items-center gap-2">
          <label className="text-xs font-medium text-luna-charcoal/70">Jeton d'accès (secret)</label>
          {isConfigured && !clearRequested && (
            <span className="rounded-full bg-green-100 px-2 py-0.5 text-[11px] text-green-700">configuré</span>
          )}
          {clearRequested && (
            <span className="rounded-full bg-red-100 px-2 py-0.5 text-[11px] text-red-700">sera supprimé</span>
          )}
        </div>
        <div className="flex max-w-sm gap-2">
          <input
            type="password"
            value={tokenValue}
            onChange={(e) => onTokenChange(e.target.value)}
            disabled={clearRequested}
            className="w-full rounded border border-black/20 px-3 py-1.5 text-sm disabled:bg-luna-cream/50"
            placeholder={isConfigured ? tokenPlaceholder : 'Aucun jeton configuré'}
          />
          {isConfigured && (
            <button
              type="button"
              onClick={onClearToggle}
              className="shrink-0 rounded border border-black/20 px-2 py-1.5 text-xs"
            >
              {clearRequested ? 'Annuler' : 'Supprimer'}
            </button>
          )}
        </div>
        <p className="mt-1 text-[11px] text-luna-charcoal/50">
          Laissez vide pour ne pas changer le jeton déjà enregistré.
        </p>
      </div>
    </div>
  );
}

function MarketingSettingsSection() {
  const queryClient = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['admin-marketing-settings'],
    queryFn: () => marketingApi.getSettings(),
  });

  const [trigger, setTrigger] = useState<PurchaseEventTrigger>('OrderCreated');
  const [metaPixelId, setMetaPixelId] = useState('');
  const [metaAccessToken, setMetaAccessToken] = useState('');
  const [clearMeta, setClearMeta] = useState(false);
  const [tikTokPixelCode, setTikTokPixelCode] = useState('');
  const [tikTokAccessToken, setTikTokAccessToken] = useState('');
  const [clearTikTok, setClearTikTok] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (settings) {
      setTrigger(settings.purchaseEventTrigger);
      setMetaPixelId(settings.metaPixelId ?? '');
      setTikTokPixelCode(settings.tikTokPixelCode ?? '');
    }
  }, [settings]);

  const save = useMutation({
    mutationFn: () =>
      marketingApi.updateSettings({
        purchaseEventTrigger: trigger,
        metaPixelId: metaPixelId.trim() || null,
        metaAccessToken: metaAccessToken.trim() || null,
        clearMetaAccessToken: clearMeta,
        tikTokPixelCode: tikTokPixelCode.trim() || null,
        tikTokAccessToken: tikTokAccessToken.trim() || null,
        clearTikTokAccessToken: clearTikTok,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-marketing-settings'], updated);
      setMetaAccessToken('');
      setClearMeta(false);
      setTikTokAccessToken('');
      setClearTikTok(false);
      setError(null);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  return (
    <section>
      <p className="mb-4 text-sm text-luna-charcoal/60">
        Facebook/TikTok Pixel et quand considérer une commande comme une vraie vente.
      </p>

      <div className="mb-6 rounded-lg border border-black/10 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold">Moment de l'événement "Achat"</h3>
        <p className="mb-3 text-xs text-luna-charcoal/60">
          Une commande COD n'est pas forcément une vraie vente dès sa création — beaucoup ne sont jamais
          confirmées (injoignable, refusée...). Choisissez quand Meta/TikTok doivent considérer la vente
          comme acquise.
        </p>
        <div className="flex flex-col gap-2">
          <label className="flex items-start gap-2 text-sm">
            <input
              type="radio"
              name="trigger"
              checked={trigger === 'OrderCreated'}
              onChange={() => setTrigger('OrderCreated')}
              className="mt-0.5"
            />
            <span>
              <span className="font-medium">À la création de la commande</span> (par défaut) — déclenché
              immédiatement dans le navigateur du client, au clic "Commander".
            </span>
          </label>
          <label className="flex items-start gap-2 text-sm">
            <input
              type="radio"
              name="trigger"
              checked={trigger === 'OrderConfirmed'}
              onChange={() => setTrigger('OrderConfirmed')}
              className="mt-0.5"
            />
            <span>
              <span className="font-medium">À la confirmation téléphonique</span> — déclenché côté
              serveur quand l'agent confirme la commande (ou immédiatement pour une commande créée déjà
              confirmée par téléphone). Nécessite un jeton d'accès Meta/TikTok ci-dessous.
            </span>
          </label>
        </div>
      </div>

      <div className="mb-4 grid gap-4">
        <CredentialField
          label="Meta (Facebook) Conversions API"
          idLabel="Pixel ID"
          idValue={metaPixelId}
          onIdChange={setMetaPixelId}
          isConfigured={settings?.isMetaAccessTokenConfigured ?? false}
          tokenValue={metaAccessToken}
          onTokenChange={setMetaAccessToken}
          clearRequested={clearMeta}
          onClearToggle={() => setClearMeta((v) => !v)}
          tokenPlaceholder="•••••••• (laisser vide = ne pas changer)"
        />
        <CredentialField
          label="TikTok Events API"
          idLabel="Pixel Code"
          idValue={tikTokPixelCode}
          onIdChange={setTikTokPixelCode}
          isConfigured={settings?.isTikTokAccessTokenConfigured ?? false}
          tokenValue={tikTokAccessToken}
          onTokenChange={setTikTokAccessToken}
          clearRequested={clearTikTok}
          onClearToggle={() => setClearTikTok((v) => !v)}
          tokenPlaceholder="•••••••• (laisser vide = ne pas changer)"
        />
      </div>

      {trigger === 'OrderConfirmed' && !settings?.isMetaAccessTokenConfigured && !settings?.isTikTokAccessTokenConfigured && (
        <p className="mb-4 rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-800">
          Aucun jeton n'est configuré : tant que c'est le cas, l'événement "Achat" à la confirmation ne
          sera pas réellement envoyé (aucun faux succès) — seul l'enregistrement interne aura lieu.
        </p>
      )}

      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      {saved && <p className="mb-4 text-sm text-green-600">Enregistré.</p>}

      <button
        type="button"
        onClick={() => save.mutate()}
        disabled={save.isPending}
        className="rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-50"
      >
        {save.isPending ? 'Enregistrement...' : 'Enregistrer'}
      </button>
    </section>
  );
}

function StoreSettingsSection() {
  const queryClient = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['admin-store-settings'],
    queryFn: () => storeSettingsApi.get(),
  });

  const [instagramUrl, setInstagramUrl] = useState('');
  const [tikTokUrl, setTikTokUrl] = useState('');
  const [facebookUrl, setFacebookUrl] = useState('');
  const [contactPhone, setContactPhone] = useState('');
  const [announcementTextFr, setAnnouncementTextFr] = useState('');
  const [announcementTextAr, setAnnouncementTextAr] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (settings) {
      setInstagramUrl(settings.instagramUrl ?? '');
      setTikTokUrl(settings.tikTokUrl ?? '');
      setFacebookUrl(settings.facebookUrl ?? '');
      setContactPhone(settings.contactPhone ?? '');
      setAnnouncementTextFr(settings.announcementTextFr ?? '');
      setAnnouncementTextAr(settings.announcementTextAr ?? '');
    }
  }, [settings]);

  const save = useMutation({
    mutationFn: () =>
      storeSettingsApi.update({
        instagramUrl: instagramUrl.trim() || null,
        tikTokUrl: tikTokUrl.trim() || null,
        facebookUrl: facebookUrl.trim() || null,
        contactPhone: contactPhone.trim() || null,
        announcementTextFr: announcementTextFr.trim() || null,
        announcementTextAr: announcementTextAr.trim() || null,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['admin-store-settings'], updated);
      // The public storefront layout caches this under the same shape — refresh it too so a
      // change here shows up without the customer having to hard-refresh their own tab later.
      queryClient.invalidateQueries({ queryKey: ['store-settings'] });
      setError(null);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    },
    onError: (err) => setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.'),
  });

  if (isLoading) {
    return <p className="text-sm text-luna-charcoal/60">Chargement...</p>;
  }

  return (
    <section>
      <p className="mb-4 text-sm text-luna-charcoal/60">
        Réseaux sociaux, numéro de contact et texte de la barre d'annonce — visibles immédiatement
        sur le site, sans redéploiement.
      </p>

      <div className="mb-4 flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4">
        <h3 className="text-sm font-semibold">Réseaux sociaux &amp; contact</h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">Instagram (URL)</label>
            <input
              value={instagramUrl}
              onChange={(e) => setInstagramUrl(e.target.value)}
              placeholder="https://instagram.com/votre-boutique"
              className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">TikTok (URL)</label>
            <input
              value={tikTokUrl}
              onChange={(e) => setTikTokUrl(e.target.value)}
              placeholder="https://tiktok.com/@votre-boutique"
              className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">Facebook (URL)</label>
            <input
              value={facebookUrl}
              onChange={(e) => setFacebookUrl(e.target.value)}
              placeholder="https://facebook.com/votre-boutique"
              className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">Numéro de téléphone</label>
            <input
              value={contactPhone}
              onChange={(e) => setContactPhone(e.target.value)}
              placeholder="0551234567"
              className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
            />
          </div>
        </div>
        <p className="text-[11px] text-luna-charcoal/50">
          Un champ laissé vide n'affiche simplement pas son icône/lien dans le pied de page du site.
        </p>
      </div>

      <div className="mb-4 flex flex-col gap-3 rounded-lg border border-black/10 bg-white p-4">
        <h3 className="text-sm font-semibold">Barre d'annonce</h3>
        <p className="text-xs text-luna-charcoal/60">
          Le bandeau noir en haut du site. Laissez vide pour garder le texte par défaut ("Livraison 58
          wilayas · Paiement à la livraison").
        </p>
        <div>
          <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">Texte (français)</label>
          <input
            value={announcementTextFr}
            onChange={(e) => setAnnouncementTextFr(e.target.value)}
            placeholder="Livraison 58 wilayas · Paiement à la livraison"
            maxLength={200}
            className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-luna-charcoal/70">Texte (arabe)</label>
          <input
            value={announcementTextAr}
            onChange={(e) => setAnnouncementTextAr(e.target.value)}
            placeholder="التوصيل في 58 ولاية · الدفع عند الاستلام"
            dir="rtl"
            maxLength={200}
            className="w-full rounded border border-black/20 px-3 py-1.5 text-sm"
          />
        </div>
      </div>

      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      {saved && <p className="mb-4 text-sm text-green-600">Enregistré.</p>}

      <button
        type="button"
        onClick={() => save.mutate()}
        disabled={save.isPending}
        className="rounded-full bg-luna-black px-5 py-2 text-sm text-white disabled:opacity-50"
      >
        {save.isPending ? 'Enregistrement...' : 'Enregistrer'}
      </button>
    </section>
  );
}

type SettingsTab = 'marketing' | 'vitrine';

const TABS: { id: SettingsTab; label: string }[] = [
  { id: 'marketing', label: 'Marketing' },
  { id: 'vitrine', label: 'Vitrine' },
];

export function SettingsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab: SettingsTab = searchParams.get('tab') === 'vitrine' ? 'vitrine' : 'marketing';

  return (
    <div className="mx-auto max-w-2xl p-6">
      <h1 className="mb-4 text-lg font-semibold">Paramètres</h1>

      <div className="mb-6 flex gap-1 border-b border-black/10">
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setSearchParams(t.id === 'marketing' ? {} : { tab: t.id })}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              tab === t.id
                ? 'border-b-2 border-luna-black text-luna-black'
                : 'text-luna-charcoal/50 hover:text-luna-black'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'marketing' ? <MarketingSettingsSection /> : <StoreSettingsSection />}
    </div>
  );
}
