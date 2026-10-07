import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { rethrowApiError } from '../lib/api-error.utils';
import type { RecipeLookup } from '../models/recipe-lookup.data';
import { RecipesSdk } from './sdk/generated/api/recipes.service';
import { recipeLookupFromSdk } from './sdk/recipe-sdk.mapper';
import { createSdkConnection } from './sdk/sdk-connection';

@Service()
export class RecipeLookupService {
    protected readonly baseUrl = environment.apiUrls.recipes;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public getById(id: string, includePublic = true): Observable<RecipeLookup> {
        return this.sdk.client.getRecipesById({ version: this.sdk.version, id, includePublic }).pipe(
            map(recipeLookupFromSdk),
            catchError((error: unknown) => rethrowApiError('Get recipe lookup error', error)),
        );
    }
}
