import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { type TdeeInsight } from '../models/tdee-insight.data';

@Injectable({
    providedIn: 'root',
})
export class TdeeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.tdee;

    public getInsight(): Observable<TdeeInsight | null> {
        return super
            .get<TdeeInsight>('')
            .pipe(catchError((error: HttpErrorResponse) => fallbackApiError('TDEE insight fetch error', error, null)));
    }
}
