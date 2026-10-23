import { signal, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdTourService } from 'fd-tour';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Subject } from 'rxjs';
import { vi } from 'vitest';

import type { WeeklyGoalDialogData } from '../app/features/weekly-check-in/dialogs/weekly-goal-dialog/weekly-goal-dialog';
import type { WeeklyReviewDialogData } from '../app/features/weekly-check-in/dialogs/weekly-review-dialog/weekly-review-dialog';
import { WeeklyCheckInFacade } from '../app/features/weekly-check-in/lib/weekly-check-in.facade';
import type { WeeklyReviewViewModel } from '../app/features/weekly-check-in/lib/weekly-check-in.types';
import type { WeekSummary } from '../app/features/weekly-check-in/models/weekly-check-in.data';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../app/features/weekly-check-in/models/weekly-goal.data';
import { WeeklyCheckInPageComponent } from '../app/features/weekly-check-in/pages/weekly-check-in-page/weekly-check-in-page';
import { kilogramsToPounds, MeasurementSystemService } from '../app/shared/measurements/measurement-system.service';
import { calendarDate } from '../app/shared/models/semantics/date-value';
import { waitForAsyncTasksAsync } from './async-testing';
import { provideTranslateTesting } from './translate-testing.module';

export const CURRENT_WEEK_START = calendarDate('2026-03-16');
export const NEXT_WEEK_START = calendarDate('2026-03-23');
export const GOAL: WeeklyGoal = {
    id: 'next-goal',
    weekStart: NEXT_WEEK_START,
    type: 'DiaryLogging',
    targetDays: 5,
    progressDays: 2,
    isCompleted: false,
    reminderEnabled: false,
    reminderTime: null,
    timeZoneOffsetMinutes: null,
};
export const WEEK: WeekSummary = {
    totalCalories: 14000,
    avgDailyCalories: 2000,
    avgProteins: 105,
    avgFats: 60,
    avgCarbs: 200,
    mealsLogged: 14,
    daysLogged: 5,
    weightStart: 80,
    weightEnd: 79,
    waistStart: 85,
    waistEnd: 84,
    totalHydrationMl: 14000,
    avgDailyHydrationMl: 2000,
};
export const REVIEW: WeeklyReviewViewModel = {
    daysLogged: 5,
    hasEnoughData: true,
    summaryKey: 'review-summary',
    focusTitleKey: 'focus-title',
    focusDescriptionKey: 'focus-description',
    focusTarget: 3,
    insights: ['first', 'second', 'third', 'fourth', 'fifth'].map(key => ({
        key,
        icon: 'info_outline',
        tone: 'info',
        labelKey: `insight-${key}`,
    })),
};

type SignalFields = Pick<
    WeeklyCheckInFacade,
    | 'isLoading'
    | 'isRefreshing'
    | 'hasDataError'
    | 'thisWeek'
    | 'review'
    | 'weeklyGoal'
    | 'selectedWeekGoal'
    | 'isGoalLoading'
    | 'isSelectedWeekGoalLoading'
    | 'hasGoalError'
    | 'hasSelectedWeekGoalError'
    | 'selectedWeek'
    | 'isSelectedWeekPast'
    | 'isGoalPeriodClosed'
    | 'goalWeekStartIso'
    | 'selectedWeekStartIso'
>;
type WritableFacadeSignals = { [Key in keyof SignalFields]: WritableSignal<ReturnType<SignalFields[Key]>> };
export type WeeklyCheckInFacadeMock = WritableFacadeSignals & {
    initialize: ReturnType<typeof vi.fn<WeeklyCheckInFacade['initialize']>>;
    retryData: ReturnType<typeof vi.fn<WeeklyCheckInFacade['retryData']>>;
    retryNextWeekGoal: ReturnType<typeof vi.fn<WeeklyCheckInFacade['retryNextWeekGoal']>>;
    retrySelectedWeekGoal: ReturnType<typeof vi.fn<WeeklyCheckInFacade['retrySelectedWeekGoal']>>;
    reloadGoal: ReturnType<typeof vi.fn<WeeklyCheckInFacade['reloadGoal']>>;
    saveGoalAsync: ReturnType<typeof vi.fn<WeeklyCheckInFacade['saveGoalAsync']>>;
};

