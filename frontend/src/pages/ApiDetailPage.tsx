import { Link, useLocation, useParams } from 'react-router-dom';
import { MethodBadge, StatusBadge, TechBadge } from '../components/Badges';
import { CodeBlock } from '../components/CodeBlock';
import { extractHeadings, Markdown, slugify } from '../components/Markdown';
import { useCatalog } from '../components/Layout';
import { useAsync } from '../hooks/useAsync';
import { usePortalApi } from '../hooks/usePortalApi';
import type { Endpoint } from '../api/types';

function curlFor(baseUrl: string, ep: Endpoint): string {
  const lines = [`curl --request ${ep.method} \\`, `  --url '${baseUrl}${ep.path}' \\`, `  --header 'Authorization: Bearer <token>'`];
  if (ep.requestExample) {
    lines[lines.length - 1] += ' \\';
    lines.push(`  --header 'Content-Type: application/json' \\`, `  --data '${ep.requestExample.replace(/'/g, "'\\''")}'`);
  }
  return lines.join('\n');
}

export function ApiDetailPage() {
  const { slug = '' } = useParams();
  const { search } = useLocation();
  const portalApi = usePortalApi();
  const { technologies } = useCatalog();
  const { data: api, loading, error } = useAsync(() => portalApi.getApi(slug), [portalApi, slug]);

  if (loading) return <div className="page muted">Cargando…</div>;
  if (error || !api) {
    return (
      <div className="page">
        <div className="alert error">{error?.message ?? 'API no encontrada'}</div>
        <Link to={{ pathname: '/', search }}>← Volver al catálogo</Link>
      </div>
    );
  }

  const tech = technologies.find((t) => t.slug === api.technology);
  const headings = extractHeadings(api.descriptionMd);

  return (
    <div className="detail">
      <article className="detail-main">
        <nav className="breadcrumb">
          <Link to={{ pathname: '/', search }}>Catálogo</Link> / <span>{api.category}</span> / <span>{api.name}</span>
        </nav>
        <header className="detail-header">
          <h1>
            {api.name} <span className="version">{api.version}</span>
          </h1>
          <p className="lead">{api.summary}</p>
          <div className="meta-row">
            <TechBadge name={api.technologyName} color={tech?.color} />
            <StatusBadge status={api.status} />
            <span className="muted small">Owner: {api.ownerTeam}</span>
            <span className="muted small">Actualizado: {new Date(api.updatedAt).toLocaleDateString('es-CO')}</span>
          </div>
          <div className="base-url">
            <span className="muted small">Base URL</span>
            <code>{api.baseUrl}</code>
          </div>
        </header>

        <Markdown source={api.descriptionMd} />

        {api.endpoints.length > 0 && <h2 id="endpoints">Endpoints</h2>}
        {api.endpoints.map((ep) => (
          <section key={ep.id} id={`ep-${slugify(ep.title)}`} className="endpoint">
            <div className="endpoint-doc">
              <h3>{ep.title}</h3>
              <div className="endpoint-line">
                <MethodBadge method={ep.method} />
                <code>{ep.path}</code>
              </div>
              <Markdown source={ep.descriptionMd} />
            </div>
            <div className="endpoint-examples">
              <CodeBlock
                title="Request"
                tabs={[
                  { label: 'cURL', code: curlFor(api.baseUrl, ep) },
                  ...(ep.requestExample ? [{ label: 'Body', code: ep.requestExample }] : []),
                ]}
              />
              {ep.responseExample && <CodeBlock title="Response" tabs={[{ label: '200', code: ep.responseExample }]} />}
            </div>
          </section>
        ))}
      </article>

      <aside className="toc" aria-label="En esta página">
        <div className="sidebar-title">En esta página</div>
        <ul>
          {headings.map((h) => (
            <li key={h.id} className={`lvl-${h.level}`}>
              <a href={`#${h.id}`}>{h.text}</a>
            </li>
          ))}
          {api.endpoints.length > 0 && (
            <li className="lvl-2">
              <a href="#endpoints">Endpoints</a>
            </li>
          )}
          {api.endpoints.map((ep) => (
            <li key={ep.id} className="lvl-3">
              <a href={`#ep-${slugify(ep.title)}`}>{ep.title}</a>
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
