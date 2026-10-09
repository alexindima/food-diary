import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { WeeklyGoalsSdk } from '../../../shared/api/sdk/generated/api/weekly-goals.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { CalendarDate } from '../../../shared/models/semantics/date-value';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';
import { weeklyGoalFromSdk } from './weekly-sdk.mapper';

@Service()
export class WeeklyGoalService {
    protected readonly baseUrl = environment.apiUrls.weeklyGoals;
    private readonly sdk = createSdkConnection(WeeklyGoalsSdk, this.baseUrl, inject(HttpClient));

    public getGoal(weekStart: CalendarDate): Observable<WeeklyGoal | null> {
        return this.sdk.client.getWeeklyGoals({ version: this.sdk.version, weekStart }).pipe(
            map(value => sdkOptional(value, weeklyGoalFromSdk)),
            catchError((error: unknown) => rethrowApiError('Get weekly goal error', error)),
        );
    }

    public upsertGoal(payload: UpsertWeeklyGoalPayload): Observable<WeeklyGoal> {
        return this.sdk.client.putWeeklyGoals({ version: this.sdk.version, upsertWeeklyGoalHttpRequest: payload }).pipe(
            map(weeklyGoalFromSdk),
            catchError((error: unknown) => rethrowApiError('Upsert weekly goal error', error)),
        );
    }
}
