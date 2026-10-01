import { FormEvent, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { useCatalog } from '../components/Layout';
import { usePortalApi } from '../hooks/usePortalApi';
import type { ApiStatus, ApiUpsert, Endpoint, HttpMethod } from '../api/types';

const emptyApi = (): ApiUpsert => ({
  slug: '',
  name: '',
  summary: '',
  technology: '',
  category: '',
  version: 'v1',
  ownerTeam: '',
  status: 'STABLE',
  tags: [],
  baseUrl: '',
  descriptionMd: '',
  endpoints: [],
});

const methods: HttpMethod[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];
const statuses: ApiStatus[] = ['STABLE', 'BETA', 'DEPRECATED'];

export function AdminPage() {
  const { isAdmin } = useAuth();
  const portalApi = usePortalApi();
  const { apis, technologies, reload } = useCatalog();
  const [form, setForm] = useState<ApiUpsert>(emptyApi);
  const [isNew, setIsNew] = useState(true);
  const [message, setMessage] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null);
  const [saving, setSaving] = useState(false);

  if (!isAdmin) return <Navigate to="/" replace />;

  const set = <K extends keyof ApiUpsert>(key: K, value: ApiUpsert[K]) => setForm((f) => ({ ...f, [key]: value }));
  const setEndpoint = (i: number, patch: Partial<Endpoint>) =>
    set('endpoints', form.endpoints.map((e, idx) => (idx === i ? { ...e, ...patch } : e)));

  const edit = async (slug: string) => {
    setMessage(null);
    const detail = await portalApi.getApi(slug);
    const { technologyName: _t, updatedAt: _u, ...rest } = detail;
    setForm(rest);
    setIsNew(false);
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);
    try {
      await portalApi.saveApi(form, isNew);
      setMessage({ kind: 'ok', text: `API “${form.name}” guardada.` });
      setIsNew(false);
      reload();
    } catch (err) {
      setMessage({ kind: 'error', text: (err as Error).message });
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!confirm(`¿Eliminar la API “${form.name}”? Esta acción no se puede deshacer.`)) return;
    await portalApi.deleteApi(form.slug);
    setForm(emptyApi());
    setIsNew(true);
    setMessage({ kind: 'ok', text: 'API eliminada.' });
    reload();
  };

  return (
    <div className="page admin">
      <h1>Administrar catálogo</h1>
      <p className="muted">Solo visible para usuarios con el rol de Azure AD de administrador.</p>

      <div className="admin-toolbar">
        <select aria-label="Editar API existente" value={isNew ? '' : form.slug} onChange={(e) => e.target.value && edit(e.target.value)}>
          <option value="">— Editar API existente —</option>
          {apis.map((a) => (
            <option key={a.slug} value={a.slug}>
              {a.name} ({a.technologyName})
            </option>
          ))}
        </select>
        <button
          className="btn secondary"
          onClick={() => {
            setForm(emptyApi());
            setIsNew(true);
            setMessage(null);
          }}
        >
          Nueva API
        </button>
      </div>

      {message && <div className={`alert ${message.kind}`}>{message.text}</div>}

      <form onSubmit={submit} className="form">
        <div className="grid-2">
          <label>
            Nombre
            <input required value={form.name} onChange={(e) => set('name', e.target.value)} />
          </label>
          <label>
            Slug (URL)
            <input
              required
              pattern="[a-z0-9-]+"
              title="Minúsculas, números y guiones"
              disabled={!isNew}
              value={form.slug}
              onChange={(e) => set('slug', e.target.value)}
            />
          </label>
          <label>
            Tecnología
            <select required value={form.technology} onChange={(e) => set('technology', e.target.value)}>
              <option value="">Selecciona…</option>
              {technologies.map((t) => (
                <option key={t.slug} value={t.slug}>
                  {t.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Categoría
            <input required value={form.category} onChange={(e) => set('category', e.target.value)} />
          </label>
          <label>
            Versión
            <input required value={form.version} onChange={(e) => set('version', e.target.value)} />
          </label>
          <label>
            Estado
            <select value={form.status} onChange={(e) => set('status', e.target.value as ApiStatus)}>
              {statuses.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </label>
          <label>
            Equipo responsable
            <input required value={form.ownerTeam} onChange={(e) => set('ownerTeam', e.target.value)} />
          </label>
          <label>
            Base URL
            <input type="url" required value={form.baseUrl} onChange={(e) => set('baseUrl', e.target.value)} />
          </label>
        </div>
        <label>
          Resumen
          <input required maxLength={300} value={form.summary} onChange={(e) => set('summary', e.target.value)} />
        </label>
        <label>
          Etiquetas (separadas por coma)
          <input
            value={form.tags.join(', ')}
            onChange={(e) => set('tags', e.target.value.split(',').map((t) => t.trim()).filter(Boolean))}
          />
        </label>
        <label>
          Documentación (Markdown)
          <textarea rows={8} value={form.descriptionMd} onChange={(e) => set('descriptionMd', e.target.value)} />
        </label>

        <h2>Endpoints</h2>
        {form.endpoints.map((ep, i) => (
          <fieldset key={ep.id} className="endpoint-form">
            <div className="grid-3">
              <select value={ep.method} onChange={(e) => setEndpoint(i, { method: e.target.value as HttpMethod })}>
                {methods.map((m) => (
                  <option key={m}>{m}</option>
                ))}
              </select>
              <input placeholder="/ruta/{id}" required value={ep.path} onChange={(e) => setEndpoint(i, { path: e.target.value })} />
              <input placeholder="Título" required value={ep.title} onChange={(e) => setEndpoint(i, { title: e.target.value })} />
            </div>
            <textarea
              rows={2}
              placeholder="Descripción (Markdown)"
              value={ep.descriptionMd}
              onChange={(e) => setEndpoint(i, { descriptionMd: e.target.value })}
            />
            <div className="grid-2">
              <textarea
                rows={4}
                placeholder="Ejemplo de request (JSON)"
                value={ep.requestExample ?? ''}
                onChange={(e) => setEndpoint(i, { requestExample: e.target.value || undefined })}
              />
              <textarea
                rows={4}
                placeholder="Ejemplo de response (JSON)"
                value={ep.responseExample ?? ''}
                onChange={(e) => setEndpoint(i, { responseExample: e.target.value || undefined })}
              />
            </div>
            <button type="button" className="link-btn danger" onClick={() => set('endpoints', form.endpoints.filter((_, idx) => idx !== i))}>
              Quitar endpoint
            </button>
          </fieldset>
        ))}
        <button
          type="button"
          className="btn secondary"
          onClick={() =>
            set('endpoints', [
              ...form.endpoints,
              { id: crypto.randomUUID(), method: 'GET', path: '', title: '', descriptionMd: '' },
            ])
          }
        >
          + Agregar endpoint
        </button>

        <div className="form-actions">
          <button className="btn" type="submit" disabled={saving}>
            {saving ? 'Guardando…' : isNew ? 'Crear API' : 'Guardar cambios'}
          </button>
          {!isNew && (
            <button type="button" className="btn danger" onClick={remove}>
              Eliminar
            </button>
          )}
        </div>
      </form>
    </div>
  );
}
