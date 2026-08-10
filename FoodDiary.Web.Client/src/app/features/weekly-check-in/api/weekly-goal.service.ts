import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';

@Service()
export class WeeklyGoalService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weeklyGoals;

    public getGoal(weekStart: string): Observable<WeeklyGoal | null> {
        return super
            .get<WeeklyGoal | null>('', { weekStart })
            .pipe(catchError((error: unknown) => fallbackApiError('Get weekly goal error', error, null)));
    }

    public upsertGoal(payload: UpsertWeeklyGoalPayload): Observable<WeeklyGoal> {
        return super.put<WeeklyGoal>('', payload).pipe(catchError((error: unknown) => rethrowApiError('Upsert weekly goal error', error)));
    }
}
