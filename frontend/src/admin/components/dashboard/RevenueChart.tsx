import { useMemo, useState } from 'react';

import type { DashboardRevenuePointDto } from '../../../lib/api/types';
import { formatPrice } from '../../../lib/format/price';

const WIDTH = 640;
const HEIGHT = 220;
const PAD_LEFT = 8;
const PAD_RIGHT = 8;
const PAD_TOP = 16;
const PAD_BOTTOM = 28;

function formatBucketLabel(date: string, granularity: 'hourly' | 'daily' | 'monthly'): string {
  const d = new Date(date);
  if (granularity === 'hourly') return d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  if (granularity === 'monthly') return d.toLocaleDateString('fr-FR', { month: 'short', year: '2-digit' });
  return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' });
}

export function RevenueChart({
  points,
  granularity,
}: {
  points: DashboardRevenuePointDto[];
  granularity: 'hourly' | 'daily' | 'monthly';
}) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  const { path, areaPath, coords, maxRevenue } = useMemo(() => {
    const max = Math.max(1, ...points.map((p) => p.revenue));
    const innerWidth = WIDTH - PAD_LEFT - PAD_RIGHT;
    const innerHeight = HEIGHT - PAD_TOP - PAD_BOTTOM;
    const step = points.length > 1 ? innerWidth / (points.length - 1) : 0;

    const pts = points.map((p, i) => {
      const x = PAD_LEFT + step * i;
      const y = PAD_TOP + innerHeight - (p.revenue / max) * innerHeight;
      return { x, y, value: p.revenue, date: p.date };
    });

    const linePath = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    const area =
      pts.length > 0
        ? `${linePath} L${pts[pts.length - 1].x.toFixed(1)},${(PAD_TOP + innerHeight).toFixed(1)} L${pts[0].x.toFixed(1)},${(PAD_TOP + innerHeight).toFixed(1)} Z`
        : '';

    return { path: linePath, areaPath: area, coords: pts, maxRevenue: max };
  }, [points]);

  if (points.length === 0 || points.every((p) => p.revenue === 0)) {
    return (
      <div className="flex h-[220px] items-center justify-center text-sm text-luna-charcoal/50">
        Aucune donnée de revenu sur cette période.
      </div>
    );
  }

  const hovered = hoverIndex !== null ? coords[hoverIndex] : null;

  return (
    <div className="relative">
      <svg
        viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
        className="w-full"
        role="img"
        aria-label="Évolution du chiffre d'affaires"
        onMouseLeave={() => setHoverIndex(null)}
      >
        <defs>
          <linearGradient id="revenueFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--color-luna-accent)" stopOpacity="0.28" />
            <stop offset="100%" stopColor="var(--color-luna-accent)" stopOpacity="0" />
          </linearGradient>
        </defs>

        {/* recessive baseline */}
        <line
          x1={PAD_LEFT}
          y1={HEIGHT - PAD_BOTTOM}
          x2={WIDTH - PAD_RIGHT}
          y2={HEIGHT - PAD_BOTTOM}
          stroke="currentColor"
          strokeOpacity="0.12"
        />

        <path d={areaPath} fill="url(#revenueFill)" />
        <path d={path} fill="none" stroke="var(--color-luna-accent)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />

        {coords.map((c, i) => (
          <g key={c.date}>
            <rect
              x={i === 0 ? c.x : (coords[i - 1].x + c.x) / 2}
              y={PAD_TOP}
              width={
                (i === coords.length - 1 ? c.x : (coords[i + 1]?.x ?? c.x + (c.x - (coords[i - 1]?.x ?? c.x)) / 2)) -
                (i === 0 ? c.x : (coords[i - 1].x + c.x) / 2)
              }
              height={HEIGHT - PAD_TOP - PAD_BOTTOM}
              fill="transparent"
              onMouseEnter={() => setHoverIndex(i)}
            />
            {hoverIndex === i && (
              <>
                <line x1={c.x} y1={PAD_TOP} x2={c.x} y2={HEIGHT - PAD_BOTTOM} stroke="currentColor" strokeOpacity="0.2" />
                <circle cx={c.x} cy={c.y} r="4" fill="var(--color-luna-accent)" stroke="white" strokeWidth="1.5" />
              </>
            )}
          </g>
        ))}

        {coords.length > 0 && (
          <>
            <text x={coords[0].x} y={HEIGHT - 8} fontSize="10" fill="currentColor" fillOpacity="0.5" textAnchor="start">
              {formatBucketLabel(coords[0].date, granularity)}
            </text>
            <text
              x={coords[coords.length - 1].x}
              y={HEIGHT - 8}
              fontSize="10"
              fill="currentColor"
              fillOpacity="0.5"
              textAnchor="end"
            >
              {formatBucketLabel(coords[coords.length - 1].date, granularity)}
            </text>
          </>
        )}
      </svg>

      {hovered && (
        <div
          className="pointer-events-none absolute top-2 -translate-x-1/2 rounded-md border border-black/10 bg-white px-2.5 py-1.5 text-xs shadow-md"
          style={{ left: `${(hovered.x / WIDTH) * 100}%` }}
        >
          <p className="font-medium">{formatPrice(hovered.value)}</p>
          <p className="text-luna-charcoal/60">{formatBucketLabel(hovered.date, granularity)}</p>
        </div>
      )}

      <p className="mt-1 text-right text-[10px] uppercase tracking-wide text-luna-charcoal/40">
        Max {formatPrice(maxRevenue)}
      </p>
    </div>
  );
}
