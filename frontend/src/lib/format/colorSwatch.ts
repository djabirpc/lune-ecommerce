/**
 * Presentational hex lookup for common French clothing color names, used to render a swatch dot
 * next to a real variant color name (e.g. "Beige"). The color NAME always comes from real product
 * data (ProductVariant.Color) — this only maps it to a plausible hex for the UI dot; unmapped names
 * fall back to a neutral grey rather than guessing.
 */
const COLOR_HEX: Record<string, string> = {
  beige: '#d8c7b2',
  noir: '#1b1b1b',
  blanc: '#f3efe7',
  'blanc cassé': '#f3efe7',
  'vert sauge': '#8c9c86',
  vert: '#6b8f71',
  'bleu denim': '#4a6484',
  bleu: '#4a6484',
  corail: '#e8836f',
  taupe: '#a99383',
  rouge: '#b8433a',
  rose: '#e3a9a1',
  gris: '#9a9a94',
  marron: '#6f4e37',
  camel: '#c19a6b',
  kaki: '#7a7a52',
  jaune: '#e0c05c',
  orange: '#d9793f',
  violet: '#7b6693',
  bordeaux: '#6f2a3a',
};

export function colorToHex(colorName: string): string {
  return COLOR_HEX[colorName.trim().toLowerCase()] ?? '#c9c2b8';
}
