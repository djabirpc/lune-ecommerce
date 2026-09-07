import { useState } from 'react';

interface ReasonModalProps {
  title: string;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

export function ReasonModal({ title, onConfirm, onCancel }: ReasonModalProps) {
  const [reason, setReason] = useState('');

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-5 shadow-xl">
        <h3 className="text-sm font-semibold">{title}</h3>
        <label className="mt-3 flex flex-col gap-1 text-sm">
          Raison (optionnel)
          <textarea
            autoFocus
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={3}
            className="rounded border border-black/20 px-3 py-2 text-sm"
          />
        </label>
        <div className="mt-4 flex justify-end gap-2">
          <button type="button" onClick={onCancel} className="rounded-full px-4 py-2 text-sm text-luna-charcoal/70 hover:bg-luna-cream">
            Annuler
          </button>
          <button
            type="button"
            onClick={() => onConfirm(reason.trim())}
            className="rounded-full bg-luna-black px-4 py-2 text-sm text-white"
          >
            Confirmer
          </button>
        </div>
      </div>
    </div>
  );
}
