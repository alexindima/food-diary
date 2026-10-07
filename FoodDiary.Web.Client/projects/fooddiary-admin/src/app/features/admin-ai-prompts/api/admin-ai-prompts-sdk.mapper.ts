import type { AdminAiPromptHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-prompt-http-response';
import type { AdminAiPromptScenarioHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-prompt-scenario-http-response';
import { requireSdkFields, sdkEnum, sdkOptional } from '../../../shared/api/sdk/sdk-response';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';
import type { AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';

export function adminAiPromptFromSdk(response: AdminAiPromptHttpResponse): AdminAiPrompt {
    const value = requireSdkFields(response, ['id', 'key', 'locale', 'promptText', 'version', 'isActive', 'createdOnUtc']);
    return { ...value, updatedOnUtc: value.updatedOnUtc ?? null };
}

export function adminAiPromptScenarioFromSdk(response: AdminAiPromptScenarioHttpResponse): AdminAiPromptScenario {
    const value = requireSdkFields(response, [
        'key',
        'locale',
        'promptText',
        'source',
        'sourceLocale',
        'inheritedPromptText',
        'inheritedSource',
        'variables',
        'responseFormatJson',
    ]);
    return {
        ...value,
        key: sdkEnum(value.key, ['vision', 'text-parse', 'nutrition', 'product-label'] as const),
        source: sdkEnum(value.source, ['custom', 'english', 'built-in'] as const),
        inheritedSource: sdkEnum(value.inheritedSource, ['custom', 'english', 'built-in'] as const),
        template: sdkOptional(value.template, adminAiPromptFromSdk),
    };
}
