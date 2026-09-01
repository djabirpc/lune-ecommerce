import { useParams } from 'react-router-dom';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  return <PagePlaceholder title={`Commande ${id}`} />;
}
