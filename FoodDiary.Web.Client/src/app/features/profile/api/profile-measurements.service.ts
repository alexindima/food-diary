import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';

export type ProfileMeasurementSummary = {
    weightKg: number | null;
    waistCm: number | null;
};

type LatestWeightResponse = {
    weightKg: number;
};

type LatestWaistResponse = {
    circumferenceCm: number;
};

@Service()
export class ProfileMeasurementsService {
    private readonly http = inject(HttpClient);

    public getLatest(): Observable<ProfileMeasurementSummary> {
        return forkJoin({
            weightKg: this.http.get<LatestWeightResponse | null>(`${environment.apiUrls.weights}/latest`),
            waistCm: this.http.get<LatestWaistResponse | null>(`${environment.apiUrls.waists}/latest`),
        }).pipe(
            map(({ weightKg, waistCm }) => ({
                weightKg: weightKg?.weightKg ?? null,
                waistCm: waistCm?.circumferenceCm ?? null,
            })),
            catchError((error: unknown) =>
                fallbackApiError('Profile measurements fetch error', error, {
                    weightKg: null,
                    waistCm: null,
                }),
            ),
        );
    }
}
