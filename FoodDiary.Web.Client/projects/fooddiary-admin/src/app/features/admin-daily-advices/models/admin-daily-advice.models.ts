export type AdminDailyAdviceItem = {
    value: string;
    locale: string;
    weight?: number;
    tag?: string | null;
};

export type AdminDailyAdvice = {
    id: string;
    weight: number;
} & AdminDailyAdviceItem;

export type AdminDailyAdvicesImportRequest = {
    version: 1;
    advices: AdminDailyAdviceItem[];
};

export type AdminDailyAdvicesImportResponse = {
    importedCount: number;
    skippedCount: number;
};
