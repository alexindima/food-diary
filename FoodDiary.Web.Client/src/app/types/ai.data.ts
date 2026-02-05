export type FoodVisionItem = {
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    confidence: number;
};

export type FoodVisionResponse = {
    items: FoodVisionItem[];
    notes?: string | null;
};

export type FoodVisionRequest = {
    imageAssetId: string;
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
