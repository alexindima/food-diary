import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { RecipeLikeStatus } from '../models/like.data';

@Service()
export class LikeService {
    protected readonly baseUrl = environment.apiUrls.recipes;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public getStatus(recipeId: string): Observable<RecipeLikeStatus> {
        return this.sdk.client.getRecipesByRecipeIdLikes({ version: this.sdk.version, recipeId }).pipe(
            map(response => requireSdkFields(response, ['isLiked', 'totalLikes'])),
            catchError((error: unknown) => fallbackApiError('Get like status error', error, { isLiked: false, totalLikes: 0 })),
        );
    }

    public toggle(recipeId: string, isLiked: boolean): Observable<RecipeLikeStatus> {
        return this.sdk.client
            .postRecipesByRecipeIdLikesToggle({
                version: this.sdk.version,
                recipeId,
                idempotencyKey: crypto.randomUUID(),
                setRecipeLikeStateHttpRequest: { isLiked },
            })
            .pipe(
                map(response => requireSdkFields(response, ['isLiked', 'totalLikes'])),
                catchError((error: unknown) => rethrowApiError('Toggle like error', error)),
            );
    }
}
