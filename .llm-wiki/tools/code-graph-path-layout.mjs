// Ranking selectors describe architectural ownership, not a particular checkout
// layout. Keep the real path first; aliases only bridge known layer boundaries.
export function rankingPathIdentities(value) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  const test = /^modules\/([^/]+)\/tests\/fooddiary\.modules\.([^/]+)\.(application|domain|infrastructure(?:\.integration)?)\.tests\/(.+)$/.exec(path);
  if (test && test[1] === test[2]) return [path, `tests/fooddiary.${test[3]}.tests/${test[4]}`];
  const match = /^modules\/([^/]+)\/(application|domain|infrastructure)\/(.+)$/.exec(path);
  if (!match || /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) return [path];
  const [, module, layer, tail] = match;
  if (layer === 'application') {
    return [path, tail.startsWith('abstractions/')
      ? `fooddiary.application.abstractions/${tail.slice('abstractions/'.length)}`
      : `fooddiary.application.${module}/${tail}`];
  }
  if (layer === 'domain') return [path, `fooddiary.domain/${tail}`];
  if (tail.startsWith('providers/')) return [path, `fooddiary.integrations/${tail.slice('providers/'.length)}`];
  return [path, tail.startsWith('model/')
    ? `fooddiary.infrastructure/persistence/${tail.slice('model/'.length)}`
    : `fooddiary.infrastructure/${tail}`];
}

export function rankingModuleIdentity(value) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  const parts = path.split('/');
  const root = parts[0] === 'modules' && parts.length >= 4 ? parts[1] : parts[0];
  return root.replace(/^fooddiary\.application\./, '').replace(/^fooddiary\./, '')
    .replaceAll(/[^\p{L}\p{N}]/gu, '');
}

// An omitted change type must not erase explicit architectural intent already
// declared by the ranking policy. Explicit documentation requests still win at
// the call site; neutral queries retain the original Any behavior.
export function implicitImplementationIntent(changeType, terms, affinities) {
  if (String(changeType).toLowerCase() !== 'any') return false;
  const strongIntent = ['domainIntentTerms', 'apiIntentTerms', 'databaseIntentTerms', 'integrationIntentTerms']
    .some(key => (affinities[key] ?? []).some(term => terms.includes(String(term).toLowerCase())));
  const infrastructureIntent = !(affinities.infrastructureExcludedIntentTerms ?? [])
    .some(term => terms.includes(String(term).toLowerCase())) &&
    (affinities.infrastructureIntentTerms ?? []).filter(term => terms.includes(String(term).toLowerCase())).length >=
      Number(affinities.infrastructureMinimumMatches ?? 2);
  return strongIntent || infrastructureIntent;
}

export function directIdentifierTermMatchesMinimum(term, minimum) {
  return term.length >= minimum || (term.length >= 2 &&
    /^[\p{L}\p{N}]+$/u.test(term) && /\p{L}/u.test(term) && /\p{N}/u.test(term));
}
