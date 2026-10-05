import { inject, Service } from '@angular/core';
import { catchError, concatMap, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { UserService } from '../../../shared/api/user.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';

@Service()
export class GoalsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.goals;
    private readonly userService = inject(UserService);

    public getGoals(): Observable<GoalsResponse | null> {
        return this.get<GoalsResponse>('').pipe(catchError((error: unknown) => fallbackApiError('Get goals error', error, null)));
    }

    public getGoalsStrict(): Observable<GoalsResponse> {
        return this.get<GoalsResponse>('').pipe(catchError((error: unknown) => rethrowApiError('Get goals error', error)));
    }

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.patch<GoalsResponse>('', request).pipe(
            concatMap(goals => this.userService.getInfoSilently().pipe(map(() => goals))),
            catchError((error: unknown) => fallbackApiError('Update goals error', error, null)),
        );
    }
}
