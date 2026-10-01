import { Link, useLocation } from 'react-router-dom';
import { StatusBadge, TechBadge } from '../components/Badges';
import { TechnologyFilter } from '../components/TechnologyFilter';
import { useCatalog } from '../components/Layout';

export function CatalogPage() {
  const { technologies, apis, loading, error, filters, toggleTechnology, clear, update } = useCatalog();
  const { search } = useLocation();
  const colorOf = (slug: string) => technologies.find((t) => t.slug === slug)?.color;
  const categories = [...new Set(apis.map((a) => a.category))].sort();

  return (
    <div className="page">
      <section className="hero">
        <h1>Developer Portal</h1>
        <p>
          Explora las APIs e integraciones de la organización. Filtra por tecnología para encontrar rápidamente
          servicios en .NET, MuleSoft, Java y más.
        </p>
      </section>

      <TechnologyFilter
        technologies={technologies}
        selected={filters.technologies}
        onToggle={toggleTechnology}
        onClear={() => update({ technologies: [] })}
      />

      <div className="results-bar">
        <span className="muted">
          {loading ? 'Cargando…' : `${apis.length} API${apis.length === 1 ? '' : 's'}`}
          {filters.q && ` para “${filters.q}”`}
        </span>
        {categories.length > 1 && (
          <select
            aria-label="Filtrar por categoría"
            value={filters.category ?? ''}
            onChange={(e) => update({ category: e.target.value || undefined })}
          >
            <option value="">Todas las categorías</option>
            {categories.map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
        )}
        {(filters.technologies.length > 0 || filters.q || filters.category) && (
          <button className="link-btn" onClick={clear}>
            Limpiar filtros
          </button>
        )}
      </div>

      {error && <div className="alert error">No fue posible cargar el catálogo: {error.message}</div>}

      <div className="card-grid">
        {apis.map((a) => (
          <Link key={a.slug} to={{ pathname: `/apis/${a.slug}`, search }} className="card">
            <div className="card-top">
              <TechBadge name={a.technologyName} color={colorOf(a.technology)} />
              <StatusBadge status={a.status} />
            </div>
            <h3>
              {a.name} <span className="version">{a.version}</span>
            </h3>
            <p>{a.summary}</p>
            <div className="card-meta">
              <span>{a.category}</span>
              <span>·</span>
              <span>{a.ownerTeam}</span>
            </div>
            {a.tags.length > 0 && (
              <div className="tags">
                {a.tags.map((t) => (
                  <span key={t} className="tag">
                    #{t}
                  </span>
                ))}
              </div>
            )}
          </Link>
        ))}
      </div>

      {!loading && !error && apis.length === 0 && (
        <div className="empty">
          <p>No hay APIs que coincidan con los filtros seleccionados.</p>
          <button className="btn" onClick={clear}>
            Ver todas
          </button>
        </div>
      )}
    </div>
  );
}
