// Ranking selectors describe architectural ownership, not a particular checkout
// layout. Keep the real path first; aliases only bridge known layer boundaries.
export function rankingPathIdentities(value) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  const sharedTest = /^(?:shared|tooling)\/(tests\/[^/]+\.tests\/.+)$/.exec(path);
  if (sharedTest) return [path, sharedTest[1]];
  if (path.startsWith('shared/fooddiary.domain.primitives/') &&
    !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.domain/${path.slice('shared/fooddiary.domain.primitives/'.length)}`];
  }
  const test = /^modules\/([^/]+)\/tests\/fooddiary\.modules\.([^/]+)\.((?:application|domain|infrastructure(?:\.integration)?)\.tests|infrastructure\.integrationtests)\/(.+)$/.exec(path);
  if (test && test[1] === test[2]) return [path, `tests/fooddiary.${test[3]}/${test[4]}`];
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

// A named module's application contract is a valid entry point unless a concrete
// implementation/layer was requested. This waives penalties; it adds no boost.
export function isModuleEntryPointQuery(value, changeType, directTerms, terms, policy) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  if (!['backend', 'any'].includes(String(changeType).toLowerCase()) ||
    !/^modules\/[^/]+\/application\//.test(path) ||
    /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) return false;
  const module = rankingModuleIdentity(path);
  if (module.length < Number(policy.moduleIdentityMinimumLength ?? 8) ||
    !directTerms.slice(0, Number(policy.moduleIdentityLeadingTermCount ?? 1))
      .map(term => term.replaceAll(/[^\p{L}\p{N}]/gu, '')).includes(module)) return false;
  const affinity = policy.genericAffinities ?? {};
  return !['domainIntentTerms', 'apiIntentTerms', 'databaseIntentTerms', 'integrationIntentTerms', 'infrastructureIntentTerms']
    .flatMap(key => affinity[key] ?? []).concat(['implementation', 'options', 'configuration', 'test'])
    .some(term => terms.includes(String(term).toLowerCase()));
}

// Reward a directly named test subject rather than common words like provider or
// client. Integer log buckets preserve runtime parity; duplicate index records
// for the same path cannot distort document frequency. Use existing score caps.
export function testIdentityWeights(directTerms, candidates, affinity) {
  const identities = new Map(candidates.map(item => [item.path.replaceAll('\\', '/').toLowerCase(), item.identity]));
  return new Map([...new Set(directTerms)]
    .filter(term => !['test', 'tests', 'spec', 'specs', 'feature', 'features'].includes(term))
    .filter(term => directIdentifierTermMatchesMinimum(term, Number(affinity.minimumTermLength ?? 3)))
    .map(term => {
      const frequency = [...identities.values()].filter(identity => identity.includes(term)).length;
      const buckets = frequency === 0 ? 0 : Math.floor(Math.log2((identities.size + 1) / (frequency + 1)));
      return [term, Math.min(buckets * Number(affinity.scorePerMatch ?? 0), Number(affinity.maximumScore ?? 0))];
    }));
}
