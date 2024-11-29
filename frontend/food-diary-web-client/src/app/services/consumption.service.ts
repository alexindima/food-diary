import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { Consumption, ConsumptionManageDto, ConsumptionFilters } from '../types/consumption.data';
import { catchError, Observable, of } from 'rxjs';
import { ApiResponse } from '../types/api-response.data';
import { PageOf } from '../types/page-of.data';

@Injectable({
    providedIn: 'root',
})
export class ConsumptionService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.consumptions;

    public query(page: number, limit: number, filters: ConsumptionFilters): Observable<ApiResponse<PageOf<Consumption>>> {
        const params = { page, limit, ...filters };
        return this.get<ApiResponse<PageOf<Consumption>>>('', params).pipe(
            catchError(error => {
                const emptyPage: PageOf<Consumption> = {
                    data: [],
                    page,
                    limit,
                    totalPages: 0,
                    totalItems: 0,
                };
                return of(ApiResponse.error(error.error?.error, emptyPage));
            }),
        );
    }

    public getById(id: number): Observable<ApiResponse<Consumption | null>> {
        return this.get<ApiResponse<Consumption>>(`${id}`).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public create(data: ConsumptionManageDto): Observable<ApiResponse<Consumption | null>> {
        return this.post<ApiResponse<Consumption>>('', data).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public update(id: number, data: Partial<ConsumptionManageDto>): Observable<ApiResponse<Consumption | null>> {
        return this.patch<ApiResponse<Consumption>>(`${id}`, data).pipe(
            catchError(error => of(ApiResponse.error(error.error?.error, null))),
        );
    }

    public deleteById(id: number): Observable<ApiResponse<null>> {
        return this.delete<ApiResponse<null>>(`${id}`).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }
}
