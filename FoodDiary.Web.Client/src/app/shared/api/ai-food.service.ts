import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { fallbackApiError, rethrowApiError } from '../lib/api-error.utils';
import type {
    FoodNutritionRequest,
    FoodNutritionResponse,
    FoodTextRequest,
    FoodVisionRequest,
    FoodVisionResponse,
    UserAiUsageResponse,
} from '../models/ai.data';
import type { RecipeImportRequest, RecipeImportResult } from '../models/recipe-import.data';
import { FoodRecognitionService } from './food-recognition.service';

@Service()
export class AiFoodService {
    private readonly baseUrl = environment.apiUrls.ai;
    private readonly http = inject(HttpClient);
    private readonly recognition = inject(FoodRecognitionService);

    public importRecipe(request: RecipeImportRequest): Observable<RecipeImportResult> {
        return this.http.post<RecipeImportResult>(`${this.baseUrl}/food/recipe-import`, request, this.createIdempotencyOptions());
    }

    public importRecipeVideo(request: RecipeImportRequest, video: File | null): Observable<RecipeImportResult> {
        const body = new FormData();
        if (request.sourceUrl !== null) {
            body.append('sourceUrl', request.sourceUrl);
        }
        if (request.text !== null) {
            body.append('text', request.text);
        }
        if (video !== null) {
            body.append('video', video);
        }
        return this.http.post<RecipeImportResult>(`${this.baseUrl}/food/recipe-import/video`, body, this.createIdempotencyOptions());
    }

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.recognition.start(request);
    }

    public parseFoodText(request: FoodTextRequest): Observable<FoodVisionResponse> {
        return this.http
            .post<FoodVisionResponse>(`${this.baseUrl}/food/text`, request, this.createIdempotencyOptions())
            .pipe(catchError((error: unknown) => rethrowApiError('Food text parsing error', error)));
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.http
            .post<FoodNutritionResponse>(`${this.baseUrl}/food/nutrition`, request, this.createIdempotencyOptions())
            .pipe(catchError((error: unknown) => rethrowApiError('Food nutrition calculation error', error)));
    }

    public getUsageSummary(): Observable<UserAiUsageResponse | null> {
        return this.http
            .get<UserAiUsageResponse>(`${this.baseUrl}/usage/me`)
            .pipe(catchError((error: unknown) => fallbackApiError('AI usage summary fetch error', error, null)));
    }

    private createIdempotencyOptions(): { headers: { 'Idempotency-Key': string } } {
        return { headers: { 'Idempotency-Key': crypto.randomUUID() } };
    }
}
