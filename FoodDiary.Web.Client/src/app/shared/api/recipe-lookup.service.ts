import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiService } from '../../services/api.service';
import { rethrowApiError } from '../lib/api-error.utils';
import { RecipeLookup } from '../models/recipe-lookup.data';

@Injectable({
    providedIn: 'root',
})
export class RecipeLookupService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public getById(id: string, includePublic = true): Observable<RecipeLookup> {
        const params = { includePublic };
        return this.get<RecipeLookup>(`${id}`, params).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Get recipe lookup error', error)),
        );
    }
}
