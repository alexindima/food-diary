import type { FoodNutritionResponse, FoodVisionResponse } from './ai.data';

export type FoodRecognitionJob = {
    id: string;
    imageAssetId: string;
    imageUrl: string;
    description: string | null;
    status: 'Queued' | 'Running' | 'Succeeded' | 'Failed';
    createdOnUtc: string;
    updatedOnUtc: string;
    vision: FoodVisionResponse | null;
    nutrition: FoodNutritionResponse | null;
    errorCode: string | null;
    nutritionErrorCode: string | null;
};
