import { useEffect, useState } from 'react';

function parts(ms: number) {
  const s = Math.max(0, Math.floor(ms / 1000));
  return {
    h: String(Math.floor(s / 3600)).padStart(2, '0'),
    m: String(Math.floor((s % 3600) / 60)).padStart(2, '0'),
    s: String(s % 60).padStart(2, '0'),
  };
}

export function Countdown({ endsAt, dark }: { endsAt: string; dark?: boolean }) {
  const [left, setLeft] = useState(() => new Date(endsAt).getTime() - Date.now());

  useEffect(() => {
    const id = setInterval(() => setLeft(new Date(endsAt).getTime() - Date.now()), 1000);
    return () => clearInterval(id);
  }, [endsAt]);

  const { h, m, s } = parts(left);
  const box = dark ? 'bg-white/15 text-white' : 'bg-luna-black text-white';

  return (
    <div className="flex items-center gap-1.5 tabular-nums">
      {[h, m, s].map((v, i) => (
        <span key={i} className={`rounded-sm px-2 py-1 text-sm font-medium ${box}`}>
          {v}
        </span>
      ))}
    </div>
  );
}
