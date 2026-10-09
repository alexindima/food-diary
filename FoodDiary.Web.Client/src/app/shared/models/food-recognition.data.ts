import type { FoodNutritionResponse, FoodVisionResponse } from './ai.data';
import type { UtcInstant } from './semantics/date-value';
import type { FoodRecognitionId, ImageAssetId } from './semantics/entity-id';
import type { PublicImageUrl } from './semantics/image-location';

export const RECOGNITION_PAGE_SIZE = 20;

export type FoodRecognitionJob = {
    isProductLabel?: boolean;
    additionalImages?: Array<{ imageAssetId: ImageAssetId; imageUrl: PublicImageUrl }>;
    id: FoodRecognitionId;
    imageAssetId: ImageAssetId;
    imageUrl: PublicImageUrl;
    description: string | null;
    status: 'Queued' | 'Running' | 'Succeeded' | 'Failed';
    createdOnUtc: UtcInstant;
    updatedOnUtc: UtcInstant;
    vision: FoodVisionResponse | null;
    nutrition: FoodNutritionResponse | null;
    errorCode: string | null;
    nutritionErrorCode: string | null;
};
