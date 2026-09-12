import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';

export interface HeroSlide {
  imageUrl: string;
  linkUrl: string | null;
}

const ROTATION_INTERVAL_MS = 7000;

/**
 * Auto-rotating hero background — crossfades between slides every 7s. A single slide renders
 * statically with no timer at all (nothing to rotate to). Respects prefers-reduced-motion by
 * disabling auto-rotation entirely, showing only the first slide, rather than trying to soften the
 * animation — CLAUDE.md doesn't call for manual carousel controls, so there'd be no other way for a
 * reduced-motion user to see the remaining slides anyway.
 */
export function HeroCarousel({ slides }: { slides: HeroSlide[] }) {
  const [index, setIndex] = useState(0);
  const prefersReducedMotion = useRef(
    typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches,
  );

  useEffect(() => {
    if (slides.length <= 1 || prefersReducedMotion.current) return;

    const id = setInterval(() => {
      setIndex((i) => (i + 1) % slides.length);
    }, ROTATION_INTERVAL_MS);

    return () => clearInterval(id);
  }, [slides.length]);

  return (
    <div className="absolute inset-0">
      {slides.map((slide, i) => {
        const image = (
          <img
            src={slide.imageUrl}
            alt=""
            className={`absolute inset-0 h-full w-full object-cover transition-opacity duration-1000 ${
              i === index ? 'opacity-80' : 'opacity-0'
            }`}
          />
        );

        // Decorative and click-through only — the overlay's own CTA buttons already provide the
        // real, properly-labeled navigation, so these background links are kept out of the tab
        // order rather than adding an unlabeled stop for keyboard/screen-reader users.
        return slide.linkUrl ? (
          <Link key={slide.imageUrl} to={slide.linkUrl} aria-hidden tabIndex={-1}>
            {image}
          </Link>
        ) : (
          <div key={slide.imageUrl} aria-hidden>
            {image}
          </div>
        );
      })}
    </div>
  );
}
