import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { ShoppingList } from '../../shopping-lists/models/shopping-list.data';

@Injectable({
    providedIn: 'root',
})
export class MealPlanService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.mealPlans;

    public getAll(dietType?: string): Observable<MealPlanSummary[]> {
        const params: Record<string, string> = {};
        if (dietType) {
            params['dietType'] = dietType;
        }
        return super
            .get<MealPlanSummary[]>('', params)
            .pipe(catchError((error: HttpErrorResponse) => fallbackApiError('Get meal plans error', error, [])));
    }

    public getById(id: string): Observable<MealPlan> {
        return super.get<MealPlan>(`/${id}`).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Get meal plan error', error)));
    }

    public adopt(id: string): Observable<MealPlan> {
        return super
            .post<MealPlan>(`/${id}/adopt`, {})
            .pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Adopt meal plan error', error)));
    }

    public generateShoppingList(id: string): Observable<ShoppingList> {
        return super
            .post<ShoppingList>(`/${id}/shopping-list`, {})
            .pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Generate shopping list error', error)));
    }
}
