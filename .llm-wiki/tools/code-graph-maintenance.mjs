import { posix } from 'node:path';
import { createHash } from 'node:crypto';
import { markdownDestinations } from './wiki-markdown-links.mjs';

// Only exact Git-confirmed moves can rewrite provenance. Narrative claims and
// ambiguous/missing targets remain review work, never speculative replacements.
export function repairWikiReferences(text, page, moves, existingPaths) {
  const findings = [];
  let frontMatter = false;
  let inSources = false;
  let repaired = 0;
  const relocate = (target, kind) => {
    if (existingPaths.has(target)) return target;
    const replacement = moves.get(target);
    const safe = replacement && existingPaths.has(replacement);
    findings.push({ kind, path: page, missing: target, replacement: safe ? replacement : null, repairable: !!safe });
    if (!safe) return target;
    repaired++;
    return replacement;
  };
  let result = text.split(/(?<=\n)/).map((line, index) => {
    if (line.trim() === '---') { frontMatter = index === 0; inSources = false; return line; }
    if (!frontMatter) return line;
    if (/^sources:\s*$/.test(line.trimEnd())) { inSources = true; return line; }
    if (/^\S/.test(line)) inSources = false;
    const match = inSources && /^(\s+-\s+)([^\r\n]+)(\r?\n)?$/.exec(line);
    if (!match) return line;
    return `${match[1]}${relocate(match[2], 'missing-source')}${match[3] ?? ''}`;
  }).join('');
  for (const node of markdownDestinations(result).reverse()) {
    const href = node.href.replace(/\\([!"#$%&'()*+,\-./:;<=>?@[\]\\^_`{|}~])/g, '$1');
    if (/^(?:[a-z][a-z0-9+.-]*:|#|\/)/i.test(href)) continue;
    const split = href.search(/[?#]/);
    const link = split < 0 ? href : href.slice(0, split);
    const suffix = split < 0 ? '' : href.slice(split);
    let decoded;
    try { decoded = decodeURIComponent(link); } catch { continue; }
    const target = posix.normalize(posix.join(posix.dirname(page), decoded));
    if (target.startsWith('../')) continue;
    const replacement = relocate(target, 'missing-link');
    if (replacement === target) continue;
    const relative = posix.relative(posix.dirname(page), replacement);
    const encoded = relative.split('/').map(segment => encodeURIComponent(segment).replaceAll('(', '%28').replaceAll(')', '%29')).join('/');
    result = result.slice(0, node.start) + encoded + suffix + result.slice(node.end);
  }
  return { text: result, findings, repaired };
}

// Derive module ownership from project identity, independently of physical
// folder names. Unknown project roles remain visible rather than guessed.
export function discoverProjectOwnership(paths) {
  return paths.filter(path => path.endsWith('.csproj')).flatMap(path => {
    const normalized = path.replaceAll('\\', '/');
    const name = normalized.split('/').at(-1).slice(0, -7);
    const match = /^FoodDiary\.Modules\.([^.]+)\.(.+)$/.exec(name);
    const layers = { Application: 'application', 'Application.Abstractions': 'abstractions',
      Domain: 'domain', 'Domain.Contracts': 'domain', Infrastructure: 'infrastructure',
      PersistenceModel: 'persistence', Presentation: 'api', 'Presentation.Contracts': 'api',
      'Presentation.Mappings': 'api', Contracts: 'contracts', 'Service.Contracts': 'contracts',
      // Products/FoodQuality/AGENTS.md owns the pure score/grade formula.
      FoodQuality: 'domain' };
    let owner, role, layer;
    if (match) {
      [, owner, role] = match;
      layer = /Tests?$/.test(role) ? 'tests' : layers[role];
    } else {
      // Project identity remains stable when hosts/services/shared libraries move.
      // Unknown identities stay in the inventory and become explicit findings.
      owner = name;
      role = name.replace(/^FoodDiary\./, '');
      const suffixRoles = { ...layers, 'Domain.Primitives': 'domain', 'Presentation.Api': 'api',
        'Application.Runtime': 'application', 'Persistence.Runtime': 'persistence',
        'Persistence.Abstractions': 'abstractions', Abstractions: 'abstractions',
        'Integrations.Http': 'infrastructure', 'Email.MailRelay': 'infrastructure',
        'ReadModel.Composition': 'persistence', Client: 'contracts', WebApi: 'api',
        'Web.Api': 'api', Initializer: 'host', JobManager: 'host', 'Telegram.Bot': 'host',
        Results: 'domain', Mediator: 'application', Testing: 'tests', Analyzers: 'tooling',
        'Development.Mcp': 'tooling', 'LlmWiki.RoslynExtractor': 'wiki',
        'LlmWiki.ContractReferenceExtractor': 'wiki', 'LlmWiki.SqliteReader': 'wiki' };
      const suffix = Object.keys(suffixRoles).sort((a, b) => b.length - a.length)
        .find(candidate => role === candidate || role.endsWith(`.${candidate}`));
      layer = /Tests?$/.test(role) ? 'tests' : suffixRoles[suffix];
    }
    return [{ project: normalized, root: normalized.slice(0, normalized.lastIndexOf('/') + 1), module: owner,
      layer: layer ?? 'unknown', role }];
  }).sort((a, b) => b.root.length - a.root.length || a.project.localeCompare(b.project));
}

export function projectOwnership(path, projects) {
  const normalized = path.replaceAll('\\', '/').toLowerCase();
  return projects.find(project => normalized.startsWith(project.root.toLowerCase()));
}

export function inspectProjectionOwnership(database, projects, repair = false) {
  const rows = database.prepare(`SELECT search.rowid id, search.path, features.module, features.layer
    FROM context_search search LEFT JOIN context_search_features features ON features.context_rowid = search.rowid`).all();
  const findings = inspectProjectionCompleteness(database);
  for (const project of projects.filter(project => project.layer === 'unknown')) {
    findings.push({ kind: 'unknown-project-role', path: project.project, role: project.role,
      repairable: false, action: 'Define the architectural role from the owning project guide; add its classification and regression before rebuilding.' });
  }
  const repairs = [];
  for (const row of rows) {
    if (row.module === null || row.layer === null) continue;
    const owner = projectOwnership(row.path, projects);
    if (!owner || owner.layer === 'unknown' || (row.module === owner.module && row.layer === owner.layer)) continue;
    repairs.push({ id: row.id, module: owner.module, layer: owner.layer });
    findings.push({ kind: 'projection-ownership-drift', path: row.path, project: owner.project,
      actual: { module: row.module, layer: row.layer }, expected: { module: owner.module, layer: owner.layer },
      repairable: true, action: 'Rebuild derived ownership from the current project identity.' });
  }
  if (repair && repairs.length > 0) {
    database.exec('BEGIN IMMEDIATE');
    try {
      const update = database.prepare('UPDATE context_search_features SET module = ?, layer = ? WHERE context_rowid = ?');
      for (const row of repairs) update.run(row.module, row.layer, row.id);
      database.exec('COMMIT');
    } catch (error) { database.exec('ROLLBACK'); throw error; }
  }
  return { checkedProjects: projects.length, checkedRecords: rows.length, findingCount: findings.length,
    fingerprint: createHash('sha256').update(JSON.stringify(rows.map(row => {
      const owner = repair && projectOwnership(row.path, projects);
      return [row.path, owner && owner.layer !== 'unknown' ? owner.module : row.module,
        owner && owner.layer !== 'unknown' ? owner.layer : row.layer];
    }).sort((a, b) => a[0].localeCompare(b[0])))).digest('hex'),
    repairedRecords: repair ? repairs.length : 0, unresolvedCount: findings.length - (repair ? repairs.length : 0),
    findings };
}

// Physical SQLite integrity and a source fingerprint cannot prove that every
// derived record exists. Rebuild the complete projection rather than inventing
// only module/layer and leaving role, test status or identity absent.
export function inspectProjectionCompleteness(database) {
  const findings = [];
  for (const row of database.prepare(`SELECT search.rowid id, search.path, COUNT(features.context_rowid) count
    FROM context_search search LEFT JOIN context_search_features features ON features.context_rowid = search.rowid
    GROUP BY search.rowid HAVING COUNT(features.context_rowid) <> 1`).all()) {
    findings.push({ kind: row.count === 0 ? 'missing-projection-features' : 'duplicate-projection-features',
      path: row.path, recordId: row.id, repairable: true, action: 'Rebuild the complete search projection.' });
  }
  for (const row of database.prepare(`SELECT features.context_rowid id FROM context_search_features features
    LEFT JOIN context_search search ON search.rowid = features.context_rowid WHERE search.rowid IS NULL`).all()) {
    findings.push({ kind: 'orphan-projection-features', path: '', recordId: row.id,
      repairable: true, action: 'Rebuild the complete search projection.' });
  }
  return findings;
}
