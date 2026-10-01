import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { ApiFilters } from '../api/types';

/** Los filtros viven en la URL (?tech=dotnet,mulesoft&q=pagos) para poder compartir enlaces. */
export function useApiFilters() {
  const [params, setParams] = useSearchParams();

  const filters = useMemo<ApiFilters>(
    () => ({
      technologies: (params.get('tech') ?? '').split(',').filter(Boolean),
      q: params.get('q') ?? '',
      category: params.get('category') ?? undefined,
    }),
    [params],
  );

  const update = useCallback(
    (next: Partial<ApiFilters>) => {
      const merged = { ...filters, ...next };
      const p = new URLSearchParams();
      if (merged.technologies.length) p.set('tech', merged.technologies.join(','));
      if (merged.q) p.set('q', merged.q);
      if (merged.category) p.set('category', merged.category);
      setParams(p, { replace: true });
    },
    [filters, setParams],
  );

  const toggleTechnology = useCallback(
    (slug: string) =>
      update({
        technologies: filters.technologies.includes(slug)
          ? filters.technologies.filter((t) => t !== slug)
          : [...filters.technologies, slug],
      }),
    [filters.technologies, update],
  );

  const clear = useCallback(() => setParams(new URLSearchParams(), { replace: true }), [setParams]);

  return { filters, update, toggleTechnology, clear };
}
