import { ALGERIA_MAP_VIEW_BOX, ALGERIA_WILAYA_SHAPES } from '../data/algeriaWilayaShapes';

interface AlgeriaMapProps {
  counts: Record<number, number>;
  selectedWilayaId: number | null;
  onSelectWilaya: (wilayaId: number | null) => void;
}

// Real (simplified) wilaya boundaries — see algeriaWilayaShapes.ts for provenance. Fill intensity per
// wilaya is driven by `color-mix()` against the existing --color-luna-accent design token (not a
// hardcoded hex scale), so the map automatically matches the rest of the admin's palette.
export function AlgeriaMap({ counts, selectedWilayaId, onSelectWilaya }: AlgeriaMapProps) {
  const maxCount = Math.max(1, ...Object.values(counts));

  return (
    <svg viewBox={ALGERIA_MAP_VIEW_BOX} role="img" aria-label="Carte des wilayas d'Algérie" className="h-auto w-full">
      {ALGERIA_WILAYA_SHAPES.map((w) => {
        const count = counts[w.code] ?? 0;
        // Square-root scale (not linear) so mid/low-count wilayas stay visually distinguishable
        // instead of the single highest wilaya making everything else look uniformly faint.
        const intensity = count > 0 ? Math.round(25 + Math.sqrt(count / maxCount) * 75) : 6;
        const isSelected = selectedWilayaId === w.code;

        return (
          <path
            key={w.code}
            d={w.path}
            onClick={() => onSelectWilaya(isSelected ? null : w.code)}
            style={{ fill: `color-mix(in oklch, var(--color-luna-accent) ${intensity}%, white)` }}
            className={`cursor-pointer transition-[stroke-width,opacity] hover:opacity-80 ${
              isSelected ? 'stroke-luna-black' : 'stroke-white'
            }`}
            strokeWidth={isSelected ? 2.2 : 0.6}
          >
            <title>
              {w.name} — {count} colis
            </title>
          </path>
        );
      })}
    </svg>
  );
}
