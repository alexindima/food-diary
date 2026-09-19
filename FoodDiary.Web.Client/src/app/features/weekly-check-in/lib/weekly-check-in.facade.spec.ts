import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import { WeeklyGoalService } from '../api/weekly-goal.service';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';
import { WeeklyCheckInFacade } from './weekly-check-in.facade';

const WEEKLY_CALORIES = 14000;
const DAYS_PER_WEEK = 7;
const TEST_YEAR = 2026;
const CURRENT_DAY = 18;
const NOON_HOUR = 12;
const PREVIOUS_MONDAY = 9;
const OLD_WEEK_CALORIES = 7000;

const GOAL: WeeklyGoal = {
    id: 'goal',
    weekStart: '2026-03-23',
    type: 'DiaryLogging',
    targetDays: 5,
    progressDays: 2,
    isCompleted: false,
    reminderEnabled: false,
    reminderTime: null,
    timeZoneOffsetMinutes: null,
};
const PAYLOAD: UpsertWeeklyGoalPayload = {
    weekStart: GOAL.weekStart,
    targetDays: 5,
    reminderEnabled: false,
    reminderTime: null,
    timeZoneOffsetMinutes: null,
};

function createData(calories = WEEKLY_CALORIES): WeeklyCheckInData {
    const week = {
        totalCalories: calories,
        avgDailyCalories: calories / DAYS_PER_WEEK,
        avgProteins: 100,
        avgFats: 60,
        avgCarbs: 200,
        mealsLogged: 14,
        daysLogged: 7,
        weightStart: 80,
        weightEnd: 79,
        waistStart: 85,
        waistEnd: 84,
        totalHydrationMl: 14000,
        avgDailyHydrationMl: 2000,
    };
    return {
        thisWeek: week,
        lastWeek: { ...week },
        suggestions: [],
        trends: {
            calorieChange: 0,
            proteinChange: 0,
            fatChange: 0,
            carbChange: 0,
            weightChange: -1,
            waistChange: -1,
            hydrationChange: 0,
            mealsLoggedChange: 0,
        },
    };
}

function setup(): {
    facade: WeeklyCheckInFacade;
    dataService: { getData: ReturnType<typeof vi.fn> };
    goals: { getGoal: ReturnType<typeof vi.fn>; upsertGoal: ReturnType<typeof vi.fn> };
    measurementSystem: ReturnType<typeof signal<'metric' | 'imperial'>>;
} {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(TEST_YEAR, 2, CURRENT_DAY, NOON_HOUR));
    TestBed.resetTestingModule();
    const dataService = { getData: vi.fn((_week: string) => of(createData())) };
    const goals = {
        getGoal: vi.fn((_week: string) => of<WeeklyGoal | null>(GOAL)),
        upsertGoal: vi.fn((_payload: UpsertWeeklyGoalPayload) => of<WeeklyGoal | null>(GOAL)),
    };
    const measurementSystem = signal<'metric' | 'imperial'>('metric');
    TestBed.configureTestingModule({
        providers: [
            WeeklyCheckInFacade,
            { provide: WeeklyCheckInService, useValue: dataService },
            { provide: WeeklyGoalService, useValue: goals },
            { provide: MeasurementSystemService, useValue: { system: measurementSystem } },
        ],
    });
    return { facade: TestBed.inject(WeeklyCheckInFacade), dataService, goals, measurementSystem };
}

async function settleAsync(): Promise<void> {
    TestBed.tick();
    await new Promise<void>(resolve => setTimeout(resolve, 0));
    TestBed.tick();
}

afterEach(() => vi.useRealTimers());

