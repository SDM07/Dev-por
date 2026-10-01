import { config } from '../config';
import { mockApis, mockTechnologies } from '../mocks/data';
import type { ApiDetail, ApiFilters, ApiSummary, ApiUpsert, Technology } from './types';

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

type TokenProvider = () => Promise<string>;

async function request<T>(getToken: TokenProvider, path: string, init: RequestInit = {}): Promise<T> {
  const token = await getToken();
  const res = await fetch(`${config.apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...init.headers,
    },
  });
  if (!res.ok) {
    let message = res.statusText;
    try {
      const body = await res.json();
      message = body.detail ?? body.title ?? body.message ?? message;
    } catch {
      /* cuerpo vacío */
    }
    throw new ApiError(res.status, message);
  }
  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}

export function buildQuery(filters: ApiFilters): string {
  const params = new URLSearchParams();
  if (filters.technologies.length) params.set('technology', filters.technologies.join(','));
  if (filters.q.trim()) params.set('q', filters.q.trim());
  if (filters.category) params.set('category', filters.category);
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

/** Mismo criterio de filtrado que aplica el backend sobre Snowflake. */
export function matchesFilters(api: ApiSummary, filters: ApiFilters): boolean {
  if (filters.technologies.length && !filters.technologies.includes(api.technology)) return false;
  if (filters.category && api.category !== filters.category) return false;
  const q = filters.q.trim().toLowerCase();
  if (!q) return true;
  return [api.name, api.summary, api.ownerTeam, ...api.tags].some((v) => v.toLowerCase().includes(q));
}

function toSummary({ descriptionMd: _d, baseUrl: _b, endpoints: _e, ...rest }: ApiDetail): ApiSummary {
  return rest;
}

const mockStore = { apis: structuredClone(mockApis) };
const delay = <T,>(v: T) => new Promise<T>((r) => setTimeout(() => r(v), 150));

export function createPortalApi(getToken: TokenProvider) {
  if (config.useMocks) {
    return {
      listTechnologies: () =>
        delay<Technology[]>(
          mockTechnologies.map((t) => ({
            ...t,
            apiCount: mockStore.apis.filter((a) => a.technology === t.slug).length,
          })),
        ),
      listApis: (filters: ApiFilters) =>
        delay(mockStore.apis.map(toSummary).filter((a) => matchesFilters(a, filters))),
      getApi: async (slug: string) => {
        const api = mockStore.apis.find((a) => a.slug === slug);
        if (!api) throw new ApiError(404, 'API no encontrada');
        return delay(structuredClone(api));
      },
      saveApi: async (api: ApiUpsert, isNew: boolean) => {
        const tech = mockTechnologies.find((t) => t.slug === api.technology);
        const full: ApiDetail = { ...api, technologyName: tech?.name ?? api.technology, updatedAt: new Date().toISOString() };
        const idx = mockStore.apis.findIndex((a) => a.slug === api.slug);
        if (isNew && idx >= 0) throw new ApiError(409, 'Ya existe una API con ese slug');
        if (idx >= 0) mockStore.apis[idx] = full;
        else mockStore.apis.push(full);
        return delay(full);
      },
      deleteApi: async (slug: string) => {
        mockStore.apis = mockStore.apis.filter((a) => a.slug !== slug);
        return delay(undefined);
      },
    };
  }

  return {
    listTechnologies: () => request<Technology[]>(getToken, '/technologies'),
    listApis: (filters: ApiFilters) => request<ApiSummary[]>(getToken, `/apis${buildQuery(filters)}`),
    getApi: (slug: string) => request<ApiDetail>(getToken, `/apis/${encodeURIComponent(slug)}`),
    saveApi: (api: ApiUpsert, isNew: boolean) =>
      request<ApiDetail>(getToken, isNew ? '/apis' : `/apis/${encodeURIComponent(api.slug)}`, {
        method: isNew ? 'POST' : 'PUT',
        body: JSON.stringify(api),
      }),
    deleteApi: (slug: string) =>
      request<void>(getToken, `/apis/${encodeURIComponent(slug)}`, { method: 'DELETE' }),
  };
}

export type PortalApi = ReturnType<typeof createPortalApi>;
