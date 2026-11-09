export type TelegramDiaryGoals = {
    calories: number | null;
    protein: number | null;
    fat: number | null;
    carbs: number | null;
    fiber: number | null;
    water: number | null;
    steps: number | null;
    weight: number | null;
    waist: number | null;
};

export type TelegramDiaryPreview = {
    counts: { meals: number; recipes: number; products: number; waterEntries: number; measurements: number };
    emailGoals: TelegramDiaryGoals;
    telegramGoals: TelegramDiaryGoals;
    canUseTelegramGoals: boolean;
};
