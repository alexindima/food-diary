import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { AiFoodService } from '../../../shared/api/ai-food.service';
import { FoodRecognitionService } from '../../../shared/api/food-recognition.service';
import type { FoodNutritionRequest, FoodNutritionResponse, FoodVisionRequest, FoodVisionResponse } from '../../../shared/models/ai.data';
import type { FoodRecognitionId } from '../../../shared/models/semantics/entity-id';

@Service()
export class ProductAiRecognitionFacade {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly recognition = inject(FoodRecognitionService);

    public recognitionCount(): Observable<number> {
        return this.recognition.list(1, 1, true).pipe(map(page => page.totalItems));
    }

    public resumeRecognition(id: FoodRecognitionId): Observable<FoodVisionResponse> {
        return this.recognition.resume(id);
    }

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.aiFoodService.analyzeFoodImage(request);
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.aiFoodService.calculateNutrition(request);
    }
}
