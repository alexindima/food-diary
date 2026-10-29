import { inject, Service } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { type FdUiToastRef, FdUiToastService } from 'fd-ui-kit';
import { EMPTY, map, merge, skip, switchMap, tap } from 'rxjs';

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
    private progressToast: FdUiToastRef | null = null;

    public readonly currentUser = this.userService.user;

    public constructor() {
        const sessionChanges = merge(
            this.sessionEvents.authenticated$,
            this.sessionEvents.localSessionInvalidating$,
            this.sessionEvents.sessionEnded$,
        ).pipe(
            tap(() => {
                this.dismissProgress();
            }),
            map(() => false),
        );
        merge(
            toObservable(this.invalidation.mealsVersion).pipe(
                skip(1),
                map(() => true),
            ),
            sessionChanges,
        )
            .pipe(
                switchMap(showProgress => {
                    if (!showProgress || !this.authService.isAuthenticated()) {
                        return EMPTY;
                    }
                    const owner = this.authService.getUserId();
                    const date = new Date();
                    return this.dashboardService
                        .getSnapshotSilently({
                            date: getDashboardDateUtc(date),
                            timeZoneOffsetMinutes: -date.getTimezoneOffset(),
                            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                            page: 1,
                            pageSize: 1,
                        })
                        .pipe(map(snapshot => ({ snapshot, owner })));
                }),
                takeUntilDestroyed(),
            )
            .subscribe(({ snapshot, owner }) => {
                if (snapshot === null || !this.authService.isAuthenticated() || owner !== this.authService.getUserId()) {
                    return;
                }
                const consumed = Math.round(snapshot.statistics.totalCalories);
                const goal = Math.round(snapshot.dailyGoal);
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
            });
    }

    public syncCurrentUser(isAuthenticated: boolean): void {
        if (!isAuthenticated) {
            this.userService.clearUser();
            this.dismissProgress();
            return;
        }

        if (this.currentUser() === null) {
            this.userService.getInfoSilently().subscribe();
        }
    }

    private dismissProgress(): void {
        this.progressToast?.dismiss();
        this.progressToast = null;
    }
}
