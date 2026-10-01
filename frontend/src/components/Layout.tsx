import { useEffect, useMemo, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useOutletContext } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { useApiFilters } from '../hooks/useApiFilters';
import { useAsync } from '../hooks/useAsync';
import { usePortalApi } from '../hooks/usePortalApi';
import type { ApiFilters, ApiSummary, Technology } from '../api/types';

export interface CatalogContext {
  technologies: Technology[];
  apis: ApiSummary[];
  loading: boolean;
  error?: Error;
  filters: ApiFilters;
  update: (next: Partial<ApiFilters>) => void;
  toggleTechnology: (slug: string) => void;
  clear: () => void;
  reload: () => void;
}

export function useCatalog() {
  return useOutletContext<CatalogContext>();
}

function groupByCategory(apis: ApiSummary[]): [string, ApiSummary[]][] {
  const groups = new Map<string, ApiSummary[]>();
  for (const api of apis) {
    groups.set(api.category, [...(groups.get(api.category) ?? []), api]);
  }
  return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
}

export function Layout() {
  const api = usePortalApi();
  const { user, isAdmin, logout } = useAuth();
  const { filters, update, toggleTechnology, clear } = useApiFilters();
  const location = useLocation();
  const [reloadKey, setReloadKey] = useState(0);
  const [search, setSearch] = useState(filters.q);
  const [menuOpen, setMenuOpen] = useState(false);

  // Debounce de la búsqueda para no consultar Snowflake en cada tecla.
  useEffect(() => {
    const t = setTimeout(() => search !== filters.q && update({ q: search }), 300);
    return () => clearTimeout(t);
  }, [search, filters.q, update]);

  useEffect(() => setMenuOpen(false), [location.pathname]);

  const techs = useAsync(() => api.listTechnologies(), [api, reloadKey]);
  const apis = useAsync(() => api.listApis(filters), [api, filters, reloadKey]);
  const groups = useMemo(() => groupByCategory(apis.data ?? []), [apis.data]);

  const ctx: CatalogContext = {
    technologies: techs.data ?? [],
    apis: apis.data ?? [],
    loading: apis.loading || techs.loading,
    error: apis.error ?? techs.error,
    filters,
    update,
    toggleTechnology,
    clear,
    reload: () => setReloadKey((k) => k + 1),
  };

  const qs = location.search;

  return (
    <div className="app">
      <header className="topbar">
        <button className="menu-toggle" aria-label="Abrir menú" onClick={() => setMenuOpen((o) => !o)}>
          ☰
        </button>
        <Link to={{ pathname: '/', search: qs }} className="brand">
          <img src="/favicon.svg" alt="" width={28} height={28} />
          <span>Developer Portal</span>
        </Link>
        <div className="search">
          <input
            type="search"
            placeholder="Buscar APIs, equipos, etiquetas…"
            aria-label="Buscar"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <nav className="topnav">
          <NavLink to={{ pathname: '/', search: qs }} end>
            Catálogo
          </NavLink>
          {isAdmin && <NavLink to="/admin">Administrar</NavLink>}
        </nav>
        <div className="user">
          <span className="avatar" title={user?.username}>
            {user?.name.slice(0, 1).toUpperCase()}
          </span>
          <span className="user-name">{user?.name}</span>
          <button className="link-btn" onClick={logout}>
            Salir
          </button>
        </div>
      </header>

      <div className="body">
        <aside className={`sidebar ${menuOpen ? 'open' : ''}`} aria-label="Navegación de APIs">
          <div className="sidebar-section">
            <div className="sidebar-title">Tecnología</div>
            <ul className="tech-list">
              {ctx.technologies.map((t) => (
                <li key={t.slug}>
                  <label>
                    <input
                      type="checkbox"
                      checked={filters.technologies.includes(t.slug)}
                      onChange={() => toggleTechnology(t.slug)}
                    />
                    <span className="dot" style={{ background: t.color }} />
                    {t.name}
                    <span className="count">{t.apiCount}</span>
                  </label>
                </li>
              ))}
            </ul>
          </div>
          <nav className="sidebar-section">
            {groups.map(([category, items]) => (
              <div key={category} className="nav-group">
                <div className="sidebar-title">{category}</div>
                <ul>
                  {items.map((a) => (
                    <li key={a.slug}>
                      <NavLink to={{ pathname: `/apis/${a.slug}`, search: qs }}>
                        <span className="nav-name">{a.name}</span>
                        <span className="nav-version">{a.version}</span>
                      </NavLink>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
            {!apis.loading && groups.length === 0 && <p className="muted small">Sin resultados.</p>}
          </nav>
          {isAdmin && (
            <div className="sidebar-section sidebar-admin">
              <NavLink to="/admin">⚙ Administrar catálogo</NavLink>
            </div>
          )}
        </aside>
        {menuOpen && <div className="backdrop" onClick={() => setMenuOpen(false)} />}
        <main className="content">
          <Outlet context={ctx} />
        </main>
      </div>
    </div>
  );
}
