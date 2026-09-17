import { Check, Circle } from 'lucide-react';

import type { OrderStatus } from '../../lib/api/types';
import { ORDER_STATUS_LABELS } from '../../lib/format/orderLabels';

// Admin-only equivalent of lib/components/OrderTimeline.tsx (the storefront's customer-facing
// version): same "happy path" progress-dot visual, but reads the plain, untranslated
// ORDER_STATUS_LABELS map instead of useOrderStatusLabels()/i18next. Deliberately NOT reusing the
// storefront component here — it stores its active language in localStorage shared with the
// storefront's own language switcher, so on a shared browser the admin panel could silently render
// Arabic labels, which would violate CLAUDE.md section 45 ("admin stays French-only").
const ORDER_FLOW: OrderStatus[] = ['PendingConfirmation', 'Confirmed', 'ReadyToShip', 'Shipped', 'OutForDelivery', 'Delivered'];

export function OrderStatusTimeline({ status }: { status: OrderStatus }) {
  const currentIndex = ORDER_FLOW.indexOf(status);

  if (currentIndex === -1) {
    return <p className="text-sm text-luna-black">{ORDER_STATUS_LABELS[status]}</p>;
  }

  return (
    <ol className="space-y-3">
      {ORDER_FLOW.map((step, index) => {
        const done = index <= currentIndex;
        return (
          <li key={step} className="flex items-center gap-3">
            <span
              className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full border ${
                done ? 'border-luna-accent bg-luna-accent text-white' : 'border-black/15 text-luna-charcoal/40'
              }`}
            >
              {done ? <Check className="h-3 w-3" /> : <Circle className="h-1.5 w-1.5" />}
            </span>
            <span className={`text-sm ${done ? 'text-luna-black' : 'text-luna-charcoal/50'}`}>{ORDER_STATUS_LABELS[step]}</span>
          </li>
        );
      })}
    </ol>
  );
}
