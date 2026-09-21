import type { DashboardStatusCountDto } from '../../../lib/api/types';
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from '../../../lib/format/orderLabels';

export function StatusBarChart({ data }: { data: DashboardStatusCountDto[] }) {
  if (data.length === 0) {
    return <p className="py-6 text-center text-sm text-luna-charcoal/50">Aucune commande sur cette période.</p>;
  }

  const max = Math.max(...data.map((d) => d.count));

  return (
    <div className="flex flex-col gap-2.5">
      {data.map((d) => (
        <div key={d.status} className="flex items-center gap-3">
          <span className="w-32 shrink-0 truncate text-xs text-luna-charcoal/70">{ORDER_STATUS_LABELS[d.status]}</span>
          <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-luna-cream">
            <div
              className="h-full rounded-full transition-[width]"
              style={{ width: `${Math.max(3, (d.count / max) * 100)}%`, backgroundColor: ORDER_STATUS_COLORS[d.status] }}
            />
          </div>
          <span className="w-8 shrink-0 text-right text-xs font-medium tabular-nums">{d.count}</span>
        </div>
      ))}
    </div>
  );
}