describe('WeeklyCheckInFacade (1)', () => {
    it('loads the local Monday and separates this week from next week goals', async () => {
        const { facade, dataService, goals } = setup();
        expect(facade.selectedWeekStartIso()).toBe('2026-03-16');
        expect(facade.goalWeekStartIso()).toBe('2026-03-23');
        expect(facade.data()).toBeNull();
        expect(facade.suggestions()).toEqual([]);
        expect(facade.weeklyGoal()).toBeNull();
        facade.initialize();
        await settleAsync();
        expect(dataService.getData).toHaveBeenCalledWith('2026-03-16');
        expect(goals.getGoal).toHaveBeenCalledWith('2026-03-16');
        expect(goals.getGoal).toHaveBeenCalledWith('2026-03-23');
        expect(facade.thisWeek()).toEqual(createData().thisWeek);
        expect(facade.trends()).toEqual(createData().trends);
        expect(facade.weeklyGoal()).toEqual(GOAL);
        expect(facade.selectedWeekGoal()).toEqual(GOAL);
        expect(facade.isLoading()).toBe(false);
        expect(facade.isGoalLoading()).toBe(false);
        expect(facade.isSelectedWeekGoalLoading()).toBe(false);
        expect(facade.review()).toBeDefined();
        expect(facade.suggestionRows()).toEqual([]);
        expect(facade.trendCards().length).toBeGreaterThan(0);
    });
    it('retains the previous week during refresh and after a failed refresh', async () => {
        const { facade, dataService } = setup();
        await settleAsync();
        const previous = facade.data();
        const pending = new Subject<WeeklyCheckInData>();
        dataService.getData.mockReturnValueOnce(pending);
        facade.selectedWeek.set(new Date(TEST_YEAR, 2, PREVIOUS_MONDAY));
        await settleAsync();
        expect(facade.isRefreshing()).toBe(true);
        expect(facade.isLoading()).toBe(false);
        expect(facade.data()).toEqual(previous);
        pending.error(new Error('offline'));
        await settleAsync();
        expect(facade.isRefreshing()).toBe(false);
        expect(facade.data()).toEqual(previous);
        expect(facade.isSelectedWeekPast()).toBe(true);
        expect(facade.isGoalPeriodClosed()).toBe(false);
        facade.selectedWeek.set(new Date(TEST_YEAR, 2, 2));
        expect(facade.isGoalPeriodClosed()).toBe(true);
    });
});

describe('WeeklyCheckInFacade (2)', () => {
    it('ignores the result of a previously selected week', async () => {
        const { facade, dataService } = setup();
        const old = new Subject<WeeklyCheckInData>();
        dataService.getData.mockReturnValueOnce(old);
        await settleAsync();
        expect(facade.isLoading()).toBe(true);
        facade.selectedWeek.set(new Date(TEST_YEAR, 2, PREVIOUS_MONDAY));
        await settleAsync();
        old.next(createData(OLD_WEEK_CALORIES));
        old.complete();
        await settleAsync();
        expect(facade.thisWeek()?.totalCalories).toBe(WEEKLY_CALORIES);
        expect(facade.selectedWeekStartIso()).toBe('2026-03-09');
    });
    it('reloads both goal resources after saving and supports explicit reload', async () => {
        const { facade, goals } = setup();
        await settleAsync();
        goals.getGoal.mockClear();
        await expect(facade.saveGoalAsync(PAYLOAD)).resolves.toEqual(GOAL);
        await settleAsync();
        expect(goals.upsertGoal).toHaveBeenCalledWith(PAYLOAD);
        expect(goals.getGoal).toHaveBeenCalledTimes(2);
        goals.getGoal.mockClear();
        facade.reloadGoal();
        await settleAsync();
        expect(goals.getGoal).toHaveBeenCalledTimes(2);
    });
});

describe('WeeklyCheckInFacade (3)', () => {
    it('does not reload goals when saving rejects', async () => {
        const { facade, goals } = setup();
        await settleAsync();
        goals.getGoal.mockClear();
        goals.upsertGoal.mockReturnValueOnce(throwError(() => new Error('offline')));
        await expect(facade.saveGoalAsync(PAYLOAD)).rejects.toThrow('offline');
        expect(goals.getGoal).not.toHaveBeenCalled();
        expect(facade.weeklyGoal()).toEqual(GOAL);
    });
    it('recalculates display cards when measurement units change', async () => {
        const { facade, measurementSystem } = setup();
        await settleAsync();
        const metric = facade.trendCards();
        measurementSystem.set('imperial');
        expect(facade.trendCards()).not.toEqual(metric);
        expect(facade.trends()?.weightChange).toBe(-1);
    });
});
