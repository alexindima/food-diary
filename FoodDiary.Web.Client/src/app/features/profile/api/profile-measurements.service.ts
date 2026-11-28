import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { WaistEntriesSdk } from '../../../shared/api/sdk/generated/api/waist-entries.service';
import { WeightEntriesSdk } from '../../../shared/api/sdk/generated/api/weight-entries.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';

@Service()
export class ProfileMeasurementsService {
    private readonly http = inject(HttpClient);
    private readonly weights = createSdkConnection(WeightEntriesSdk, environment.apiUrls.weights, this.http);
    private readonly waists = createSdkConnection(WaistEntriesSdk, environment.apiUrls.waists, this.http);

    public getLatestWeight(): Observable<number | null> {
        return this.weights.client.getWeightEntriesLatest({ version: this.weights.version }).pipe(
            map(value => sdkOptional(value, entry => entry.weightKg ?? null)),
            catchError((error: unknown) => rethrowApiError('Profile weight fetch error', error)),
        );
    }

    public getLatestWaist(): Observable<number | null> {
        return this.waists.client.getWaistEntriesLatest({ version: this.waists.version }).pipe(
            map(value => sdkOptional(value, entry => entry.circumferenceCm ?? null)),
            catchError((error: unknown) => rethrowApiError('Profile waist fetch error', error)),
        );
    }
}
