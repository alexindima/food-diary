import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { RecipeLikeStatus } from '../models/like.data';

@Injectable({
    providedIn: 'root',
})
export class LikeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public getStatus(recipeId: string): Observable<RecipeLikeStatus> {
        return this.get<RecipeLikeStatus>(`${recipeId}/likes`).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get like status error', error, { isLiked: false, totalLikes: 0 })),
        );
    }

    public toggle(recipeId: string): Observable<RecipeLikeStatus> {
        return this.post<RecipeLikeStatus>(`${recipeId}/likes/toggle`, {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Toggle like error', error)),
        );
    }
}
