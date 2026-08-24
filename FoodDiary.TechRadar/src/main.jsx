import React, { useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import * as d3 from 'd3';
import catalog from '../catalog/radar.json';
import inventory from '../generated/inventory.json';
import './styles.css';

const ringColors = ['#b8db36', '#ff7657', '#f0ad27', '#4a93d8'];
const ringNames = ['ADOPT', 'TRIAL', 'ASSESS', 'HOLD'];

function pointFor(entry, index) {
  const radiusBands = [[24, 31], [34, 47], [51, 64], [68, 82]];
  const [min, max] = radiusBands[entry.ring];
  const seed = ((index + 1) * 47 + entry.quadrant * 23) % 100;
  const radius = min + ((max - min) * seed) / 100;
  const quadrantStart = [-90, 0, 90, 180][entry.quadrant];
  const angle = (quadrantStart + 12 + ((index * 67) % 66)) * (Math.PI / 180);
  return { x: 50 + Math.cos(angle) * radius / 2, y: 50 + Math.sin(angle) * radius / 2 };
}

function Radar({ entries, selectedId, onSelect }) {
  return (
    <div className="radar-wrap" aria-label="Interactive technology radar">
      <svg className="radar" viewBox="0 0 100 100" role="img">
        <title>FoodDiary technology radar</title>
        {[14, 28, 42, 49].map((r, i) => <circle key={r} cx="50" cy="50" r={r} className={`ring ring-${i}`} />)}
        <path d="M50 1V99M1 50H99" className="axis" />
        {entries.map((entry, index) => {
          const point = pointFor(entry, index);
          const selected = entry.id === selectedId;
          return (
            <g key={entry.id} className={`blip ${selected ? 'selected' : ''}`} role="button" tabIndex="0"
              aria-label={`${entry.name}, ${ringNames[entry.ring]}`}
              onClick={() => onSelect(entry.id)}
              onKeyDown={(event) => event.key === 'Enter' && onSelect(entry.id)}>
              <circle cx={point.x} cy={point.y} r={selected ? 2.8 : 2.15} fill="#101820" stroke={ringColors[entry.ring]} />
              <text x={point.x} y={point.y + 0.8}>{index + 1}</text>
            </g>
          );
        })}
      </svg>
      <span className="quadrant q0">Languages &amp; Frameworks</span>
      <span className="quadrant q1">Architecture &amp; Practices</span>
      <span className="quadrant q2">Infrastructure &amp; Quality</span>
      <span className="quadrant q3">Data &amp; Integrations</span>
    </div>
  );
}

function Detail({ entry, index }) {
  const facts = inventory.technologies[entry.id] ?? {};
  return (
    <aside className="detail">
      <div className="detail-heading">
        <span className="detail-number" style={{ borderColor: ringColors[entry.ring] }}>{index + 1}</span>
        <div><h2>{entry.name}</h2><p className="status"><i style={{ background: ringColors[entry.ring] }} />{ringNames[entry.ring]}</p></div>
      </div>
      <p className="description">{entry.description}</p>
      <h3>Repository evidence</h3>
      <ul className="evidence">
        {entry.evidence.map((item) => <li key={item.path}><span>↗</span><div><strong>{item.label}</strong><small>{item.path}</small></div></li>)}
      </ul>
      {facts.version && <p className="version">Detected version <strong>{facts.version}</strong></p>}
      <p className="reviewed">Reviewed {entry.reviewedAt}</p>
    </aside>
  );
}

function App() {
  const entries = catalog.entries.filter((entry) => entry.active !== false);
  const [selectedId, setSelectedId] = useState(entries[0].id);
  const [showList, setShowList] = useState(false);
  const selectedIndex = entries.findIndex((entry) => entry.id === selectedId);
  const selected = entries[selectedIndex];
  const grouped = useMemo(() => d3.group(entries, (entry) => entry.ring), [entries]);

  return <main>
    <header><a className="brand" href="https://fooddiary.club">FoodDiary <span>/</span> <b>Technology Radar</b></a>
      <nav><a href="https://fooddiary.club">Product</a><a href="https://github.com/alexindima/FoodDiary/blob/master/docs/ARCHITECTURE.md">Architecture</a><a href="https://github.com/alexindima/FoodDiary/tree/master/docs/adr">ADRs</a><a href="https://github.com/alexindima/FoodDiary">GitHub ↗</a></nav>
      <time>{catalog.dateLabel}</time>
    </header>
    <section className="intro">
      <h1>Technology choices,<br />made visible.</h1>
      <p>A living map of the tools and practices behind fooddiary.club.</p>
      <button onClick={() => setShowList(!showList)} aria-expanded={showList}>☷&nbsp;&nbsp; {showList ? 'Hide technology list' : 'View all technologies'}</button>
      <div className="legend">
        {ringNames.map((name, index) => <div key={name}><i style={{ borderColor: ringColors[index] }} /><span><strong>{name}</strong><small>{['Proven and widely used. Default choice.', 'Actively experimenting in production.', 'Promising. Under evaluation.', 'Not recommended for new work.'][index]}</small></span></div>)}
      </div>
    </section>
    <section className="canvas">
      {showList ? <div className="technology-list">{ringNames.map((ring, ringIndex) => <div key={ring}><h2 style={{ color: ringColors[ringIndex] }}>{ring}</h2>{(grouped.get(ringIndex) ?? []).map((entry) => <button key={entry.id} onClick={() => { setSelectedId(entry.id); setShowList(false); }}>{entry.name}</button>)}</div>)}</div> : <Radar entries={entries} selectedId={selectedId} onSelect={setSelectedId} />}
    </section>
    <Detail entry={selected} index={selectedIndex} />
    <footer><span>Inspired by the open-source Zalando Tech Radar.</span><span>Last updated: {catalog.updatedAt}</span><span>Repository-backed, human-reviewed.</span></footer>
  </main>;
}

createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>);
