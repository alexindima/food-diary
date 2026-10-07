import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminMealPlansSdk } from '../../../shared/api/sdk/generated/api/admin-meal-plans.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { CatalogPlan, CatalogPlanRequest, CatalogPlanSummary, CatalogRecipe } from '../models/admin-meal-plan.data';
import { catalogRecipeFromSdk, mealPlanFromSdk, mealPlanSummaryFromSdk } from './admin-meal-plans-sdk.mapper';

@Service()
export class AdminMealPlansService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/meal-plans`;
    private readonly sdk = createSdkConnection(AdminMealPlansSdk, this.url, this.http);

    public getAll(): Observable<CatalogPlanSummary[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAdminMealPlans({ version: this.sdk.version, page, limit })
                .pipe(map(items => items.map(mealPlanSummaryFromSdk))),
        );
    }

    public get(id: string): Observable<CatalogPlan> {
        return this.sdk.client.getAdminMealPlansById({ version: this.sdk.version, id }).pipe(map(mealPlanFromSdk));
    }

    public save(id: string | null, request: CatalogPlanRequest): Observable<CatalogPlan> {
        return id === null
            ? this.sdk.client
                  .postAdminMealPlans({ version: this.sdk.version, saveCatalogMealPlanHttpRequest: request })
                  .pipe(map(mealPlanFromSdk))
            : this.sdk.client
                  .putAdminMealPlansById({ version: this.sdk.version, id, saveCatalogMealPlanHttpRequest: request })
                  .pipe(map(mealPlanFromSdk));
    }

    public recipes(search: string): Observable<CatalogRecipe[]> {
        return this.sdk.client
            .getAdminMealPlansRecipes({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, { search }))
            .pipe(map(items => items.map(catalogRecipeFromSdk)));
    }
}
