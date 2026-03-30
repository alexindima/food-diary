import { Injectable } from '@angular/core';
import { catchError, Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { AggregatedStatistics, GetStatisticsDto } from '../models/statistics.data';

@Injectable({
    providedIn: 'root',
})
export class StatisticsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.statistics;

    public getAggregatedStatistics(params: GetStatisticsDto): Observable<AggregatedStatistics[]> {
        const queryParams = {
            ...params,
            dateFrom: this.toIsoString(params.dateFrom),
            dateTo: this.toIsoString(params.dateTo),
        };

        return this.get<AggregatedStatistics[]>('', queryParams).pipe(catchError(() => of([])));
    }

    private toIsoString(value: Date | string): string {
        if (typeof value === 'string') {
            const parsed = new Date(value);
            return parsed.toISOString();
        }
        return value.toISOString();
    }
}
