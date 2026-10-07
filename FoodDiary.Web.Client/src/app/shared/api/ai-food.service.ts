import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

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
import { foodNutritionFromSdk, foodVisionFromSdk, recipeImportFromSdk } from './sdk/ai-sdk.mapper';
import { AiSdk } from './sdk/generated/api/ai.service';
import { createSdkConnection } from './sdk/sdk-connection';
import { requireSdkFields } from './sdk/sdk-response';

@Service()
export class AiFoodService {
    private readonly baseUrl = environment.apiUrls.ai;
    private readonly http = inject(HttpClient);
    private readonly sdk = createSdkConnection(AiSdk, this.baseUrl, this.http);
    private readonly recognition = inject(FoodRecognitionService);

    public importRecipe(request: RecipeImportRequest): Observable<RecipeImportResult> {
        return this.sdk.client
            .postAiFoodRecipeImport({ version: this.sdk.version, idempotencyKey: crypto.randomUUID(), recipeImportHttpRequest: request })
            .pipe(map(recipeImportFromSdk));
    }

    public importRecipeVideo(request: RecipeImportRequest, video: File | null): Observable<RecipeImportResult> {
        return this.sdk.client
            .postAiFoodRecipeImportVideo({
                version: this.sdk.version,
                idempotencyKey: crypto.randomUUID(),
                sourceUrl: request.sourceUrl ?? undefined,
                text: request.text ?? undefined,
                video: video ?? undefined,
            })
            .pipe(map(recipeImportFromSdk));
    }

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.recognition.start(request);
    }

    public parseFoodText(request: FoodTextRequest): Observable<FoodVisionResponse> {
        return this.sdk.client
            .postAiFoodText({ version: this.sdk.version, idempotencyKey: crypto.randomUUID(), foodTextHttpRequest: request })
            .pipe(
                map(foodVisionFromSdk),
                catchError((error: unknown) => rethrowApiError('Food text parsing error', error)),
            );
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.sdk.client
            .postAiFoodNutrition({ version: this.sdk.version, idempotencyKey: crypto.randomUUID(), foodNutritionHttpRequest: request })
            .pipe(
                map(foodNutritionFromSdk),
                catchError((error: unknown) => rethrowApiError('Food nutrition calculation error', error)),
            );
    }

    public getUsageSummary(): Observable<UserAiUsageResponse | null> {
        return this.sdk.client.getAiUsageMe({ version: this.sdk.version }).pipe(
            map(value => requireSdkFields(value, ['inputLimit', 'outputLimit', 'inputUsed', 'outputUsed', 'resetAtUtc'])),
            catchError((error: unknown) => fallbackApiError('AI usage summary fetch error', error, null)),
        );
    }
}
