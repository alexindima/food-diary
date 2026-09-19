// Physical ownership is independent of a file's role and ranking aliases.
export function contextPathOwnership(value) {
  const path = String(value ?? '').replaceAll('\\', '/');
  const parts = path.split('/');
  const lower = path.toLowerCase();
  const owner = parts[0]?.toLowerCase() === 'modules' && parts.length >= 4 ? parts[1] : parts[0];
  const isTest = /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/i.test(path);
  if (isTest) return { module: owner, layer: 'tests' };
  if (parts[0]?.toLowerCase() === 'modules' && parts.length >= 4) {
    const layers = { application: 'application', 'application.abstractions': 'abstractions',
      domain: 'domain', 'domain.contracts': 'domain', infrastructure: 'infrastructure',
      persistencemodel: 'persistence', presentation: 'api', 'presentation.contracts': 'api',
      'presentation.mappings': 'api', contracts: 'contracts', 'service.contracts': 'contracts' };
    const root = parts[2].toLowerCase();
    const nested = parts[3].toLowerCase();
    const layer = root === 'application' && nested === 'abstractions' ? 'abstractions'
      : root === 'infrastructure' && nested === 'model' ? 'persistence' : layers[root] ?? 'other';
    return { module: owner, layer };
  }
  const layer = lower.startsWith('.llm-wiki/') ? 'wiki'
    : lower.startsWith('docs/') ? 'documentation'
      : lower.startsWith('fooddiary.web.client/') ? 'frontend'
        : /presentation|web\.api/.test(lower) ? 'api'
          : /persistencemodel/.test(lower) ? 'persistence'
            : /infrastructure|integrations|jobmanager/.test(lower) ? 'infrastructure'
              : /application\.abstractions/.test(lower) ? 'abstractions'
                : /application/.test(lower) ? 'application'
                  : /domain/.test(lower) ? 'domain' : 'other';
  return { module: owner, layer };
}

export function exactFileIdentity(path, query) {
  const identity = String(query ?? '').trim().replaceAll('\\', '/').toLowerCase();
  const normalized = String(path ?? '').replaceAll('\\', '/').toLowerCase();
  const name = normalized.split('/').at(-1);
  return identity === normalized || identity === name || identity === name.replace(/\.[^.]+$/, '');
}

// Ranking selectors describe architectural ownership, not a particular checkout
// layout. Keep the real path first; aliases only bridge known layer boundaries.
export function rankingPathIdentities(value) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  const readComposition = /^fooddiary\.readmodel\.composition\/([^/]+\/.+\.cs)$/.exec(path);
  if (readComposition && !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)/.test(path)) {
    return [path, `fooddiary.infrastructure/persistence/${readComposition[1]}`];
  }
  const sharedTest = /^(?:shared|tooling|hosts|platform)\/(tests\/[^/]+tests\/.+)$/.exec(path);
  if (sharedTest) return [path, sharedTest[1]];
  const persistenceModel = /^shared\/fooddiary\.([^.\/]+)\.persistencemodel\/(.+)$/.exec(path);
  if (persistenceModel && !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    const [, owner, tail] = persistenceModel;
    return [path, tail.startsWith('configurations/')
      ? `fooddiary.infrastructure/persistence/configurations/${owner}/${tail.slice('configurations/'.length)}`
      : `fooddiary.infrastructure/persistence/${owner}/${tail}`];
  }
  const sharedContract = /^shared\/fooddiary\.([^.\/]+)\.contracts\/(.+)$/.exec(path);
  if (sharedContract && !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.application.abstractions/${sharedContract[2]}`];
  }
  const sharedPersistence = /^shared\/fooddiary\.([^.\/]+)\.infrastructure\/persistence\/(.+)$/.exec(path);
  if (sharedPersistence && !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.infrastructure/persistence/${sharedPersistence[1]}/${sharedPersistence[2]}`];
  }
  if (path.startsWith('shared/fooddiary.email.mailrelay/') && !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.integrations/email/${path.slice('shared/fooddiary.email.mailrelay/'.length)}`];
  }
  if (path.startsWith('shared/fooddiary.integrations.http/') &&
    !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.integrations/${path.slice('shared/fooddiary.integrations.http/'.length)}`];
  }
  if (path.startsWith('shared/fooddiary.domain.primitives/') &&
    !/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) {
    return [path, `fooddiary.domain/${path.slice('shared/fooddiary.domain.primitives/'.length)}`];
  }
  const test = /^modules\/([^/]+)\/tests\/fooddiary\.modules\.([^/]+)\.((?:application|domain|infrastructure(?:\.integration)?)\.tests|infrastructure\.integrationtests)\/(.+)$/.exec(path);
  if (test && test[1] === test[2]) return /infrastructure\.integration\.?tests/.test(test[3])
    ? [path, `tests/fooddiary.${test[3]}/${test[4]}`, `platform/tests/fooddiary.infrastructure.integrationtests/${test[4]}`]
    : [path, `tests/fooddiary.${test[3]}/${test[4]}`];
  const match = /^modules\/([^/]+)\/(application|application\.abstractions|domain|domain\.contracts|infrastructure|persistencemodel|presentation|contracts)\/(.+)$/.exec(path);
  if (!match || /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/.test(path)) return [path];
  const [, module, layer, tail] = match;
  if (layer === 'persistencemodel') return [path, `fooddiary.infrastructure/persistence/${tail}`];
  if (layer === 'application.abstractions') return [path, `fooddiary.application.abstractions/${tail}`];
  if (layer === 'domain.contracts') return /^(?:enums|valueobjects)\//.test(tail)
    ? [path, `fooddiary.domain/${tail}`] : [path];
  if (layer === 'presentation') return [path, `fooddiary.presentation.api/${tail}`];
  // Consumer contracts keep abstraction selectors; they gain no implementation layer.
  if (layer === 'contracts') return [path, `fooddiary.application.abstractions/${tail}`];
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
  const root = (parts[0] === 'modules' && parts.length >= 4)
    || (parts[0] === 'fooddiary.readmodel.composition' && parts.length >= 3) ? parts[1] : parts[0];
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

