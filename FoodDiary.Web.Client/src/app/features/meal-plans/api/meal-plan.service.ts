import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { addOptionalStringParam, type ApiQueryParams } from '../../../shared/lib/api-query-params.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ShoppingList } from '../../shopping-lists/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';

const DEFAULT_PAGE_SIZE = 50;

@Service()
export class MealPlanService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.mealPlans;

    public getPage(dietType?: string, page = 1, limit = DEFAULT_PAGE_SIZE): Observable<PageOf<MealPlanSummary>> {
        const params: ApiQueryParams = { page, limit };
        addOptionalStringParam(params, 'dietType', dietType);

        return super
            .get<PageOf<MealPlanSummary>>('', params)
            .pipe(
                catchError((error: unknown) =>
                    fallbackApiError('Get meal plans error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
                ),
            );
    }

    public getById(id: string): Observable<MealPlan> {
        return super.get<MealPlan>(id).pipe(catchError((error: unknown) => rethrowApiError('Get meal plan error', error)));
    }

    public adopt(id: string): Observable<MealPlan> {
        return super
            .post<MealPlan>(`${id}/adopt`, {})
            .pipe(catchError((error: unknown) => rethrowApiError('Adopt meal plan error', error)));
    }

    public generateShoppingList(id: string): Observable<ShoppingList> {
        return super
            .post<ShoppingList>(`${id}/shopping-list`, {})
            .pipe(catchError((error: unknown) => rethrowApiError('Generate shopping list error', error)));
    }
}
