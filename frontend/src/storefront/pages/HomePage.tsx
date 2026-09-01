import { Link } from 'react-router-dom';

const TRUST_ITEMS = [
  'Livraison partout en Algérie',
  'Paiement à la livraison',
  'Échange facile',
  'Satisfait ou remboursé',
];

export function HomePage() {
  return (
    <div>
      <section className="flex flex-col items-center gap-6 px-4 py-20 text-center">
        <h1 className="max-w-md text-3xl font-semibold text-luna-black">La mode qui vous ressemble.</h1>
        <div className="flex flex-wrap justify-center gap-3">
          <Link to="/categories" className="rounded-full bg-luna-black px-6 py-3 text-sm text-white">
            Découvrir la collection
          </Link>
          <Link to="/promotions" className="rounded-full border border-luna-black px-6 py-3 text-sm">
            Voir les promotions
          </Link>
        </div>
      </section>

      <section className="grid grid-cols-2 gap-4 border-y border-black/5 px-4 py-8 text-xs sm:grid-cols-4">
        {TRUST_ITEMS.map((item) => (
          <div key={item} className="text-center text-luna-charcoal/80">
            {item}
          </div>
        ))}
      </section>
    </div>
  );
}
