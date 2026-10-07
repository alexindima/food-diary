import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { WeeklyCheckInSdk } from '../../../shared/api/sdk/generated/api/weekly-check-in.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import { weeklyCheckInFromSdk } from './weekly-sdk.mapper';

@Service()
export class WeeklyCheckInService {
    protected readonly baseUrl = environment.apiUrls.weeklyCheckIn;
    private readonly sdk = createSdkConnection(WeeklyCheckInSdk, this.baseUrl, inject(HttpClient));

    public getData(weekStart: string): Observable<WeeklyCheckInData> {
        return this.sdk.client.getWeeklyCheckIn({ version: this.sdk.version, weekStart }).pipe(
            map(weeklyCheckInFromSdk),
            catchError((error: unknown) => rethrowApiError('Get weekly check-in error', error)),
        );
    }
}
