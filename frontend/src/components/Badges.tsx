import type { ApiStatus, HttpMethod } from '../api/types';

const statusLabel: Record<ApiStatus, string> = { STABLE: 'Estable', BETA: 'Beta', DEPRECATED: 'Deprecada' };

export function StatusBadge({ status }: { status: ApiStatus }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{statusLabel[status]}</span>;
}

export function MethodBadge({ method }: { method: HttpMethod }) {
  return <span className={`method method-${method.toLowerCase()}`}>{method}</span>;
}

export function TechBadge({ name, color }: { name: string; color?: string }) {
  return (
    <span className="tech-badge" style={{ ['--tech' as string]: color ?? 'var(--accent)' }}>
      <span className="dot" />
      {name}
    </span>
  );
}
