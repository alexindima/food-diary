import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { catchError, Observable, of } from 'rxjs';
import { ApiResponse } from '../types/api-response.data';
import { PageOf } from '../types/page-of.data';
import { RecipeDto } from '../types/recipe.data';
import { ApiService } from './api.service';

@Injectable({
    providedIn: 'root',
})
export class RecipeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public query(page: number, limit: number, filters: Record<string, any> = {}): Observable<ApiResponse<PageOf<RecipeDto>>> {
        const params = { page, limit, ...filters };
        return this.get<ApiResponse<PageOf<RecipeDto>>>('', params).pipe(
            catchError(error => {
                const emptyPage: PageOf<RecipeDto> = {
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

    public getById(id: number): Observable<ApiResponse<RecipeDto | null>> {
        return this.get<ApiResponse<RecipeDto>>(`${id}`).pipe(
            catchError(error => of(ApiResponse.error(error.error?.error, null)))
        );
    }

    public create(data: RecipeDto): Observable<RecipeDto | null> {
        return this.post<RecipeDto>('', data).pipe(
            catchError(() => of(null))
        );
    }

    public update(id: number, data: Partial<RecipeDto>): Observable<ApiResponse<RecipeDto | null>> {
        return this.patch<ApiResponse<RecipeDto>>(`${id}`, data).pipe(
            catchError(error => of(ApiResponse.error(error.error?.error, null)))
        );
    }

    public deleteById(id: number): Observable<ApiResponse<null>> {
        return this.delete<ApiResponse<null>>(`${id}`).pipe(
            catchError(error => of(ApiResponse.error(error.error?.error, null)))
        );
    }
}
