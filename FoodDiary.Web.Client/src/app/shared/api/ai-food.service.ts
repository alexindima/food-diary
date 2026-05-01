import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { fallbackApiError, rethrowApiError } from '../lib/api-error.utils';
import {
    FoodNutritionRequest,
    FoodNutritionResponse,
    FoodTextRequest,
    FoodVisionRequest,
    FoodVisionResponse,
    UserAiUsageResponse,
} from '../models/ai.data';

@Injectable({ providedIn: 'root' })
export class AiFoodService {
    private readonly baseUrl = environment.apiUrls.ai;
    private readonly http = inject(HttpClient);

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.http
            .post<FoodVisionResponse>(`${this.baseUrl}/food/vision`, request)
            .pipe(catchError(error => rethrowApiError('Food image analysis error', error)));
    }

    public parseFoodText(request: FoodTextRequest): Observable<FoodVisionResponse> {
        return this.http
            .post<FoodVisionResponse>(`${this.baseUrl}/food/text`, request)
            .pipe(catchError(error => rethrowApiError('Food text parsing error', error)));
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.http
            .post<FoodNutritionResponse>(`${this.baseUrl}/food/nutrition`, request)
            .pipe(catchError(error => rethrowApiError('Food nutrition calculation error', error)));
    }

    public getUsageSummary(): Observable<UserAiUsageResponse | null> {
        return this.http
            .get<UserAiUsageResponse>(`${this.baseUrl}/usage/me`)
            .pipe(catchError(error => fallbackApiError('AI usage summary fetch error', error, null)));
    }
}