function createFacadeMock(): WeeklyCheckInFacadeMock {
    return {
        isLoading: signal(false),
        isRefreshing: signal(false),
        hasDataError: signal(false),
        thisWeek: signal<WeekSummary | undefined>(WEEK),
        review: signal<WeeklyReviewViewModel | null>(REVIEW),
        weeklyGoal: signal<WeeklyGoal | null>(null),
        selectedWeekGoal: signal<WeeklyGoal | null>(null),
        isGoalLoading: signal(false),
        isSelectedWeekGoalLoading: signal(false),
        hasGoalError: signal(false),
        hasSelectedWeekGoalError: signal(false),
        selectedWeek: signal(new Date('2026-03-16T00:00:00')),
        isSelectedWeekPast: signal(false),
        isGoalPeriodClosed: signal(false),
        selectedWeekStartIso: signal(CURRENT_WEEK_START),
        goalWeekStartIso: signal(NEXT_WEEK_START),
        initialize: vi.fn(),
        retryData: vi.fn(),
        retryNextWeekGoal: vi.fn(),
        retrySelectedWeekGoal: vi.fn(),
        reloadGoal: vi.fn(),
        saveGoalAsync: vi.fn(async (_payload: UpsertWeeklyGoalPayload) => {
            await waitForAsyncTasksAsync();
            return GOAL;
        }),
    };
}

type DialogData = WeeklyGoalDialogData | WeeklyReviewDialogData;
type GoalDialogRefMock = { afterClosed: () => Subject<WeeklyGoal | null | undefined> };

export function setupWeeklyPage(): {
    facade: WeeklyCheckInFacadeMock;
    closed: Subject<WeeklyGoal | null | undefined>;
    dialog: { open: ReturnType<typeof vi.fn<(_component: unknown, config?: FdUiDialogConfig<DialogData>) => unknown>> };
    tour: { start: ReturnType<typeof vi.fn<FdTourService['start']>> };
    measurements: WritableSignal<'metric' | 'imperial'>;
} {
    const facade = createFacadeMock();
    const closed = new Subject<WeeklyGoal | null | undefined>();
    const dialog = {
        open: vi.fn((_component: unknown, _config?: FdUiDialogConfig<DialogData>): GoalDialogRefMock => ({
            afterClosed: (): Subject<WeeklyGoal | null | undefined> => closed,
        })),
    };
    const tour = { start: vi.fn<FdTourService['start']>() };
    const measurements = signal<'metric' | 'imperial'>('metric');
    TestBed.configureTestingModule({
        imports: [WeeklyCheckInPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogService, useValue: dialog },
            { provide: FdTourService, useValue: tour },
            { provide: MeasurementSystemService, useValue: { system: measurements, displayWeight: kilogramsToPounds } },
        ],
    }).overrideComponent(WeeklyCheckInPageComponent, {
        set: { providers: [{ provide: WeeklyCheckInFacade, useValue: facade }] },
    });
    return { facade, closed, dialog, tour, measurements };
}

export function renderWeeklyPage(): ComponentFixture<WeeklyCheckInPageComponent> {
    const fixture = TestBed.createComponent(WeeklyCheckInPageComponent);
    fixture.detectChanges();
    return fixture;
}

export function getElement(fixture: ComponentFixture<WeeklyCheckInPageComponent>): HTMLElement {
    const element: unknown = fixture.nativeElement;
    if (!(element instanceof HTMLElement)) {
        throw new TypeError('Expected a rendered weekly check-in element');
    }
    return element;
}

export function getButton(element: HTMLElement, selector: string): HTMLButtonElement {
    const button = element.querySelector<HTMLButtonElement>(selector);
    if (button === null) {
        throw new Error(`Missing weekly check-in action: ${selector}`);
    }
    return button;
}

export function getGoalDialogData(dialog: ReturnType<typeof setupWeeklyPage>['dialog']): WeeklyGoalDialogData {
    const data = dialog.open.mock.calls.at(-1)?.[1]?.data;
    if (data === undefined || data === null || !('saveGoalAsync' in data)) {
        throw new Error('Expected a weekly goal dialog');
    }
    return data;
}
