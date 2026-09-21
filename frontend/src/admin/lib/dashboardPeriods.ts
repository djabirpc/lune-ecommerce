export interface DashboardPeriodPreset {
  key: string;
  label: string;
  range: () => { from: Date; to: Date };
}

function startOfDay(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

function endOfDay(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 23, 59, 59, 999);
}

function startOfWeek(d: Date): Date {
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day; // week starts Monday
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  return startOfDay(monday);
}

function addDays(d: Date, n: number): Date {
  const copy = new Date(d);
  copy.setDate(copy.getDate() + n);
  return copy;
}

export const DASHBOARD_PERIOD_PRESETS: DashboardPeriodPreset[] = [
  {
    key: 'today',
    label: "Aujourd'hui",
    range: () => {
      const now = new Date();
      return { from: startOfDay(now), to: endOfDay(now) };
    },
  },
  {
    key: 'yesterday',
    label: 'Hier',
    range: () => {
      const y = addDays(new Date(), -1);
      return { from: startOfDay(y), to: endOfDay(y) };
    },
  },
  {
    key: 'thisWeek',
    label: 'Cette semaine',
    range: () => {
      const now = new Date();
      return { from: startOfWeek(now), to: endOfDay(now) };
    },
  },
  {
    key: 'lastWeek',
    label: 'Semaine dernière',
    range: () => {
      const startThisWeek = startOfWeek(new Date());
      const startLastWeek = addDays(startThisWeek, -7);
      return { from: startLastWeek, to: endOfDay(addDays(startLastWeek, 6)) };
    },
  },
  {
    key: 'thisMonth',
    label: 'Ce mois',
    range: () => {
      const now = new Date();
      return { from: new Date(now.getFullYear(), now.getMonth(), 1), to: endOfDay(now) };
    },
  },
  {
    key: 'lastMonth',
    label: 'Mois dernier',
    range: () => {
      const now = new Date();
      const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const to = new Date(now.getFullYear(), now.getMonth(), 0);
      return { from, to: endOfDay(to) };
    },
  },
  {
    key: 'thisQuarter',
    label: 'Ce trimestre',
    range: () => {
      const now = new Date();
      const qStartMonth = Math.floor(now.getMonth() / 3) * 3;
      return { from: new Date(now.getFullYear(), qStartMonth, 1), to: endOfDay(now) };
    },
  },
  {
    key: 'lastQuarter',
    label: 'Trimestre dernier',
    range: () => {
      const now = new Date();
      const qStartMonth = Math.floor(now.getMonth() / 3) * 3;
      const from = new Date(now.getFullYear(), qStartMonth - 3, 1);
      const to = new Date(now.getFullYear(), qStartMonth, 0);
      return { from, to: endOfDay(to) };
    },
  },
  {
    key: 'thisYear',
    label: 'Cette année',
    range: () => {
      const now = new Date();
      return { from: new Date(now.getFullYear(), 0, 1), to: endOfDay(now) };
    },
  },
  {
    key: 'lastYear',
    label: 'Année dernière',
    range: () => {
      const now = new Date();
      return { from: new Date(now.getFullYear() - 1, 0, 1), to: endOfDay(new Date(now.getFullYear() - 1, 11, 31)) };
    },
  },
  {
    key: 'custom',
    label: 'Personnalisée',
    range: () => {
      const now = new Date();
      return { from: startOfDay(now), to: endOfDay(now) };
    },
  },
];

export function toApiDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function formatPeriodLabel(from: string, to: string): string {
  const fmt = (s: string) => new Date(s).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  return `${fmt(from)} – ${fmt(to)}`;
}
