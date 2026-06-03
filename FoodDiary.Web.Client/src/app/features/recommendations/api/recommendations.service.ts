import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type { DietologistRecommendation } from '../../../shared/models/dietologist.data';

@Service()
export class RecommendationsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recommendations;

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.get<DietologistRecommendation[]>('');
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.put<void>(`${recommendationId}/read`, {});
    }
}
