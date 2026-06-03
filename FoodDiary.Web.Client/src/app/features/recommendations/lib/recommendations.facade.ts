import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { DietologistRecommendation } from '../../../shared/models/dietologist.data';
import { RecommendationsService } from '../api/recommendations.service';

@Service()
export class RecommendationsFacade {
    private readonly recommendationsService = inject(RecommendationsService);

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.recommendationsService.getMyRecommendations();
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.recommendationsService.markAsRead(recommendationId);
    }
}
