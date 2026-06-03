import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AiFoodService } from '../api/ai-food.service';
import type {
    FoodNutritionRequest,
    FoodNutritionResponse,
    FoodTextRequest,
    FoodVisionRequest,
    FoodVisionResponse,
} from '../models/ai.data';

@Service()
export class AiFoodFacade {
    private readonly aiFoodService = inject(AiFoodService);

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.aiFoodService.analyzeFoodImage(request);
    }

    public parseFoodText(request: FoodTextRequest): Observable<FoodVisionResponse> {
        return this.aiFoodService.parseFoodText(request);
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.aiFoodService.calculateNutrition(request);
    }
}
