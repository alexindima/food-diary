import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { RecipeId } from '../../../shared/models/semantics/entity-id';
import type { PublicRecipe, PublicRecipeFilters } from '../models/public-recipe.data';
import { PUBLIC_RECIPE_API_URL } from './public-recipe-api.token';
import { publicRecipeFromSdk } from './public-recipe-sdk.mapper';

@Service()
export class PublicRecipeService {
    protected readonly baseUrl = inject(PUBLIC_RECIPE_API_URL);
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public query(filters: PublicRecipeFilters): Observable<PageOf<PublicRecipe>> {
        return this.sdk.client
            .getRecipesPublic({ version: this.sdk.version, ...filters, limit: 20 })
            .pipe(map(value => sdkPage(value, publicRecipeFromSdk)));
    }

    public getCategories(search: string, language?: string): Observable<string[]> {
        return this.sdk.client.getRecipesPublicCategories({ version: this.sdk.version, search, language });
    }

    public getById(id: RecipeId): Observable<PublicRecipe> {
        return this.sdk.client.getRecipesPublicById({ version: this.sdk.version, id }).pipe(map(publicRecipeFromSdk));
    }
}
