import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { type PageOf } from '../../../shared/models/page-of.data';
import { type ExploreFilters, type ExploreRecipe } from '../models/explore.data';

@Injectable({
    providedIn: 'root',
})
export class ExploreService extends ApiService {
    protected readonly baseUrl = `${environment.apiUrls.recipes}/explore`;

    public query(page: number, limit: number, filters?: ExploreFilters): Observable<PageOf<ExploreRecipe>> {
        const params: Record<string, string | number> = { page, limit };

        if (filters?.search?.trim()) {
            params['search'] = filters.search.trim();
        }
        if (filters?.category) {
            params['category'] = filters.category;
        }
        if (filters?.maxPrepTime) {
            params['maxPrepTime'] = filters.maxPrepTime;
        }
        if (filters?.sortBy) {
            params['sortBy'] = filters.sortBy;
        }

        return this.get<PageOf<ExploreRecipe>>('', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Explore recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }
}
