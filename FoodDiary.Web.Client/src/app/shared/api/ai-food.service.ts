import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FoodNutritionRequest, FoodNutritionResponse, FoodVisionRequest, FoodVisionResponse, UserAiUsageResponse } from '../models/ai.data';

@Injectable({ providedIn: 'root' })
export class AiFoodService {
    private readonly baseUrl = environment.apiUrls.ai;

    public constructor(private readonly http: HttpClient) {}

    public analyzeFoodImage(request: FoodVisionRequest): Observable<FoodVisionResponse> {
        return this.http.post<FoodVisionResponse>(`${this.baseUrl}/food/vision`, request).pipe(
            catchError(error => {
                console.error('Food image analysis error', error);
                return throwError(() => error);
            }),
        );
    }

    public calculateNutrition(request: FoodNutritionRequest): Observable<FoodNutritionResponse> {
        return this.http.post<FoodNutritionResponse>(`${this.baseUrl}/food/nutrition`, request).pipe(
            catchError(error => {
                console.error('Food nutrition calculation error', error);
                return throwError(() => error);
            }),
        );
    }

    public getUsageSummary(): Observable<UserAiUsageResponse | null> {
        return this.http.get<UserAiUsageResponse>(`${this.baseUrl}/usage/me`).pipe(
            catchError(error => {
                console.error('AI usage summary fetch error', error);
                return of(null);
            }),
        );
    }
}
