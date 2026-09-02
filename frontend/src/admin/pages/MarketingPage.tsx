import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { marketingApi } from '../../lib/api/marketing';
import { formatPrice } from '../../lib/format/price';

const DAY_OPTIONS = [7, 30, 90];

export function MarketingPage() {
  const [days, setDays] = useState(30);

  const { data: sources, isLoading } = useQuery({
    queryKey: ['marketing-sources', days],
    queryFn: () => marketingApi.getSources(days),
  });

  const totalOrders = sources?.reduce((sum, s) => sum + s.orderCount, 0) ?? 0;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">Marketing</h1>
        <select
          value={days}
          onChange={(e) => setDays(Number(e.target.value))}
          className="rounded border border-black/20 px-3 py-2 text-sm"
        >
          {DAY_OPTIONS.map((d) => (
            <option key={d} value={d}>
              {d} derniers jours
            </option>
          ))}
        </select>
      </div>

      {isLoading && <p className="text-sm text-luna-charcoal/60">Chargement...</p>}

      {sources && (
        <div className="overflow-x-auto rounded-lg border border-black/10 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/10 text-xs uppercase text-luna-charcoal/60">
              <tr>
                <th className="px-4 py-2">Source</th>
                <th className="px-4 py-2">Commandes</th>
                <th className="px-4 py-2">Part</th>
                <th className="px-4 py-2">Chiffre d'affaires</th>
              </tr>
            </thead>
            <tbody>
              {sources.map((s) => (
                <tr key={s.source} className="border-b border-black/5 last:border-0">
                  <td className="px-4 py-2 font-medium">{s.source}</td>
                  <td className="px-4 py-2">{s.orderCount}</td>
                  <td className="px-4 py-2">{totalOrders > 0 ? `${Math.round((s.orderCount / totalOrders) * 100)}%` : '—'}</td>
                  <td className="px-4 py-2">{formatPrice(s.totalRevenue)}</td>
                </tr>
              ))}
              {sources.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-luna-charcoal/60">
                    Aucune commande sur cette période.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <p className="mt-4 text-xs text-luna-charcoal/50">
        "Direct" regroupe les commandes sans paramètres UTM (accès direct, favoris, ou source non trackée).
      </p>
    </div>
  );
}
