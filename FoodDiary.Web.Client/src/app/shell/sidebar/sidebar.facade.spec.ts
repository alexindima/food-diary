import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit';
import { type Observable, of, Subject } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../shared/api/user.service';
import { SessionEventsService } from '../../shared/auth/session-events.service';
import { MEAL_FEEDBACK_TOAST_KEY } from '../../shared/notifications/meal-feedback.config';
import { NutritionDataInvalidationService } from '../../shared/state/nutrition-data-invalidation.service';
import { SidebarFacade } from './sidebar.facade';

const CONSUMED_KCAL = 720;
const GOAL_KCAL = 1900;

const user = signal<{ id: string } | null>(null);
const userService = {
    user,
    clearUser: vi.fn(() => {
        user.set(null);
    }),
    getInfoSilently: vi.fn(() => of(null)),
};
const dashboardService = {
    getSnapshotSilently: vi.fn((): Observable<{ statistics: { totalCalories: number }; dailyGoal: number } | null> =>
        of({
            statistics: { totalCalories: CONSUMED_KCAL },
            dailyGoal: GOAL_KCAL,
        }),
    ),
};
let facade: SidebarFacade;
let invalidation: NutritionDataInvalidationService;
const authenticated = signal(true);
const owner = signal('user-1');
const dismiss = vi.fn();
const toastService = { success: vi.fn(() => ({ dismiss })) };

beforeEach(() => {
    user.set(null);
    authenticated.set(true);
    owner.set('user-1');
    vi.clearAllMocks();
    TestBed.configureTestingModule({
        providers: [
            SidebarFacade,
            { provide: UserService, useValue: userService },
            { provide: DashboardService, useValue: dashboardService },
            { provide: AuthService, useValue: { isAuthenticated: authenticated, getUserId: owner } },
            { provide: FdUiToastService, useValue: toastService },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
        ],
    });
    facade = TestBed.inject(SidebarFacade);
    invalidation = TestBed.inject(NutritionDataInvalidationService);
    TestBed.tick();
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

it('does not fetch progress on startup, goal changes or body metric changes', () => {
    invalidation.reportGoalMutation();
    invalidation.reportBodyMetricMutation();
    TestBed.tick();
    expect(dashboardService.getSnapshotSilently).not.toHaveBeenCalled();
});

it('shows daily progress after a successful meal mutation and replaces earlier feedback', () => {
    invalidation.reportMealMutation();
    TestBed.tick();

    expect(toastService.success).toHaveBeenCalledWith('MEAL_PROGRESS.TODAY_WITH_GOAL', {
        key: MEAL_FEEDBACK_TOAST_KEY,
        duration: 5000,
        progress: { value: CONSUMED_KCAL, max: GOAL_KCAL, label: 'MEAL_PROGRESS.DAILY_GOAL' },
    });
    invalidation.reportMealMutation();
    TestBed.tick();
    expect(dismiss).not.toHaveBeenCalled();
});

it('keeps calorie feedback useful without a configured goal', () => {
    dashboardService.getSnapshotSilently.mockReturnValueOnce(of({ statistics: { totalCalories: CONSUMED_KCAL }, dailyGoal: 0 }));
    invalidation.reportMealMutation();
    TestBed.tick();
    expect(toastService.success).toHaveBeenCalledWith('MEAL_PROGRESS.TODAY_TOTAL', { key: MEAL_FEEDBACK_TOAST_KEY, duration: 5000, progress: undefined });
});

it('cancels stale requests after another mutation or a session change', () => {
    const stale = new Subject<{ statistics: { totalCalories: number }; dailyGoal: number }>();
    dashboardService.getSnapshotSilently.mockReturnValueOnce(stale);
    invalidation.reportMealMutation();
    TestBed.tick();
    invalidation.reportMealMutation();
    TestBed.tick();
    stale.next({ statistics: { totalCalories: 100 }, dailyGoal: GOAL_KCAL });
    expect(toastService.success).toHaveBeenCalledTimes(1);

    const pending = new Subject<{ statistics: { totalCalories: number }; dailyGoal: number }>();
    dashboardService.getSnapshotSilently.mockReturnValueOnce(pending);
    invalidation.reportMealMutation();
    TestBed.tick();
    TestBed.inject(SessionEventsService).notifyLocalSessionInvalidating();
    pending.next({ statistics: { totalCalories: 200 }, dailyGoal: GOAL_KCAL });
    expect(toastService.success).toHaveBeenCalledTimes(1);
    expect(dismiss).toHaveBeenCalledOnce();
});

it('ignores meal mutations while signed out', () => {
    authenticated.set(false);
    invalidation.reportMealMutation();
    TestBed.tick();
    expect(dashboardService.getSnapshotSilently).not.toHaveBeenCalled();
});

it('omits unavailable progress and recovers on the next successful mutation', () => {
    dashboardService.getSnapshotSilently.mockReturnValueOnce(of(null));
    invalidation.reportMealMutation();
    TestBed.tick();
    expect(toastService.success).not.toHaveBeenCalled();

    invalidation.reportMealMutation();
    TestBed.tick();
    expect(toastService.success).toHaveBeenCalledOnce();
});

it('does not display a response belonging to a previous account', () => {
    const pending = new Subject<{ statistics: { totalCalories: number }; dailyGoal: number }>();
    dashboardService.getSnapshotSilently.mockReturnValueOnce(pending);
    invalidation.reportMealMutation();
    TestBed.tick();
    owner.set('user-2');
    pending.next({ statistics: { totalCalories: CONSUMED_KCAL }, dailyGoal: GOAL_KCAL });
    expect(toastService.success).not.toHaveBeenCalled();
});
