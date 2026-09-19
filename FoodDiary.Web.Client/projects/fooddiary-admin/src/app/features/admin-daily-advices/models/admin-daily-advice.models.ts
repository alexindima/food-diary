export type AdminDailyAdviceItem = {
    value: string;
    locale: string;
    weight?: number;
    tag?: string | null;
};

export type AdminDailyAdvice = {
    id: string;
    ru: string | null;
    en: string | null;
    weight: number;
    tag: string | null;
};

export type AdminDailyAdviceUpdate = {
    ru: string;
    en: string;
    weight: number;
    tag: string | null;
};

export type AdminDailyAdvicesImportRequest =
    | {
          version: 1;
          advices: AdminDailyAdviceItem[];
      }
    | {
          version: 2;
          advices: Array<AdminDailyAdviceUpdate & { id: string }>;
      };

export type AdminDailyAdvicesImportResponse = {
    importedCount: number;
    skippedCount: number;
};