export function compoundModuleMention(module, terms) {
  for (let start = 0; start < terms.length; start++) {
    let phrase = terms[start];
    for (let end = start + 1; end < terms.length && end < start + 4; end++) {
      phrase += terms[end];
      if (phrase === module || (module.endsWith('s') && !module.endsWith('ss') && !module.endsWith('status') && phrase === module.slice(0, -1))) return true;
    }
  }
  return false;
}

export function directIdentifierTermMatchesMinimum(term, minimum) {
  return term.length >= minimum || (term.length >= 2 &&
    /^[\p{L}\p{N}]+$/u.test(term) && /\p{L}/u.test(term) && /\p{N}/u.test(term));
}

// A fully named compound identifier is more specific than a longer helper or
// event name sharing only its prefix. Do not boost single generic words.
export function completeFileIdentityMatches(value, terms) {
  const stem = String(value).replaceAll('\\', '/').split('/').at(-1).replace(/\.[^.]+$/, '');
  const words = stem.replace(/([\p{Ll}\p{N}])([\p{Lu}])/gu, '$1 $2').toLowerCase().match(/[\p{L}\p{N}]+/gu) ?? [];
  let matched = 0;
  for (const term of terms) if (matched < words.length && words[matched] === term) matched++;
  return words.length >= 2 && matched === words.length;
}

// CQRS handlers now own use cases formerly implemented by application services.
// These are role identities only; literal name matching still uses the real file.
export function applicationRoleIdentity(value) {
  const path = String(value ?? '').replaceAll('\\', '/').toLowerCase();
  if (!/^modules\/[^/]+\/application\/.+/.test(path) || /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)/.test(path)) return '';
  if (path.endsWith('queryhandler.cs')) {
    const stem = path.slice(0, -'queryhandler.cs'.length);
    const collection = stem.endsWith('s') && !stem.endsWith('status') && !stem.endsWith('ss') && !stem.endsWith('ids');
    return `read service reader readservice${collection ? ' collection' : ''}`;
  }
  if (path.endsWith('commandhandler.cs')) return 'service';
  return '';
}

// Preserve a hyphenated identifier's compact spelling as well as its words,
// so prose such as stock-count can match a StockCount symbol.
export function hyphenatedIdentifierTerms(value) {
  return [...new Set((String(value ?? '').toLowerCase().match(/\p{L}{2,}(?:-\p{L}{2,})+/gu) ?? [])
    .map(term => term.replaceAll('-', '')))];
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
