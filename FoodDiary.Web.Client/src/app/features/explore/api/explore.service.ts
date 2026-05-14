import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { addOptionalNumberParam, addOptionalStringParam, type ApiQueryParams } from '../../../shared/lib/api-query-params.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ExploreFilters, ExploreRecipe } from '../models/explore.data';

@Injectable({ providedIn: 'root' })
export class ExploreService extends ApiService {
    protected readonly baseUrl = `${environment.apiUrls.recipes}/explore`;

    public query(page: number, limit: number, filters?: ExploreFilters): Observable<PageOf<ExploreRecipe>> {
        const params: ApiQueryParams = { page, limit };

        const search = filters?.search?.trim();
        addOptionalStringParam(params, 'search', search);
        addOptionalStringParam(params, 'category', filters?.category);
        addOptionalNumberParam(params, 'maxPrepTime', filters?.maxPrepTime);
        addOptionalStringParam(params, 'sortBy', filters?.sortBy);

        return this.get<PageOf<ExploreRecipe>>('', params).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Explore recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }
}
