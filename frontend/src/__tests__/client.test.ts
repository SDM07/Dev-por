import { buildQuery, matchesFilters } from '../api/client';
import type { ApiSummary } from '../api/types';

const api: ApiSummary = {
  slug: 'accounts',
  name: 'Cuentas',
  summary: 'Crea o conecta cuentas',
  technology: 'dotnet',
  technologyName: '.NET',
  category: 'Core',
  version: 'v1',
  ownerTeam: 'Equipo Core',
  status: 'STABLE',
  tags: ['saldos'],
  updatedAt: '2026-01-01T00:00:00Z',
};

describe('buildQuery', () => {
  it('serializa tecnologías múltiples separadas por coma', () => {
    expect(buildQuery({ technologies: ['dotnet', 'mulesoft'], q: ' pagos ' })).toBe('?technology=dotnet%2Cmulesoft&q=pagos');
  });
  it('devuelve cadena vacía sin filtros', () => {
    expect(buildQuery({ technologies: [], q: '' })).toBe('');
  });
});

describe('matchesFilters', () => {
  it('filtra por tecnología', () => {
    expect(matchesFilters(api, { technologies: ['dotnet'], q: '' })).toBe(true);
    expect(matchesFilters(api, { technologies: ['mulesoft'], q: '' })).toBe(false);
  });
  it('busca en nombre, resumen, equipo y etiquetas sin distinguir mayúsculas', () => {
    expect(matchesFilters(api, { technologies: [], q: 'SALDOS' })).toBe(true);
    expect(matchesFilters(api, { technologies: [], q: 'core' })).toBe(true);
    expect(matchesFilters(api, { technologies: [], q: 'kyc' })).toBe(false);
  });
  it('filtra por categoría', () => {
    expect(matchesFilters(api, { technologies: [], q: '', category: 'Pagos' })).toBe(false);
  });
});
