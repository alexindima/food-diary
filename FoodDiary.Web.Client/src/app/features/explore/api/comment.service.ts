import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { RecipeCommentId, RecipeId } from '../../../shared/models/semantics/entity-id';
import type { CreateCommentDto, RecipeComment, UpdateCommentDto } from '../models/comment.data';
import { commentFromSdk } from './community-sdk.mapper';

@Service()
export class CommentService {
    protected readonly baseUrl = environment.apiUrls.recipes;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public getComments(recipeId: RecipeId, page: number, limit: number): Observable<PageOf<RecipeComment>> {
        return this.sdk.client.getRecipesByRecipeIdComments({ version: this.sdk.version, recipeId, page, limit }).pipe(
            map(value => sdkPage(value, commentFromSdk)),
            catchError((error: unknown) =>
                fallbackApiError('Get comments error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public createComment(recipeId: RecipeId, dto: CreateCommentDto): Observable<RecipeComment> {
        return this.sdk.client
            .postRecipesByRecipeIdComments({ version: this.sdk.version, recipeId, createRecipeCommentHttpRequest: dto })
            .pipe(
                map(commentFromSdk),
                catchError((error: unknown) => rethrowApiError('Create comment error', error)),
            );
    }

    public updateComment(recipeId: RecipeId, commentId: RecipeCommentId, dto: UpdateCommentDto): Observable<RecipeComment> {
        return this.sdk.client
            .patchRecipesByRecipeIdCommentsByCommentId({
                version: this.sdk.version,
                recipeId,
                commentId,
                updateRecipeCommentHttpRequest: dto,
            })
            .pipe(
                map(commentFromSdk),
                catchError((error: unknown) => rethrowApiError('Update comment error', error)),
            );
    }

    public deleteComment(recipeId: RecipeId, commentId: RecipeCommentId): Observable<void> {
        return this.sdk.client
            .deleteRecipesByRecipeIdCommentsByCommentId({ version: this.sdk.version, recipeId, commentId })
            .pipe(catchError((error: unknown) => rethrowApiError('Delete comment error', error)));
    }
}
