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

export function initPixels(): void {
  if (initialized || typeof window === 'undefined') return;
  initialized = true;

  const metaPixelId = import.meta.env.VITE_META_PIXEL_ID as string | undefined;
  if (metaPixelId) {
    loadMetaPixel(metaPixelId);
  }

  const tiktokPixelId = import.meta.env.VITE_TIKTOK_PIXEL_ID as string | undefined;
  if (tiktokPixelId) {
    loadTikTokPixel(tiktokPixelId);
  }
}

function loadMetaPixel(pixelId: string): void {
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

  window.fbq?.('init', pixelId);
  window.fbq?.('track', 'PageView');
}

function loadTikTokPixel(pixelId: string): void {
  window.ttq = window.ttq ?? ({} as Window['ttq']);
  const script = document.createElement('script');
  script.async = true;
  script.src = 'https://analytics.tiktok.com/i18n/pixel/events.js';
  script.onload = () => {
    window.ttq?.load(pixelId);
    window.ttq?.page();
  };
  document.head.appendChild(script);
}

export function trackEvent(event: TrackedEvent, params?: Record<string, unknown>): void {
  if (typeof window === 'undefined') return;

  window.fbq?.('track', META_EVENT_NAMES[event], params);
  window.ttq?.track(TIKTOK_EVENT_NAMES[event], params);
}
