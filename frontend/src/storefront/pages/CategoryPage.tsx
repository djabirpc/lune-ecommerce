import { useParams } from 'react-router-dom';
import { PagePlaceholder } from '../../lib/components/PagePlaceholder';

export function CategoryPage() {
  const { slug } = useParams<{ slug: string }>();
  return <PagePlaceholder title={`Catégorie: ${slug}`} />;
}
