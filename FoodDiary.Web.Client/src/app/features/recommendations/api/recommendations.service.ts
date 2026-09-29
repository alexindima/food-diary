import { Service } from '@angular/core';
import { EMPTY, expand, reduce, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type {
    CreateRecommendationCommentRequest,
    DietologistRecommendation,
    RecommendationComment,
} from '../../../shared/models/dietologist.data';
import type { PageOf } from '../../../shared/models/page-of.data';

@Service()
export class RecommendationsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recommendations;

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.get<DietologistRecommendation[]>('');
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.put<void>(`${recommendationId}/read`, {});
    }

    public getComments(recommendationId: string): Observable<RecommendationComment[]> {
        return this.get<PageOf<RecommendationComment>>(`${recommendationId}/comments`, { page: 1, limit: 50 }).pipe(
            expand(response => (response.page < response.totalPages
                ? this.get<PageOf<RecommendationComment>>(`${recommendationId}/comments`, { page: response.page + 1, limit: 50 })
                : EMPTY)),
            reduce((comments, response) => [...comments, ...response.data], [] as RecommendationComment[]),
        );
    }

    public createComment(recommendationId: string, request: CreateRecommendationCommentRequest): Observable<RecommendationComment> {
        return this.post<RecommendationComment>(`${recommendationId}/comments`, request);
    }
}
