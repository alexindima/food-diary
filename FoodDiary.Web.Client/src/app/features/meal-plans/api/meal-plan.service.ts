import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { MealPlansSdk } from '../../../shared/api/sdk/generated/api/meal-plans.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkQueryString } from '../../../shared/api/sdk/sdk-query';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { shoppingListFromSdk } from '../../../shared/api/sdk/shopping-sdk.mapper';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ShoppingList } from '../../../shared/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { mealPlanFromSdk, mealPlanSummaryFromSdk } from './meal-plan-sdk.mapper';

const DEFAULT_PAGE_SIZE = 50;

@Service()
export class MealPlanService {
    protected readonly baseUrl = environment.apiUrls.mealPlans;
    private readonly sdk = createSdkConnection(MealPlansSdk, this.baseUrl, inject(HttpClient));

    public getPage(dietType?: string, page = 1, limit = DEFAULT_PAGE_SIZE): Observable<PageOf<MealPlanSummary>> {
        return this.sdk.client.getMealPlans({ version: this.sdk.version, page, limit, dietType: sdkQueryString(dietType) }).pipe(
            map(value => sdkPage(value, mealPlanSummaryFromSdk)),
            catchError((error: unknown) => rethrowApiError('Get meal plans error', error)),
        );
    }

    public getById(id: string): Observable<MealPlan> {
        return this.sdk.client.getMealPlansById({ version: this.sdk.version, id }).pipe(
            map(mealPlanFromSdk),
            catchError((error: unknown) => rethrowApiError('Get meal plan error', error)),
        );
    }

    public adopt(id: string): Observable<MealPlan> {
        return this.sdk.client.postMealPlansByIdAdopt({ version: this.sdk.version, id }).pipe(
            map(mealPlanFromSdk),
            catchError((error: unknown) => rethrowApiError('Adopt meal plan error', error)),
        );
    }

    public deletePlan(id: string): Observable<void> {
        return this.sdk.client
            .deleteMealPlansById({ version: this.sdk.version, id })
            .pipe(catchError((error: unknown) => rethrowApiError('Delete meal plan error', error)));
    }

    public generateShoppingList(id: string): Observable<ShoppingList> {
        return this.sdk.client.postMealPlansByIdShoppingList({ version: this.sdk.version, id }).pipe(
            map(shoppingListFromSdk),
            catchError((error: unknown) => rethrowApiError('Generate shopping list error', error)),
        );
    }
}
