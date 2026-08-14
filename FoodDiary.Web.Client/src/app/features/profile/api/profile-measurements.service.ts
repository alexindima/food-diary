import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';

export type ProfileMeasurementSummary = {
    weight: number | null;
    waist: number | null;
};

type LatestWeightResponse = {
    weight: number;
};

type LatestWaistResponse = {
    circumference: number;
};

@Service()
export class ProfileMeasurementsService {
    private readonly http = inject(HttpClient);

    public getLatest(): Observable<ProfileMeasurementSummary> {
        return forkJoin({
            weight: this.http.get<LatestWeightResponse | null>(`${environment.apiUrls.weights}/latest`),
            waist: this.http.get<LatestWaistResponse | null>(`${environment.apiUrls.waists}/latest`),
        }).pipe(
            map(({ weight, waist }) => ({
                weight: weight?.weight ?? null,
                waist: waist?.circumference ?? null,
            })),
            catchError((error: unknown) =>
                fallbackApiError('Profile measurements fetch error', error, {
                    weight: null,
                    waist: null,
                }),
            ),
        );
    }
}
