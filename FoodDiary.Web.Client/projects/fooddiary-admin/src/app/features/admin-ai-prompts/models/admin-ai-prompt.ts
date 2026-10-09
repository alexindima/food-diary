import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminAiPrompt = {
    id: AdminId<'ai-prompt'>;
    key: string;
    locale: string;
    promptText: string;
    version: number;
    isActive: boolean;
    createdOnUtc: AdminUtcInstant;
    updatedOnUtc: AdminUtcInstant | null;
};
