import { inject, Service, signal, untracked } from '@angular/core';
import { rxResource, takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { type FdUiToastRef, FdUiToastService } from 'fd-ui-kit';
import { merge, skip, tap } from 'rxjs';

import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { getDashboardDateUtc } from '../../features/dashboard/contracts/dashboard-date';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../shared/api/user.service';
import { SessionEventsService } from '../../shared/auth/session-events.service';
import { MEAL_FEEDBACK_TOAST_KEY } from '../../shared/notifications/meal-feedback.config';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';

@Service()
export class SidebarFacade {
    private readonly userService = inject(UserService);
    private readonly dashboardService = inject(DashboardService);
    private readonly authService = inject(AuthService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly progressRequest = signal<
        | {
              owner: ReturnType<AuthService['getUserId']>;
              query: Parameters<DashboardService['getSnapshotSilently']>[0];
          }
        | undefined
    >(undefined);
    private readonly progressResource = rxResource({
        params: this.progressRequest,
        stream: ({ params, abortSignal }) =>
            this.dashboardService.getSnapshotSilently(params.query).pipe(
                tap(snapshot => {
                    if (
                        snapshot !== null &&
                        !abortSignal.aborted &&
                        this.progressRequest() === params &&
                        this.authService.isAuthenticated() &&
                        this.authService.getUserId() === params.owner
                    ) {
                        this.showProgress(snapshot.statistics.totalCalories, snapshot.dailyGoal);
                    }
                }),
            ),
    });
    private readonly currentUserRequest = signal<object | undefined>(undefined);
    private readonly currentUserResource = rxResource({
        params: this.currentUserRequest,
        stream: () => this.userService.getInfoSilently(),
    });
    private progressToast: FdUiToastRef | null = null;

    public readonly currentUser = this.userService.user;

    public constructor() {
        merge(this.sessionEvents.authenticated$, this.sessionEvents.localSessionInvalidating$, this.sessionEvents.sessionEnded$)
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.resetRequests();
            });
        toObservable(this.invalidation.mealsVersion)
            .pipe(skip(1), takeUntilDestroyed())
            .subscribe(() => {
                this.progressResource.set(undefined);
                if (this.authService.isAuthenticated()) {
                    const date = new Date();
                    this.progressRequest.set({
                        owner: this.authService.getUserId(),
                        query: {
                            date: getDashboardDateUtc(date),
                            timeZoneOffsetMinutes: -date.getTimezoneOffset(),
                            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                            page: 1,
                            pageSize: 1,
                        },
                    });
                }
            });
    }

    public syncCurrentUser(isAuthenticated: boolean): void {
        if (!isAuthenticated) {
            this.userService.clearUser();
            this.resetRequests();
            return;
        }

        if (this.currentUser() === null && !untracked(this.currentUserResource.isLoading)) {
            this.currentUserRequest.set({});
        }
    }

    private showProgress(totalCalories: number, dailyGoal: number): void {
        const consumed = Math.round(totalCalories);
        const goal = Math.round(dailyGoal);
        const label = this.translateService.instant('MEAL_PROGRESS.DAILY_GOAL');
        const message = this.translateService.instant(goal > 0 ? 'MEAL_PROGRESS.TODAY_WITH_GOAL' : 'MEAL_PROGRESS.TODAY_TOTAL', {
            consumed,
            goal,
        });
        this.progressToast = this.toastService.success(message, {
            key: MEAL_FEEDBACK_TOAST_KEY,
            duration: 5000,
            progress: goal > 0 ? { value: consumed, max: goal, label } : undefined,
        });
    }

    private resetRequests(): void {
        this.progressRequest.set(undefined);
        this.progressResource.set(undefined);
        this.currentUserRequest.set(undefined);
        this.currentUserResource.set(undefined);
        this.progressToast?.dismiss();
        this.progressToast = null;
    }
}
