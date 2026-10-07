import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { WaistEntriesSdk } from '../../../shared/api/sdk/generated/api/waist-entries.service';
import { WeightEntriesSdk } from '../../../shared/api/sdk/generated/api/weight-entries.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';

export type ProfileMeasurementSummary = {
    weightKg: number | null;
    waistCm: number | null;
};

@Service()
export class ProfileMeasurementsService {
    private readonly http = inject(HttpClient);
    private readonly weights = createSdkConnection(WeightEntriesSdk, environment.apiUrls.weights, this.http);
    private readonly waists = createSdkConnection(WaistEntriesSdk, environment.apiUrls.waists, this.http);

    public getLatest(): Observable<ProfileMeasurementSummary> {
        return forkJoin({
            weightKg: this.weights.client
                .getWeightEntriesLatest({ version: this.weights.version })
                .pipe(map(value => sdkOptional(value, entry => entry.weightKg ?? null))),
            waistCm: this.waists.client
                .getWaistEntriesLatest({ version: this.waists.version })
                .pipe(map(value => sdkOptional(value, entry => entry.circumferenceCm ?? null))),
        }).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Profile measurements fetch error', error, {
                    weightKg: null,
                    waistCm: null,
                }),
            ),
        );
    }
}
