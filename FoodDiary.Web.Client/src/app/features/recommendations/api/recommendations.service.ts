import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type {
    CreateRecommendationCommentRequest,
    DietologistRecommendation,
    RecommendationComment,
} from '../../../shared/models/dietologist.data';

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
        return this.get<RecommendationComment[]>(`${recommendationId}/comments`);
    }

    public createComment(recommendationId: string, request: CreateRecommendationCommentRequest): Observable<RecommendationComment> {
        return this.post<RecommendationComment>(`${recommendationId}/comments`, request);
    }
}
