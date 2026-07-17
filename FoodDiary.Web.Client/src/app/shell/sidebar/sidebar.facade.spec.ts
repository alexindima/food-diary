import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { UserService } from '../../shared/api/user.service';
import { SidebarFacade } from './sidebar.facade';

const CONSUMED_KCAL = 720;
const GOAL_KCAL = 1900;

describe('SidebarFacade', () => {
    const user = signal<{ id: string } | null>(null);
    const userService = {
        user,
        clearUser: vi.fn(() => {
            user.set(null);
        }),
        getInfoSilently: vi.fn(() => of(null)),
    };
    const dashboardService = {
        getSnapshotSilently: vi.fn(() =>
            of({
                statistics: { totalCalories: CONSUMED_KCAL },
                dailyGoal: GOAL_KCAL,
            }),
        ),
    };
    let facade: SidebarFacade;

    beforeEach(() => {
        user.set(null);
        vi.clearAllMocks();
        TestBed.configureTestingModule({
            providers: [
                SidebarFacade,
                { provide: UserService, useValue: userService },
                { provide: DashboardService, useValue: dashboardService },
            ],
        });
        facade = TestBed.inject(SidebarFacade);
    });

    it('loads the current user only when authentication exists and the user is missing', () => {
        facade.syncCurrentUser(true);
        user.set({ id: 'user-1' });
        facade.syncCurrentUser(true);

        expect(userService.getInfoSilently).toHaveBeenCalledTimes(1);
        expect(userService.clearUser).not.toHaveBeenCalled();
    });

    it('clears user state for an unauthenticated session', () => {
        user.set({ id: 'user-1' });

        facade.syncCurrentUser(false);

        expect(userService.clearUser).toHaveBeenCalledTimes(1);
        expect(facade.currentUser()).toBeNull();
    });

    it('maps the dashboard snapshot to sidebar progress signals', () => {
        const date = new Date('2026-07-17T00:00:00Z');

        facade.syncDailyProgress(date);

        expect(dashboardService.getSnapshotSilently).toHaveBeenCalledWith({ date, page: 1, pageSize: 1 });
        expect(facade.dailyConsumedKcal()).toBe(CONSUMED_KCAL);
        expect(facade.dailyGoalKcal()).toBe(GOAL_KCAL);
    });
});
