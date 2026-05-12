import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { GamificationData } from '../models/gamification.data';

@Injectable({
    providedIn: 'root',
})
export class GamificationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.gamification;

    public getData(): Observable<GamificationData> {
        return super.get<GamificationData>('').pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get gamification error', error, {
                    currentStreak: 0,
                    longestStreak: 0,
                    totalMealsLogged: 0,
                    healthScore: 0,
                    weeklyAdherence: 0,
                    badges: [],
                }),
            ),
        );
    }
}
