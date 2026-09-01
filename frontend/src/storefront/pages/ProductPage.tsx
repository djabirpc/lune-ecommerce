import { useParams } from 'react-router-dom';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function ProductPage() {
  const { slug } = useParams<{ slug: string }>();
  return <PagePlaceholder title={`Produit: ${slug}`} />;
}
