import type { AdminAiPrompt } from './admin-ai-prompt';

export type AdminAiPromptKey = 'vision' | 'text-parse' | 'nutrition' | 'product-label';
export type AdminAiPromptSource = 'custom' | 'english' | 'built-in';
export type AdminAiPromptScenario = {
    key: AdminAiPromptKey;
    locale: string;
    promptText: string;
    source: AdminAiPromptSource;
    sourceLocale: string;
    inheritedPromptText: string;
    inheritedSource: AdminAiPromptSource;
    template: AdminAiPrompt | null;
    variables: string[];
    responseFormatJson: string;
};
export type AdminAiPromptDraft = {
    key: AdminAiPromptKey;
    locale: string;
    promptText: string;
    text?: string;
    imageAssetId?: string;
    foodName?: string;
    amount?: number;
    unit?: string;
};
