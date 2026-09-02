import type { MarketingAttribution } from '../api/types';

const STORAGE_KEY = 'luna-marketing-attribution';

const UTM_PARAM_KEYS: Record<keyof Pick<MarketingAttribution, 'utmSource' | 'utmMedium' | 'utmCampaign' | 'utmContent' | 'utmTerm'>, string> = {
  utmSource: 'utm_source',
  utmMedium: 'utm_medium',
  utmCampaign: 'utm_campaign',
  utmContent: 'utm_content',
  utmTerm: 'utm_term',
};

/**
 * Captures utm_* params, fbclid, ttclid, referrer, and landingPage from the URL on first landing
 * and persists it in sessionStorage — first-touch model: if attribution was already captured this session, later
 * page loads (even with different query params) don't overwrite it, so the original ad click still
 * gets credit for the eventual order. Call once, as early as possible (StorefrontLayout mount).
 */
export function captureAttributionOnLoad(): void {
  if (typeof window === 'undefined') return;
  if (sessionStorage.getItem(STORAGE_KEY)) return;

  const params = new URLSearchParams(window.location.search);
  const attribution: MarketingAttribution = {};

  for (const [key, param] of Object.entries(UTM_PARAM_KEYS) as [keyof typeof UTM_PARAM_KEYS, string][]) {
    const value = params.get(param);
    if (value) attribution[key] = value;
  }

  const fbclid = params.get('fbclid');
  if (fbclid) attribution.fbclid = fbclid;

  const ttclid = params.get('ttclid');
  if (ttclid) attribution.ttclid = ttclid;

  if (document.referrer) attribution.referrer = document.referrer;
  attribution.landingPage = window.location.href;

  const hasAnyValue = Object.keys(attribution).length > 0;
  if (hasAnyValue) {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(attribution));
  }
}

export function getStoredAttribution(): MarketingAttribution | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as MarketingAttribution) : null;
  } catch {
    return null;
  }
}
