import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { catchError, Observable, of } from 'rxjs';
import { ApiResponse } from '../types/api-response.data';
import { Food, FoodFilters } from '../types/food.data';
import { PageOf } from '../types/page-of.data';

@Injectable({
    providedIn: 'root',
})
export class FoodService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.products;

    public query(page: number, limit: number, filters: FoodFilters): Observable<ApiResponse<PageOf<Food>>> {
        const params = { page, limit, ...filters };
        return this.get<ApiResponse<PageOf<Food>>>('', params).pipe(
            catchError(error => {
                const emptyPage: PageOf<Food> = {
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

    public getById(id: number): Observable<ApiResponse<Food | null>> {
        return this.get<ApiResponse<Food>>(`${id}`).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public create(data: Partial<Food>): Observable<ApiResponse<Food | null>> {
        return this.post<ApiResponse<Food>>('', data).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public update(id: number, data: Partial<Food>): Observable<ApiResponse<Food | null>> {
        return this.patch<ApiResponse<Food>>(`${id}`, data).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }

    public deleteById(id: number): Observable<ApiResponse<null>> {
        return this.delete<ApiResponse<null>>(`${id}`).pipe(catchError(error => of(ApiResponse.error(error.error?.error, null))));
    }
}
