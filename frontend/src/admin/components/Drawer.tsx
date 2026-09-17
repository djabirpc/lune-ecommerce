import type { ReactNode } from 'react';
import { X } from 'lucide-react';

interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

// Generic slide-over panel for admin quick-look views (Order Summary / Confirmation Summary) — mirrors
// the storefront mobile nav drawer's overlay + slide-in-panel CSS pattern (StorefrontLayout.tsx), the
// only existing "drawer" precedent in this codebase, so it looks/behaves consistently rather than
// introducing a second, different overlay style. Full-screen on mobile, a fixed side panel on larger
// screens, per CLAUDE.md's mobile-first rule.
export function Drawer({ open, onClose, title, children }: DrawerProps) {
  return (
    <div
      className={`fixed inset-0 z-50 transition-opacity ${open ? 'pointer-events-auto opacity-100' : 'pointer-events-none opacity-0'}`}
      aria-hidden={!open}
    >
      <div className="absolute inset-0 bg-luna-black/40" onClick={onClose} />
      <div
        className={`absolute inset-y-0 end-0 flex w-full flex-col bg-luna-cream shadow-xl transition-transform duration-300 sm:w-[440px] ${
          open ? 'translate-x-0' : 'translate-x-full rtl:-translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between border-b border-black/10 bg-white px-5 py-4">
          <h2 className="text-sm font-semibold uppercase text-luna-charcoal/70">{title}</h2>
          <button type="button" onClick={onClose} aria-label="Fermer" className="rounded p-1.5 text-luna-charcoal/60 hover:bg-luna-cream">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>
      </div>
    </div>
  );
}
