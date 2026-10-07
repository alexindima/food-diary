import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import { recipeFromSdk } from '../../../shared/api/sdk/recipe-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkQueryString } from '../../../shared/api/sdk/sdk-query';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ExploreFilters, ExploreRecipe } from '../models/explore.data';

@Service()
export class ExploreService {
    protected readonly baseUrl = `${environment.apiUrls.recipes}/explore`;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public query(page: number, limit: number, filters?: ExploreFilters): Observable<PageOf<ExploreRecipe>> {
        return this.sdk.client
            .getRecipesExplore({
                version: this.sdk.version,
                page,
                limit,
                search: sdkQueryString(filters?.search?.trim()),
                category: sdkQueryString(filters?.category),
                maxPrepTime: filters?.maxPrepTime,
                sortBy: filters?.sortBy,
            })
            .pipe(
                map(value => sdkPage(value, recipeFromSdk)),
                catchError((error: unknown) =>
                    fallbackApiError('Explore recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
                ),
            );
    }
}
