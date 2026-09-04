/**
 * Meta Pixel / TikTok Pixel loader (CLAUDE.md section 21). Scripts are only injected when a real
 * Pixel ID is configured via VITE_META_PIXEL_ID / VITE_TIKTOK_PIXEL_ID — with no ID, this is a
 * silent no-op rather than injecting a pixel pointed at nothing. Same "don't fake an integration
 * you can't actually run" principle as the Yalidine/ZR Express shipping adapters.
 */

declare global {
  interface Window {
    fbq?: (...args: unknown[]) => void;
    _fbq?: unknown;
    ttq?: {
      load: (pixelId: string) => void;
      page: () => void;
      track: (event: string, params?: Record<string, unknown>) => void;
      [key: string]: unknown;
    };
  }
}

export type TrackedEvent = 'PAGE_VIEW' | 'VIEW_CONTENT' | 'ADD_TO_CART' | 'INITIATE_CHECKOUT' | 'ORDER_CREATED';

const META_EVENT_NAMES: Record<TrackedEvent, string> = {
  PAGE_VIEW: 'PageView',
  VIEW_CONTENT: 'ViewContent',
  ADD_TO_CART: 'AddToCart',
  INITIATE_CHECKOUT: 'InitiateCheckout',
  ORDER_CREATED: 'Purchase',
};

const TIKTOK_EVENT_NAMES: Record<TrackedEvent, string> = {
  PAGE_VIEW: 'PageView',
  VIEW_CONTENT: 'ViewContent',
  ADD_TO_CART: 'AddToCart',
  INITIATE_CHECKOUT: 'InitiateCheckout',
  ORDER_CREATED: 'PlaceAnOrder',
};

let initialized = false;
const initedMetaPixelIds = new Set<string>();
const initedTikTokPixelIds = new Set<string>();

export function initPixels(): void {
  if (initialized || typeof window === 'undefined') return;
  initialized = true;

  const metaPixelId = import.meta.env.VITE_META_PIXEL_ID as string | undefined;
  if (metaPixelId) {
    initMetaPixelId(metaPixelId);
    window.fbq?.('track', 'PageView');
  }

  const tiktokPixelId = import.meta.env.VITE_TIKTOK_PIXEL_ID as string | undefined;
  if (tiktokPixelId) {
    initTikTokPixelId(tiktokPixelId);
  }
}

/**
 * Registers an additional, product-specific Pixel ID alongside the site-wide one (CLAUDE.md
 * section 21 + admin product form). Meta/TikTok both support multiple simultaneously-inited
 * pixel IDs: once inited, both the site-wide and product pixel receive every subsequent
 * trackEvent() call. Dedup guards avoid re-initing the same ID (e.g. on re-render/re-navigation).
 */
export function initProductPixels(facebookPixelId?: string | null, tiktokPixelId?: string | null): void {
  if (typeof window === 'undefined') return;
  if (facebookPixelId) initMetaPixelId(facebookPixelId);
  if (tiktokPixelId) initTikTokPixelId(tiktokPixelId);
}

function ensureMetaScriptLoaded(): void {
  /* eslint-disable */
  (function (f: any, b: Document, e: string, v: string) {
    if (f.fbq) return;
    const n: any = (f.fbq = function (...args: unknown[]) {
      n.callMethod ? n.callMethod.apply(n, args) : n.queue.push(args);
    });
    if (!f._fbq) f._fbq = n;
    n.push = n;
    n.loaded = true;
    n.version = '2.0';
    n.queue = [];
    const t = b.createElement(e) as HTMLScriptElement;
    t.async = true;
    t.src = v;
    const s = b.getElementsByTagName(e)[0];
    s.parentNode?.insertBefore(t, s);
  })(window, document, 'script', 'https://connect.facebook.net/en_US/fbevents.js');
  /* eslint-enable */
}

function initMetaPixelId(pixelId: string): void {
  if (initedMetaPixelIds.has(pixelId)) return;
  initedMetaPixelIds.add(pixelId);
  ensureMetaScriptLoaded();
  window.fbq?.('init', pixelId);
}

/**
 * TikTok's standard public base-code stub: `ttq`'s methods (load/page/track/...) are defined
 * synchronously and simply queue their calls (`push`) until the real events.js finishes loading
 * and upgrades them in place. This means calling `window.ttq.track(...)` is always safe — before,
 * during, or after the async script load — instead of throwing "not a function" on an empty stub.
 */
function ensureTikTokBaseCode(): void {
  if (window.ttq) return;

  const ttq = (window.ttq = [] as unknown as NonNullable<Window['ttq']>);
  ttq.methods = [
    'page', 'track', 'identify', 'instances', 'debug', 'on', 'off', 'once',
    'ready', 'alias', 'group', 'enableCookie', 'disableCookie',
    'holdConsent', 'revokeConsent', 'grantConsent',
  ];
  ttq.setAndDefer = (target: Record<string, unknown>, method: string) => {
    target[method] = (...args: unknown[]) => (target as unknown[] & Record<string, unknown>).push?.([method, ...args]);
  };
  for (const method of ttq.methods as string[]) {
    (ttq.setAndDefer as (t: Record<string, unknown>, m: string) => void)(ttq, method);
  }
  ttq.load = (pixelId: string) => {
    const url = 'https://analytics.tiktok.com/i18n/pixel/events.js';
    const script = document.createElement('script');
    script.type = 'text/javascript';
    script.async = true;
    script.src = `${url}?sdkid=${pixelId}&lib=ttq`;
    const first = document.getElementsByTagName('script')[0];
    first.parentNode?.insertBefore(script, first);
  };
}

function initTikTokPixelId(pixelId: string): void {
  if (initedTikTokPixelIds.has(pixelId)) return;
  initedTikTokPixelIds.add(pixelId);
  ensureTikTokBaseCode();
  window.ttq?.load(pixelId);
  window.ttq?.page();
}

export function trackEvent(event: TrackedEvent, params?: Record<string, unknown>): void {
  if (typeof window === 'undefined') return;

  window.fbq?.('track', META_EVENT_NAMES[event], params);
  window.ttq?.track(TIKTOK_EVENT_NAMES[event], params);
}
