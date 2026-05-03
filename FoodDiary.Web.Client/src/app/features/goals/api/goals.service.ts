import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { type GoalsResponse, type UpdateGoalsRequest } from '../models/goals.data';

@Injectable({
    providedIn: 'root',
})
export class GoalsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.goals;

    public getGoals(): Observable<GoalsResponse | null> {
        return this.get<GoalsResponse>('').pipe(catchError(error => fallbackApiError('Get goals error', error, null)));
    }

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.patch<GoalsResponse>('', request).pipe(catchError(error => fallbackApiError('Update goals error', error, null)));
    }
}
