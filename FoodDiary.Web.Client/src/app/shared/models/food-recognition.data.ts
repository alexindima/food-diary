import type { FoodNutritionResponse, FoodVisionResponse } from './ai.data';

export const RECOGNITION_PAGE_SIZE = 20;

export type FoodRecognitionJob = {
    isProductLabel?: boolean;
    additionalImages?: Array<{ imageAssetId: string; imageUrl: string }>;
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
