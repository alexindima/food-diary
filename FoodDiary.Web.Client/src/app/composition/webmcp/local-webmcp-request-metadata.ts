const GUID = /^[a-f\d]{8}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{12}$/i;
const FINGERPRINT = /^[a-f\d]{64}$/i;

// Templates come from the checked-in SDK contract. Only these static labels are returned.
const PATHS = [
    'dashboard',
    'statistics',
    'statistics/summary',
    'cycles',
    'cycles/current',
    'cycles/current/nutrition-summary',
    'cycles/{cycleProfileId}',
    'cycles/{cycleProfileId}/consents/{purpose}',
    'cycles/{cycleProfileId}/days',
    'cycles/{cycleProfileId}/factors',
    'cycles/{cycleProfileId}/menstrual-episodes/{menstrualEpisodeId}',
    'cycles/{cycleProfileId}/period-start',
    'cycles/{cycleProfileId}/settings',
    'dietologist/clients',
    'dietologist/clients/attention',
    'dietologist/clients/attention/{signalId}/state',
    'dietologist/clients/tasks/{taskId}/cancel',
    'dietologist/clients/{clientUserId}',
    'dietologist/clients/{clientUserId}/dashboard',
    'dietologist/clients/{clientUserId}/goals',
    'dietologist/clients/{clientUserId}/recommendations',
    'dietologist/clients/{clientUserId}/tasks',
    'dietologist/recommendation-templates',
    'dietologist/recommendation-templates/{templateId}',
    'dietologist/relationship',
    'dietologist/permissions',
    'products',
    'products/overview',
    'products/public/{id}',
    'products/recent',
    'products/suggestions',
    'products/{id}',
    'products/{id}/duplicate',
    'recipes',
    'recipes/explore',
    'recipes/overview',
    'recipes/public',
    'recipes/public/categories',
    'recipes/public/{id}',
    'recipes/recent',
    'recipes/{id}',
    'recipes/{id}/duplicate',
    'recipes/{recipeId}/comments',
    'recipes/{recipeId}/comments/{commentId}',
    'recipes/{recipeId}/likes',
    'recipes/{recipeId}/likes/toggle',
    'meals',
    'meals/overview',
    'meals/recognitions/{operationId}/undo',
    'meals/recognitions/{recognitionId}',
    'meals/{id}',
    'meals/{id}/repeat',
    'weight-entries',
    'weight-entries/latest',
    'weight-entries/page-summary',
    'weight-entries/summary',
    'weight-entries/{id}',
    'waist-entries',
    'waist-entries/latest',
    'waist-entries/page-summary',
    'waist-entries/summary',
    'waist-entries/{id}',
].map(path => path.split('/'));

export function developmentOperation(method: string, pathname: string): string | null {
    const parts = developmentApiPathParts(pathname);
    if (parts === null) {
        return null;
    }
    const template = PATHS.find(
        candidate => candidate.length === parts.length && candidate.every((part, index) => matchesSegment(part, parts[index])),
    );
    return template === undefined ? null : `${method} /api/v{version}/${template.join('/')}`;
}

function matchesSegment(template: string, actual: string): boolean {
    if (!template.startsWith('{')) {
        return template === actual;
    }
    return template === '{signalId}' || template === '{purpose}' || GUID.test(actual);
}

export function developmentApiPathParts(pathname: string): string[] | null {
    const [, api, version, ...parts] = pathname.split('/');
    return api === 'api' && parts.length > 0 && pathname.startsWith('/') && /^v\d+$|^v\d+\.\d+$/.test(version) ? parts : null;
}

export function developmentApiIdentity(
    apiBuild: string | null | undefined,
    apiInstance: string | null | undefined,
): { binaryFingerprint: string; instanceId: string } | null {
    return apiBuild !== null &&
        apiBuild !== undefined &&
        apiInstance !== null &&
        apiInstance !== undefined &&
        FINGERPRINT.test(apiBuild) &&
        GUID.test(apiInstance)
        ? { binaryFingerprint: apiBuild.toLowerCase(), instanceId: apiInstance.toLowerCase() }
        : null;
}
