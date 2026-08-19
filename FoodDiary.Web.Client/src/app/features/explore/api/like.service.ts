import { HttpHeaders } from '@angular/common/http';
import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { RecipeLikeStatus } from '../models/like.data';

@Service()
export class LikeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public getStatus(recipeId: string): Observable<RecipeLikeStatus> {
        return this.get<RecipeLikeStatus>(`${recipeId}/likes`).pipe(
            catchError((error: unknown) => fallbackApiError('Get like status error', error, { isLiked: false, totalLikes: 0 })),
        );
    }

    public toggle(recipeId: string, isLiked: boolean): Observable<RecipeLikeStatus> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.post<RecipeLikeStatus>(`${recipeId}/likes/toggle`, { isLiked }, headers).pipe(
            catchError((error: unknown) => rethrowApiError('Toggle like error', error)),
        );
    }
}
