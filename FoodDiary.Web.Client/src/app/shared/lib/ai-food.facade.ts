import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AiFoodService } from '../api/ai-food.service';
import { FoodRecognitionService } from '../api/food-recognition.service';
import type {
    FoodNutritionRequest,
    FoodNutritionResponse,
    FoodTextRequest,
    FoodVisionRequest,
    FoodVisionResponse,
} from '../models/ai.data';
import type { FoodRecognitionJob } from '../models/food-recognition.data';

@Service()
export class AiFoodFacade {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly recognition = inject(FoodRecognitionService);

    public resumeRecognition(id: string): Observable<FoodVisionResponse> {
        return this.recognition.resume(id);
    }

    public listRecognitions(): Observable<FoodRecognitionJob[]> {
        return this.recognition.list();
    }

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
