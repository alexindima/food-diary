import { inject, Service, signal } from '@angular/core';

import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { UserService } from '../../shared/api/user.service';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';

@Service()
export class SidebarFacade {
    private readonly userService = inject(UserService);
    private readonly dashboardService = inject(DashboardService);
    private readonly invalidation = inject(NutritionDataInvalidationService);

    public readonly currentUser = this.userService.user;
    public readonly dailyConsumedKcal = signal(0);
    public readonly dailyGoalKcal = signal(0);
    public readonly dailyProgressInvalidationVersion = this.invalidation.dashboardVersion;

    public syncCurrentUser(isAuthenticated: boolean): void {
        if (!isAuthenticated) {
            this.userService.clearUser();
            return;
        }

        if (this.currentUser() === null) {
            this.userService.getInfoSilently().subscribe();
        }
    }

    public syncDailyProgress(date = new Date()): void {
        this.dashboardService.getSnapshotSilently({ date, page: 1, pageSize: 1 }).subscribe(snapshot => {
            this.dailyConsumedKcal.set(snapshot?.statistics.totalCalories ?? 0);
            this.dailyGoalKcal.set(snapshot?.dailyGoal ?? 0);
        });
    }
}
