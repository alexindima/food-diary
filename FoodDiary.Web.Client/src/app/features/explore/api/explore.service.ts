import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ExploreFilters, ExploreRecipe } from '../models/explore.data';

function addOptionalStringParam(params: Record<string, string | number>, key: string, value: string | undefined): void {
    if (value !== undefined && value.length > 0) {
        params[key] = value;
    }
}

function addOptionalNumberParam(params: Record<string, string | number>, key: string, value: number | undefined): void {
    if (value !== undefined) {
        params[key] = value;
    }
}

@Injectable({
    providedIn: 'root',
})
export class ExploreService extends ApiService {
    protected readonly baseUrl = `${environment.apiUrls.recipes}/explore`;

    public query(page: number, limit: number, filters?: ExploreFilters): Observable<PageOf<ExploreRecipe>> {
        const params: Record<string, string | number> = { page, limit };

        const search = filters?.search?.trim();
        addOptionalStringParam(params, 'search', search);
        addOptionalStringParam(params, 'category', filters?.category);
        addOptionalNumberParam(params, 'maxPrepTime', filters?.maxPrepTime);
        addOptionalStringParam(params, 'sortBy', filters?.sortBy);

        return this.get<PageOf<ExploreRecipe>>('', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Explore recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }
}
