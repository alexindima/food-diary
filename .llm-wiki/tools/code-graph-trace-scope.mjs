// Candidate scope is independent of score: a backend/module request must not
// silently widen to frontend files or similarly named neighboring modules.
export function traceCandidateMatchesScope(row, filters = {}) {
  const path = row.path.replaceAll('\\', '/').toLowerCase();
  const layer = (filters.layer ?? 'Auto').toLowerCase();
  if (layer === 'backend' && row.language !== 'csharp') return false;
  if (layer === 'frontend' && !['typescript', 'javascript'].includes(row.language)) return false;
  const prefix = (filters.pathPrefix ?? '').replaceAll('\\', '/').toLowerCase();
  if (prefix && !path.startsWith(prefix)) return false;
  const module = (filters.module ?? '').toLowerCase();
  // Match folder or dotted project segments, not FavoriteRecipes for Recipes.
  const directories = path.split('/').slice(0, -1);
  return !module || directories.some((part) => part === module || part.split('.').includes(module));
}
