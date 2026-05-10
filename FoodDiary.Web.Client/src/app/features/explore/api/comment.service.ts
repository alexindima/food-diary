import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { CreateCommentDto, RecipeComment, UpdateCommentDto } from '../models/comment.data';

@Injectable({
    providedIn: 'root',
})
export class CommentService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public getComments(recipeId: string, page: number, limit: number): Observable<PageOf<RecipeComment>> {
        return this.get<PageOf<RecipeComment>>(`${recipeId}/comments`, { page, limit }).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get comments error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public createComment(recipeId: string, dto: CreateCommentDto): Observable<RecipeComment> {
        return this.post<RecipeComment>(`${recipeId}/comments`, dto).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Create comment error', error)),
        );
    }

    public updateComment(recipeId: string, commentId: string, dto: UpdateCommentDto): Observable<RecipeComment> {
        return this.patch<RecipeComment>(`${recipeId}/comments/${commentId}`, dto).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update comment error', error)),
        );
    }

    public deleteComment(recipeId: string, commentId: string): Observable<void> {
        return this.delete<void>(`${recipeId}/comments/${commentId}`).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Delete comment error', error)),
        );
    }
}
