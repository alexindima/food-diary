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
import { type FoodRecognitionJob, RECOGNITION_PAGE_SIZE } from '../models/food-recognition.data';
import type { PageOf } from '../models/page-of.data';

@Service()
export class AiFoodFacade {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly recognition = inject(FoodRecognitionService);

    public resumeRecognition(id: string): Observable<FoodVisionResponse> {
        return this.recognition.resume(id);
    }

    public deleteRecognition(id: string): Observable<void> {
        return this.recognition.deleteRecognition(id);
    }

    public listRecognitions(page = 1, limit = RECOGNITION_PAGE_SIZE, isProductLabel = false): Observable<PageOf<FoodRecognitionJob>> {
        return this.recognition.list(page, limit, isProductLabel);
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
