import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { favoriteRecipeFromSdk } from '../../../shared/api/sdk/favorite-sdk.mapper';
import { type GetRecipesRequestParams, RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import { recipeFromSdk } from '../../../shared/api/sdk/recipe-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkQueryString, sdkQueryValue } from '../../../shared/api/sdk/sdk-query';
import { requireSdkFields, sdkOptional, sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { Recipe, RecipeDto, RecipeFilters, RecipeOverview } from '../../../shared/models/recipe.data';
import { RECIPE_API_LIMITS } from './recipe-api.tokens';

export type RecipeOverviewQuery = {
    page: number;
    limit: number;
    filters?: RecipeFilters;
    includePublic?: boolean;
    recentLimit?: number;
    favoriteLimit?: number;
};

@Service()
export class RecipeService {
    private readonly defaultLimits = inject(RECIPE_API_LIMITS);

    protected readonly baseUrl = environment.apiUrls.recipes;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public query(page: number, limit: number, filters?: RecipeFilters, includePublic = true): Observable<PageOf<Recipe>> {
        return this.sdk.client.getRecipes({ ...this.recipeFiltersToSdk(filters), page, limit, includePublic }).pipe(
            map(value => sdkPage(value, recipeFromSdk)),
            catchError((error: unknown) =>
                fallbackApiError('Query recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public getById(id: string, includePublic = true): Observable<Recipe | null> {
        return this.sdk.client.getRecipesById({ version: this.sdk.version, id, includePublic }).pipe(
            map(value => sdkOptional(value, recipeFromSdk)),
            catchError((error: unknown) => fallbackApiError('Get recipe error', error, null)),
        );
    }

    public getRecent(limit?: number, includePublic = true): Observable<Recipe[]> {
        return this.sdk.client
            .getRecipesRecent({ version: this.sdk.version, limit: limit ?? this.defaultLimits.recent, includePublic })
            .pipe(
                map(values => values.map(recipeFromSdk)),
                catchError((error: unknown) => fallbackApiError('Get recent recipes error', error, [])),
            );
    }

    public queryOverview(query: RecipeOverviewQuery): Observable<RecipeOverview> {
        const {
            page,
            limit,
            filters,
            includePublic = true,
            recentLimit = this.defaultLimits.overviewRecent,
            favoriteLimit = this.defaultLimits.overviewFavorite,
        } = query;
        return this.sdk.client
            .getRecipesOverview({ ...this.recipeFiltersToSdk(filters), page, limit, includePublic, recentLimit, favoriteLimit })
            .pipe(
                map(response => {
                    const value = requireSdkFields(response, ['recentItems', 'allRecipes', 'favoriteItems', 'favoriteTotalCount']);
                    return {
                        ...value,
                        recentItems: value.recentItems.map(recipeFromSdk),
                        allRecipes: sdkPage(value.allRecipes, recipeFromSdk),
                        favoriteItems: value.favoriteItems.map(favoriteRecipeFromSdk),
                    };
                }),
                catchError((error: unknown) => rethrowApiError('Query recipe overview error', error)),
            );
    }

    public create(data: RecipeDto): Observable<Recipe> {
        return this.sdk.client.postRecipes({ version: this.sdk.version, createRecipeHttpRequest: data }).pipe(
            map(recipeFromSdk),
            catchError((error: unknown) => rethrowApiError('Create recipe error', error)),
        );
    }

    private recipeFiltersToSdk(filters?: RecipeFilters): GetRecipesRequestParams {
        return {
            version: this.sdk.version,
            search: sdkQueryString(filters?.search?.trim()),
            category: sdkQueryString(filters?.category?.trim()),
            maxTotalTime: sdkQueryValue(filters?.maxTotalTime),
            caloriesFrom: sdkQueryValue(filters?.caloriesFrom),
            caloriesTo: sdkQueryValue(filters?.caloriesTo),
            hasImage: sdkQueryValue(filters?.hasImage),
        };
    }

    public update(id: string, data: RecipeDto): Observable<Recipe> {
        return this.sdk.client.patchRecipesById({ version: this.sdk.version, id, updateRecipeHttpRequest: data }).pipe(
            map(recipeFromSdk),
            catchError((error: unknown) => rethrowApiError('Update recipe error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.sdk.client
            .deleteRecipesById({ version: this.sdk.version, id })
            .pipe(catchError((error: unknown) => rethrowApiError('Delete recipe error', error)));
    }

    public duplicate(id: string): Observable<Recipe> {
        return this.sdk.client.postRecipesByIdDuplicate({ version: this.sdk.version, id }).pipe(
            map(recipeFromSdk),
            catchError((error: unknown) => rethrowApiError('Duplicate recipe error', error)),
        );
    }
}
