import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { ShoppingList } from '../../shopping-lists/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';

@Injectable({ providedIn: 'root' })
export class MealPlanService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.mealPlans;

    public getAll(dietType?: string): Observable<MealPlanSummary[]> {
        const params: Record<string, string> = {};
        if (dietType !== undefined && dietType.trim().length > 0) {
            params['dietType'] = dietType;
        }
        return super
            .get<MealPlanSummary[]>('', params)
            .pipe(catchError((error: unknown) => fallbackApiError('Get meal plans error', error, [])));
    }

    public getById(id: string): Observable<MealPlan> {
        return super.get<MealPlan>(`/${id}`).pipe(catchError((error: unknown) => rethrowApiError('Get meal plan error', error)));
    }

    public adopt(id: string): Observable<MealPlan> {
        return super
            .post<MealPlan>(`/${id}/adopt`, {})
            .pipe(catchError((error: unknown) => rethrowApiError('Adopt meal plan error', error)));
    }

    public generateShoppingList(id: string): Observable<ShoppingList> {
        return super
            .post<ShoppingList>(`/${id}/shopping-list`, {})
            .pipe(catchError((error: unknown) => rethrowApiError('Generate shopping list error', error)));
    }
}
