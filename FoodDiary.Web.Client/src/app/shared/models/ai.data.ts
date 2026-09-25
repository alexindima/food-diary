export type FoodVisionItem = {
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    confidence: number;
    centerX?: number | null;
    centerY?: number | null;
    locationConfidence?: number | null;
};

export type FoodVisionResponse = {
    items: FoodVisionItem[];
    notes?: string | null;
    productLabel?: ProductLabel | null;
    recognition?: { id: string; nutrition: FoodNutritionResponse | null; errorCode: string | null };
};

export type ProductLabel = {
    name: string | null;
    brand: string | null;
    baseAmount: number | null;
    baseUnit: 'g' | 'ml' | 'pcs' | null;
    calories: number | null;
    protein: number | null;
    fat: number | null;
    carbs: number | null;
    fiber: number | null;
    alcohol: number | null;
    notes: string | null;
};

export type FoodVisionRequest = {
    isProductLabel?: boolean;
    additionalImageAssetIds?: string[];
    imageAssetId: string;
    description?: string | null;
};

export type FoodTextRequest = {
    text: string;
};

export type FoodNutritionItem = {
    name: string;
    amount: number;
    unit: string;
    calories: number;
    protein: number;
    fat: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};

export type FoodNutritionResponse = {
    calories: number;
    protein: number;
    fat: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    items: FoodNutritionItem[];
    notes?: string | null;
};

export type FoodNutritionRequest = {
    items: FoodVisionItem[];
};

export type UserAiUsageResponse = {
    inputLimit: number;
    outputLimit: number;
    inputUsed: number;
    outputUsed: number;
    resetAtUtc: string;
};
