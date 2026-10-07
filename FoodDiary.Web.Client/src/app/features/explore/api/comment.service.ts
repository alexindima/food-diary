import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { RecipesSdk } from '../../../shared/api/sdk/generated/api/recipes.service';
import type { RecipeCommentHttpResponse } from '../../../shared/api/sdk/generated/model/recipe-comment-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields, sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { CreateCommentDto, RecipeComment, UpdateCommentDto } from '../models/comment.data';

@Service()
export class CommentService {
    protected readonly baseUrl = environment.apiUrls.recipes;
    private readonly sdk = createSdkConnection(RecipesSdk, this.baseUrl, inject(HttpClient));

    public getComments(recipeId: string, page: number, limit: number): Observable<PageOf<RecipeComment>> {
        return this.sdk.client.getRecipesByRecipeIdComments({ version: this.sdk.version, recipeId, page, limit }).pipe(
            map(value => sdkPage(value, commentFromSdk)),
            catchError((error: unknown) =>
                fallbackApiError('Get comments error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public createComment(recipeId: string, dto: CreateCommentDto): Observable<RecipeComment> {
        return this.sdk.client
            .postRecipesByRecipeIdComments({ version: this.sdk.version, recipeId, createRecipeCommentHttpRequest: dto })
            .pipe(
                map(commentFromSdk),
                catchError((error: unknown) => rethrowApiError('Create comment error', error)),
            );
    }

    public updateComment(recipeId: string, commentId: string, dto: UpdateCommentDto): Observable<RecipeComment> {
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

    public deleteComment(recipeId: string, commentId: string): Observable<void> {
        return this.sdk.client
            .deleteRecipesByRecipeIdCommentsByCommentId({ version: this.sdk.version, recipeId, commentId })
            .pipe(catchError((error: unknown) => rethrowApiError('Delete comment error', error)));
    }
}

function commentFromSdk(value: RecipeCommentHttpResponse): RecipeComment {
    return requireSdkFields(value, ['id', 'recipeId', 'authorId', 'text', 'createdAtUtc', 'isOwnedByCurrentUser']);
}
