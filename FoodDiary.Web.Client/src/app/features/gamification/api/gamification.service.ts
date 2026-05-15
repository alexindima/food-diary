import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { createDefaultGamificationData } from '../lib/gamification.constants';
import type { GamificationData } from '../models/gamification.data';

@Injectable({ providedIn: 'root' })
export class GamificationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.gamification;

    public getData(): Observable<GamificationData> {
        return super
            .get<GamificationData>('')
            .pipe(catchError((error: unknown) => fallbackApiError('Get gamification error', error, createDefaultGamificationData())));
    }
}
