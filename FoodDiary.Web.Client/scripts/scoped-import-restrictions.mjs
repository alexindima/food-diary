import { builtinRules } from 'eslint/use-at-your-own-risk';

// Flat config replaces repeated rule options. Independent scopes must keep
// independent rule IDs so a UI restriction cannot erase a layer restriction.
export const scopedImportRestrictions = {
    meta: { name: 'fooddiary-import-scopes' },
    rules: {},
};

export function restrictImports(scope, options) {
    scopedImportRestrictions.rules[scope] = builtinRules.get('no-restricted-imports');
    return { [`scope-imports/${scope}`]: options };
}
