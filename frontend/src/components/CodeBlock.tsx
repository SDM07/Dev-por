import { useState } from 'react';

interface Tab {
  label: string;
  code: string;
}

export function CodeBlock({ title, tabs }: { title: string; tabs: Tab[] }) {
  const [active, setActive] = useState(0);
  const [copied, setCopied] = useState(false);
  if (!tabs.length) return null;
  const current = tabs[Math.min(active, tabs.length - 1)];

  const copy = async () => {
    await navigator.clipboard.writeText(current.code);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  };

  return (
    <div className="code-block">
      <div className="code-header">
        <span className="code-title">{title}</span>
        <div className="code-tabs" role="tablist">
          {tabs.map((t, i) => (
            <button
              key={t.label}
              role="tab"
              aria-selected={i === active}
              className={i === active ? 'active' : ''}
              onClick={() => setActive(i)}
            >
              {t.label}
            </button>
          ))}
        </div>
        <button className="copy-btn" onClick={copy} aria-label="Copiar código">
          {copied ? 'Copiado' : 'Copiar'}
        </button>
      </div>
      <pre>
        <code>{current.code}</code>
      </pre>
    </div>
  );
}
