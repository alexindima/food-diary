import { inject, Service } from '@angular/core';
import { catchError, concatMap, map, type Observable, of } from 'rxjs';

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
            catchError((error: unknown) => fallbackApiError('Update goals error', error, null)),
            concatMap(goals =>
                goals === null
                    ? of(null)
                    : this.clearBodyTargets(request, goals).pipe(
                          concatMap(updated => this.userService.getInfoSilently().pipe(map(() => updated))),
                      ),
            ),
        );
    }

    private clearBodyTargets(request: UpdateGoalsRequest, goals: GoalsResponse): Observable<GoalsResponse> {
        return this.clearWeightTarget(request, goals).pipe(concatMap(updated => this.clearWaistTarget(request, updated)));
    }

    private clearWeightTarget(request: UpdateGoalsRequest, goals: GoalsResponse): Observable<GoalsResponse> {
        if (request.desiredWeightKg !== null || goals.desiredWeightKg === null) {
            return of(goals);
        }
        return this.userService.updateWeightGoal(null).pipe(map(result => ({ ...goals, desiredWeightKg: result.desiredWeightKg })));
    }

    private clearWaistTarget(request: UpdateGoalsRequest, goals: GoalsResponse): Observable<GoalsResponse> {
        if (request.desiredWaistCm !== null || goals.desiredWaistCm === null) {
            return of(goals);
        }
        return this.userService.updateWaistGoal(null).pipe(map(result => ({ ...goals, desiredWaistCm: result.desiredWaistCm })));
    }
}
