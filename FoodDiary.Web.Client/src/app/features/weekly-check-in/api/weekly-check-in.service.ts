import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';

@Service()
export class WeeklyCheckInService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weeklyCheckIn;

    public getData(weekStart: string): Observable<WeeklyCheckInData> {
        return super
            .get<WeeklyCheckInData>('', { weekStart })
            .pipe(catchError((error: unknown) => rethrowApiError('Get weekly check-in error', error)));
    }
}
