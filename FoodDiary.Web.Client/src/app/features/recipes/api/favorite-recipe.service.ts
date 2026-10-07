import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { favoriteRecipeFromSdk } from '../../../shared/api/sdk/favorite-sdk.mapper';
import { FavoriteRecipesSdk } from '../../../shared/api/sdk/generated/api/favorite-recipes.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { FavoriteRecipe } from '../../../shared/models/recipe.data';

const FAVORITE_PAGE_SIZE = 10;

const LOOKUP_PAGE_SIZE = 100;

@Service()
export class FavoriteRecipeService {
    protected readonly baseUrl = environment.apiUrls.favoriteRecipes;
    private readonly sdk = createSdkConnection(FavoriteRecipesSdk, this.baseUrl, inject(HttpClient));

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteRecipe>> {
        return this.sdk.client.getFavoriteRecipesPage({ version: this.sdk.version, page, limit, search: search.trim() }).pipe(
            map(value => sdkPage(value, favoriteRecipeFromSdk)),
            catchError((error: unknown) => rethrowApiError('Get favorite recipe page error', error)),
        );
    }

    public getLookupPage(): Observable<FavoriteRecipe[]> {
        return this.getPage(1, LOOKUP_PAGE_SIZE).pipe(
            expand(page => (page.page < page.totalPages ? this.getPage(page.page + 1, LOOKUP_PAGE_SIZE) : EMPTY)),
            reduce((items, page) => [...items, ...page.data], [] as FavoriteRecipe[]),
            catchError((error: unknown) => fallbackApiError('Get favorite recipes error', error, [])),
        );
    }

    public isFavorite(recipeId: string): Observable<boolean> {
        return this.sdk.client
            .getFavoriteRecipesCheckByRecipeId({ version: this.sdk.version, recipeId })
            .pipe(catchError((error: unknown) => fallbackApiError('Check favorite recipe error', error, false)));
    }

    public add(recipeId: string, name?: string): Observable<FavoriteRecipe> {
        return this.sdk.client.postFavoriteRecipes({ version: this.sdk.version, addFavoriteRecipeHttpRequest: { recipeId, name } }).pipe(
            map(favoriteRecipeFromSdk),
            catchError((error: unknown) => rethrowApiError('Add favorite recipe error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.sdk.client.deleteFavoriteRecipesById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Remove favorite recipe error', error)),
        );
    }
}
