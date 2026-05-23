import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type { DietologistRecommendation } from '../../../shared/models/dietologist.data';

@Injectable({ providedIn: 'root' })
export class RecommendationsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.auth.replace('/auth', '/recommendations');

    public getMyRecommendations(): Observable<DietologistRecommendation[]> {
        return this.get<DietologistRecommendation[]>('');
    }

    public markAsRead(recommendationId: string): Observable<void> {
        return this.put<void>(`${recommendationId}/read`, {});
    }
}
