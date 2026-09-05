import { Check, Circle } from 'lucide-react';

import { ORDER_STATUS_LABELS } from '../format/orderLabels';
import type { OrderStatus } from '../api/types';

// The CLAUDE.md section 12 "happy path" — order-status history/reasons stay admin-only (privacy,
// see PROJECT_CONTEXT.md Important Decisions), so this timeline is derived purely from the
// order's *current* status, without per-step timestamps.
const ORDER_FLOW: OrderStatus[] = [
  'PendingConfirmation',
  'Confirmed',
  'Preparing',
  'ReadyToShip',
  'Shipped',
  'OutForDelivery',
  'Delivered',
];

export function OrderTimeline({ status }: { status: OrderStatus }) {
  const currentIndex = ORDER_FLOW.indexOf(status);

  if (currentIndex === -1) {
    return <p className="text-sm text-luna-black">{ORDER_STATUS_LABELS[status]}</p>;
  }

  return (
    <ol className="space-y-4">
      {ORDER_FLOW.map((step, index) => {
        const done = index <= currentIndex;
        return (
          <li key={step} className="flex gap-3">
            <span
              className={`mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full border ${
                done ? 'border-luna-accent bg-luna-accent text-white' : 'border-black/15 text-luna-charcoal/50'
              }`}
            >
              {done ? <Check className="h-3.5 w-3.5" /> : <Circle className="h-2 w-2" />}
            </span>
            <p className={`text-sm ${done ? 'text-luna-black' : 'text-luna-charcoal/50'}`}>{ORDER_STATUS_LABELS[step]}</p>
          </li>
        );
      })}
    </ol>
  );
}
