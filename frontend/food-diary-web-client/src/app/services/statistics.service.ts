import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../types/api-response.data';
import { AggregatedStatistics, GetStatisticsDto } from '../types/statistics.data';
import { catchError, Observable, of } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class StatisticsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.statistics;

    public getAggregatedStatistics(params: GetStatisticsDto): Observable<ApiResponse<AggregatedStatistics[]>> {
        return this.get<ApiResponse<AggregatedStatistics[]>>('', { ...params }).pipe(
            catchError(error => {
                return of(ApiResponse.error(error.error?.error, []));
            }),
        );
    }
}
