import type { Technology } from '../api/types';

interface Props {
  technologies: Technology[];
  selected: string[];
  onToggle: (slug: string) => void;
  onClear: () => void;
}

export function TechnologyFilter({ technologies, selected, onToggle, onClear }: Props) {
  return (
    <div className="tech-filter" role="group" aria-label="Filtrar por tecnología">
      <button
        type="button"
        className={`chip ${selected.length === 0 ? 'active' : ''}`}
        aria-pressed={selected.length === 0}
        onClick={onClear}
      >
        Todas
      </button>
      {technologies.map((t) => {
        const active = selected.includes(t.slug);
        return (
          <button
            key={t.slug}
            type="button"
            className={`chip ${active ? 'active' : ''}`}
            style={{ ['--tech' as string]: t.color }}
            aria-pressed={active}
            onClick={() => onToggle(t.slug)}
          >
            <span className="dot" />
            {t.name}
            <span className="count">{t.apiCount}</span>
          </button>
        );
      })}
    </div>
  );
}
