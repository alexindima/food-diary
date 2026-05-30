import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AiFoodService } from '../../../shared/api/ai-food.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import type { FoodNutritionRequest, FoodNutritionResponse, FoodVisionRequest, FoodVisionResponse } from '../../../shared/models/ai.data';

@Injectable({ providedIn: 'root' })
export class ProductAiRecognitionFacade {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly imageUploadService = inject(ImageUploadService);

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.aiFoodService.analyzeFoodImage(request);
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.aiFoodService.calculateNutrition(request);
    }

    public deleteAsset(assetId: string): Observable<void> {
        return this.imageUploadService.deleteAsset(assetId);
    }
}
