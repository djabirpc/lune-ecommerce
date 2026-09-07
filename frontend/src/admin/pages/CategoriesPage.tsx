import { CategoryQuickManager } from '../components/CategoryQuickManager';

export function CategoriesPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold">Catégories</h1>

      <CategoryQuickManager />
    </div>
  );
}
