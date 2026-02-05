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
