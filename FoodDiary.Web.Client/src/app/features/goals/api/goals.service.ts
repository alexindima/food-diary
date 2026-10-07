import { inject, Service } from '@angular/core';
import { catchError, concatMap, map, type Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { GoalsSdk } from '../../../shared/api/sdk/generated/api/goals.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { UserService } from '../../../shared/api/user.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';

@Service()
export class GoalsService {
    protected readonly baseUrl = environment.apiUrls.goals;
    private readonly userService = inject(UserService);
    private readonly sdk = createSdkConnection(GoalsSdk, this.baseUrl, inject(HttpClient));

    public getGoals(): Observable<GoalsResponse | null> {
        return this.sdk.client.getGoals({ version: this.sdk.version }).pipe(
            map(response => requireSdkFields(response, ['calorieCyclingEnabled'])),
            catchError((error: unknown) => fallbackApiError('Get goals error', error, null)),
        );
    }

    public getGoalsStrict(): Observable<GoalsResponse> {
        return this.sdk.client.getGoals({ version: this.sdk.version }).pipe(
            map(response => requireSdkFields(response, ['calorieCyclingEnabled'])),
            catchError((error: unknown) => rethrowApiError('Get goals error', error)),
        );
    }

    public updateGoals(request: UpdateGoalsRequest): Observable<GoalsResponse | null> {
        return this.sdk.client.patchGoals({ version: this.sdk.version, updateGoalsHttpRequest: request }).pipe(
            map(response => requireSdkFields(response, ['calorieCyclingEnabled'])),
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
import { HttpClient } from '@angular/common/http';
