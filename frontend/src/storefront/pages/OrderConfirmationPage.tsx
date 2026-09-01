import { useParams } from 'react-router-dom';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function OrderConfirmationPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>();
  return <PagePlaceholder title={`Commande ${orderNumber} confirmée`} />;
}
