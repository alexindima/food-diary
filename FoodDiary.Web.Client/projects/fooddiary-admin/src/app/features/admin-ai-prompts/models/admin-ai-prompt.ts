export type AdminAiPrompt = {
    id: string;
    key: string;
    locale: string;
    promptText: string;
    version: number;
    isActive: boolean;
    createdOnUtc: string;
    updatedOnUtc: string | null;
};
