import type { CanMatchFn, UrlSegment } from '@angular/router';

/** Public pages that use the marketing header instead of the signed-in shell. */
export const MARKETING_PATHS = [
  'product',
  'demo',
  'pricing',
  'about',
  'contact',
  'faq',
  'privacy',
  'copyright',
] as const;

export type MarketingPageId = (typeof MARKETING_PATHS)[number];

export const MARKETING_TITLES: Record<MarketingPageId, string> = {
  product: 'Product',
  demo: 'Demo',
  pricing: 'Pricing',
  about: 'About',
  contact: 'Contact',
  faq: 'FAQ',
  privacy: 'Privacy Policy',
  copyright: 'Copyright',
};

const PUBLIC_PATHS: ReadonlySet<string> = new Set([...MARKETING_PATHS, 'architecture']);

/**
 * Matches the home URL and the public pages only, so /dashboard and the rest
 * of the signed-in app fall through to the authenticated shell.
 */
export const marketingMatch: CanMatchFn = (_route, segments: UrlSegment[]) => {
  if (segments.length > 1) {
    return false;
  }
  return segments.length === 0 || PUBLIC_PATHS.has(segments[0].path);
};
