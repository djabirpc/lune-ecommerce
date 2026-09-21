import { DASHBOARD_PERIOD_PRESETS, toApiDate } from '../../lib/dashboardPeriods';

export function PeriodFilter({
  presetKey,
  customFrom,
  customTo,
  onPresetChange,
  onCustomChange,
}: {
  presetKey: string;
  customFrom: string;
  customTo: string;
  onPresetChange: (key: string) => void;
  onCustomChange: (from: string, to: string) => void;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <select
        value={presetKey}
        onChange={(e) => onPresetChange(e.target.value)}
        className="rounded border border-black/20 bg-white px-3 py-2 text-sm"
      >
        {DASHBOARD_PERIOD_PRESETS.map((p) => (
          <option key={p.key} value={p.key}>
            {p.label}
          </option>
        ))}
      </select>

      {presetKey === 'custom' && (
        <div className="flex items-center gap-2">
          <input
            type="date"
            value={customFrom}
            max={customTo || toApiDate(new Date())}
            onChange={(e) => onCustomChange(e.target.value, customTo)}
            className="rounded border border-black/20 bg-white px-2 py-2 text-sm"
          />
          <span className="text-luna-charcoal/50">→</span>
          <input
            type="date"
            value={customTo}
            min={customFrom}
            max={toApiDate(new Date())}
            onChange={(e) => onCustomChange(customFrom, e.target.value)}
            className="rounded border border-black/20 bg-white px-2 py-2 text-sm"
          />
        </div>
      )}
    </div>
  );
}
