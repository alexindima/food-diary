import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { WeekSummaryHttpResponse } from '../../shared/api/sdk/generated/model/week-summary-http-response';
import type { WeeklyCheckInHttpResponse } from '../../shared/api/sdk/generated/model/weekly-check-in-http-response';
import type { WeeklyGoalHttpResponse } from '../../shared/api/sdk/generated/model/weekly-goal-http-response';
import weeklyCheckInRoutes from './weekly-check-in.routes';

const CHECK_IN_URL = environment.apiUrls.weeklyCheckIn;
const GOALS_URL = environment.apiUrls.weeklyGoals;
const LOCAL_SUNDAY = '2026-03-08T01:55:00';
const CURRENT_WEEK = '2026-03-02';
const NEXT_WEEK = '2026-03-09';
const PREVIOUS_WEEK = '2026-02-23';
const CLOSED_WEEK = '2026-02-16';
const PREVIOUS_SUNDAY = '2026-03-01';
const CURRENT_DAYS_LOGGED = 6;
const PREVIOUS_DAYS_LOGGED = 3;
const STANDARD_GOAL_DAYS = 5;
const LONG_GOAL_DAYS = 7;
const REMINDER_TIME = '19:45';
const DEFAULT_SUMMARY = {
    totalCalories: 1800,
    avgDailyCalories: 1800,
    avgProteins: 95,
    avgFats: 60,
    avgCarbs: 220,
    mealsLogged: 3,
    daysLogged: 1,
    totalHydrationMl: 1900,
    avgDailyHydrationMl: 1900,
} satisfies WeekSummaryHttpResponse;

type WeeklyFixture = { harness: RouterTestingHarness; http: HttpTestingController; page: HTMLElement };

afterEach(() => {
    try {
        TestBed.inject(FdUiDialogService).closeAll();
        TestBed.inject(HttpTestingController).verify();
    } finally {
        vi.unstubAllGlobals();
        vi.useRealTimers();
    }
});

function redraw(harness: RouterTestingHarness): void {
    TestBed.tick();
    harness.detectChanges();
}

async function settleAsync(harness: RouterTestingHarness): Promise<void> {
    await new Promise<void>(resolve => {
        setTimeout(resolve, 0);
    });
    redraw(harness);
}

async function renderWeeklyPageAsync(): Promise<WeeklyFixture> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(LOCAL_SUNDAY));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'weekly-check-in', children: weeklyCheckInRoutes }]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
        ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('en'));
    const harness = await RouterTestingHarness.create('/weekly-check-in');
    redraw(harness);
    const page = harness.routeNativeElement;
    if (page === null) {
        throw new Error('Expected the weekly check-in route');
    }
    return { harness, http: TestBed.inject(HttpTestingController), page };
}

function makeReport(daysLogged: number): WeeklyCheckInHttpResponse {
    return {
        thisWeek: {
            ...DEFAULT_SUMMARY,
            daysLogged,
            totalCalories: DEFAULT_SUMMARY.avgDailyCalories * daysLogged,
            totalHydrationMl: DEFAULT_SUMMARY.avgDailyHydrationMl * daysLogged,
            mealsLogged: DEFAULT_SUMMARY.mealsLogged * daysLogged,
        },
        lastWeek: DEFAULT_SUMMARY,
        trends: { calorieChange: 0, proteinChange: 0, fatChange: 0, carbChange: 0, hydrationChange: 0, mealsLoggedChange: 0 },
        suggestions: [],
    };
}

function makeGoal(weekStart: string, targetDays = STANDARD_GOAL_DAYS, progressDays = 2): WeeklyGoalHttpResponse {
    return {
        id: '2ad73d24-e0a5-49a8-a794-bd015e16ef71',
        weekStart,
        type: 'DiaryLogging',
        targetDays,
        progressDays,
        isCompleted: progressDays === targetDays,
        reminderEnabled: false,
    };
}

function expectWeekRead(http: HttpTestingController, url: string, weekStart: string): TestRequest {
    const match = http.expectOne(
        request => request.method === 'GET' && request.url === url && request.params.get('weekStart') === weekStart,
    );
    expect(match.request.params.get('timeZoneId')).toBe(new Intl.DateTimeFormat().resolvedOptions().timeZone);
    return match;
}

function expectSelectionReads(
    http: HttpTestingController,
    weekStart: string,
    followingWeek: string,
): [report: TestRequest, selectedGoal: TestRequest, nextGoal: TestRequest] {
    return [
        expectWeekRead(http, CHECK_IN_URL, weekStart),
        expectWeekRead(http, GOALS_URL, weekStart),
        expectWeekRead(http, GOALS_URL, followingWeek),
    ];
}

function unavailable(request: TestRequest): void {
    request.flush({ error: 'Service unavailable' }, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
}

function queryElement(root: ParentNode, selector: string): HTMLElement {
    const element = root.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected ${selector} to render`);
    }
    return element;
}

function queryButton(root: ParentNode, label: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll('button')).find(candidate => {
        const visibleLabel = candidate.querySelector('.fd-ui-button__label')?.textContent ?? candidate.textContent;
        return visibleLabel.trim() === label || candidate.getAttribute('aria-label') === label;
    });
    if (button === undefined) {
        throw new Error(`Expected the action ${label} to render`);
    }
    return button;
}

function selectWeekInCalendar(fixture: WeeklyFixture, day: string): void {
    queryButton(fixture.page, 'WEEKLY_CHECK_IN.WEEK_PICKER.OPEN').click();
    redraw(fixture.harness);
    const calendar = queryElement(document, '.fd-ui-week-picker__panel fd-ui-calendar');
    queryElement(calendar, `button[data-date="${day}"]`).click();
    redraw(fixture.harness);
    expect(document.querySelector('.fd-ui-week-picker__panel')).toBeNull();
}

function expectDaysLogged(page: HTMLElement, days: number): void {
    expect(page.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe(String(days));
}

describe('Rendered weekly review for a new diary', () => {
    it('shows Start Logging as the only current-week insight when both weeks have no entries', async () => {
        const { harness, http, page } = await renderWeeklyPageAsync();
        const [report, selectedGoal, nextGoal] = expectSelectionReads(http, CURRENT_WEEK, NEXT_WEEK);
        const emptyWeek = {
            totalCalories: 0,
            avgDailyCalories: 0,
            avgProteins: 0,
            avgFats: 0,
            avgCarbs: 0,
            mealsLogged: 0,
            daysLogged: 0,
            weightStart: null,
            weightEnd: null,
            waistStart: null,
            waistEnd: null,
            totalHydrationMl: 0,
            avgDailyHydrationMl: 0,
        } satisfies WeekSummaryHttpResponse;
        report.flush({
            thisWeek: emptyWeek,
            lastWeek: emptyWeek,
            trends: {
                calorieChange: 0,
                proteinChange: 0,
                fatChange: 0,
                carbChange: 0,
                hydrationChange: 0,
                mealsLoggedChange: 0,
                weightChange: null,
                waistChange: null,
            },
            suggestions: ['suggestion.log_more_days'],
        } satisfies WeeklyCheckInHttpResponse);
        selectedGoal.flush(null);
        nextGoal.flush(null);
        await settleAsync(harness);

        expectDaysLogged(page, 0);
        const summary = queryElement(page, '[data-tour-id="weekly-check-in-stats"]');
        const insights = summary.querySelectorAll('.check-in__insight .fd-ui-body-sm');
        expect(Array.from(insights, insight => insight.textContent.trim())).toEqual(['WEEKLY_CHECK_IN.INSIGHTS.START_LOGGING']);
    });
});

describe('Rendered weekly selection and report recovery', () => {
    it('switches all three HTTP reads from the local current week to a week selected on the calendar', async () => {
        const fixture = await renderWeeklyPageAsync();
        const obsoleteReads = expectSelectionReads(fixture.http, CURRENT_WEEK, NEXT_WEEK);
        expect(queryButton(fixture.page, 'WEEKLY_CHECK_IN.WEEK_PICKER.NEXT').disabled).toBe(true);

        selectWeekInCalendar(fixture, PREVIOUS_SUNDAY);

        expect(obsoleteReads.every(request => request.cancelled)).toBe(true);
        const [report, selectedGoal, followingGoal] = expectSelectionReads(fixture.http, PREVIOUS_WEEK, CURRENT_WEEK);
        report.flush(makeReport(1));
        selectedGoal.flush(makeGoal(PREVIOUS_WEEK));
        followingGoal.flush(null);
        await settleAsync(fixture.harness);

        expectDaysLogged(fixture.page, 1);
        expect(fixture.page.textContent).toContain('WEEKLY_CHECK_IN.INSIGHTS.LOGGING_PROGRESS_ONE');
        expect(queryButton(fixture.page, 'WEEKLY_CHECK_IN.GOAL.CLOSED_ACTION').disabled).toBe(true);
        expect(queryButton(fixture.page, 'WEEKLY_CHECK_IN.GOAL.CREATE_NEXT').disabled).toBe(false);
    });

    it('shows an unavailable selected week and retries its report without flashing the previous result', async () => {
        const fixture = await renderWeeklyPageAsync();
        const [currentReport, currentGoal, nextGoal] = expectSelectionReads(fixture.http, CURRENT_WEEK, NEXT_WEEK);
        currentReport.flush(makeReport(CURRENT_DAYS_LOGGED));
        currentGoal.flush(null);
        nextGoal.flush(null);
        await settleAsync(fixture.harness);
        expectDaysLogged(fixture.page, CURRENT_DAYS_LOGGED);

        queryButton(fixture.page, 'WEEKLY_CHECK_IN.WEEK_PICKER.PREVIOUS').click();
        redraw(fixture.harness);
        const [pastReport, pastGoal, followingGoal] = expectSelectionReads(fixture.http, PREVIOUS_WEEK, CURRENT_WEEK);
        pastGoal.flush(null);
        followingGoal.flush(null);
        expect(fixture.page.querySelector('[role="progressbar"]')).toBeNull();

        unavailable(pastReport);
        await settleAsync(fixture.harness);
        expect(fixture.page.querySelector('[role="progressbar"]')).toBeNull();
        const error = queryElement(fixture.page, 'div:not([hidden]) > fd-error-state');
        queryButton(error, 'ERRORS.RETRY').click();
        redraw(fixture.harness);
        fixture.http.expectNone(request => request.url === GOALS_URL);
        expectWeekRead(fixture.http, CHECK_IN_URL, PREVIOUS_WEEK).flush(makeReport(PREVIOUS_DAYS_LOGGED));
        await settleAsync(fixture.harness);

        expect(fixture.page.querySelector('div:not([hidden]) > fd-error-state')).toBeNull();
        expectDaysLogged(fixture.page, PREVIOUS_DAYS_LOGGED);
        expect(fixture.page.textContent).toContain('WEEKLY_CHECK_IN.SUMMARY.TITLE');
    });
});

describe('Rendered weekly goal ownership', () => {
    it('keeps the selected goal action busy while its report and next goal are already visible', async () => {
        const { harness, http, page } = await renderWeeklyPageAsync();
        const [report, selectedGoal, nextGoal] = expectSelectionReads(http, CURRENT_WEEK, NEXT_WEEK);
        report.flush(makeReport(CURRENT_DAYS_LOGGED));
        nextGoal.flush(makeGoal(NEXT_WEEK));
        await settleAsync(harness);

        const currentCard = queryElement(page, '[data-tour-id="weekly-check-in-current-goal"]');
        const nextCard = queryElement(page, '[data-tour-id="weekly-check-in-suggestions"]');
        const pendingAction = queryButton(currentCard, 'WEEKLY_CHECK_IN.GOAL.CREATE_CURRENT');
        expect(pendingAction.disabled).toBe(true);
        expect(pendingAction.getAttribute('aria-busy')).toBe('true');
        expectDaysLogged(page, CURRENT_DAYS_LOGGED);
        expect(nextCard.textContent).toContain('2 / 5');
        expect(queryButton(nextCard, 'WEEKLY_CHECK_IN.GOAL.EDIT').disabled).toBe(false);
        pendingAction.click();
        expect(document.querySelector('fd-weekly-goal-dialog')).toBeNull();
        http.expectNone(request => request.url === GOALS_URL && request.method === 'PUT');

        selectedGoal.flush(makeGoal(CURRENT_WEEK, LONG_GOAL_DAYS, CURRENT_DAYS_LOGGED));
        await settleAsync(harness);

        expect(currentCard.textContent).toContain('6 / 7');
        expect(queryButton(currentCard, 'WEEKLY_CHECK_IN.GOAL.EDIT').disabled).toBe(false);
        expect(nextCard.textContent).toContain('2 / 5');
    });

    it.each([
        { goal: 'selected', card: 'weekly-check-in-current-goal', label: 'WEEKLY_CHECK_IN.GOAL.CREATE_CURRENT' },
        { goal: 'next', card: 'weekly-check-in-suggestions', label: 'WEEKLY_CHECK_IN.GOAL.CREATE_NEXT' },
    ])('rejects the previous $goal goal action after choosing a closed week before repaint', async ({ card, label }) => {
        const { harness, http, page } = await renderWeeklyPageAsync();
        const [report, selectedGoal, nextGoal] = expectSelectionReads(http, CURRENT_WEEK, NEXT_WEEK);
        report.flush(makeReport(CURRENT_DAYS_LOGGED));
        selectedGoal.flush(null);
        nextGoal.flush(null);
        await settleAsync(harness);
        const action = queryButton(queryElement(page, `[data-tour-id="${card}"]`), label);
        const previousWeek = queryButton(page, 'WEEKLY_CHECK_IN.WEEK_PICKER.PREVIOUS');
        const openDialog = vi.spyOn(TestBed.inject(FdUiDialogService), 'open');

        previousWeek.click();
        previousWeek.click();
        expect(action.isConnected).toBe(true);
        expect(action.disabled).toBe(false);
        action.click();
        expect(openDialog).not.toHaveBeenCalled();

        redraw(harness);
        const [closedReport, closedGoal, followingGoal] = expectSelectionReads(http, CLOSED_WEEK, PREVIOUS_WEEK);
        closedReport.flush(makeReport(PREVIOUS_DAYS_LOGGED));
        closedGoal.flush(null);
        followingGoal.flush(null);
        await settleAsync(harness);
        expectDaysLogged(page, PREVIOUS_DAYS_LOGGED);
        const closedCard = queryElement(page, `[data-tour-id="${card}"]`);
        expect(queryButton(closedCard, 'WEEKLY_CHECK_IN.GOAL.CLOSED_ACTION').disabled).toBe(true);
        expect(document.querySelector('fd-weekly-goal-dialog')).toBeNull();
    });
});

describe('Rendered weekly goal recovery', () => {
    it.each([
        {
            name: 'selected',
            failed: CURRENT_WEEK,
            healthy: NEXT_WEEK,
            card: 'weekly-check-in-current-goal',
            healthyCard: 'weekly-check-in-suggestions',
        },
        {
            name: 'next',
            failed: NEXT_WEEK,
            healthy: CURRENT_WEEK,
            card: 'weekly-check-in-suggestions',
            healthyCard: 'weekly-check-in-current-goal',
        },
    ])('retries the $name goal without reloading the other goal or the report', async ({ failed, healthy, card, healthyCard }) => {
        const { harness, http, page } = await renderWeeklyPageAsync();
        expectWeekRead(http, CHECK_IN_URL, CURRENT_WEEK).flush(makeReport(CURRENT_DAYS_LOGGED));
        unavailable(expectWeekRead(http, GOALS_URL, failed));
        expectWeekRead(http, GOALS_URL, healthy).flush(makeGoal(healthy));
        await settleAsync(harness);

        const failedCard = queryElement(page, `[data-tour-id="${card}"]`);
        const otherCard = queryElement(page, `[data-tour-id="${healthyCard}"]`);
        expect(otherCard.textContent).toContain('2 / 5');
        queryButton(failedCard, 'ERRORS.RETRY').click();
        redraw(harness);
        expectWeekRead(http, GOALS_URL, failed).flush(makeGoal(failed, LONG_GOAL_DAYS));
        http.expectNone(request => request.url === GOALS_URL || request.url === CHECK_IN_URL);
        await settleAsync(harness);

        expect(failedCard.querySelector('fd-error-state')).toBeNull();
        expect(failedCard.textContent).toContain('2 / 7');
        expect(otherCard.textContent).toContain('2 / 5');
        expectDaysLogged(page, CURRENT_DAYS_LOGGED);
    });
});

function pickReminderTime(dialog: HTMLElement, harness: RouterTestingHarness): void {
    queryButton(dialog, 'WEEKLY_CHECK_IN.GOAL.REMINDER_ARIA').click();
    redraw(harness);
    queryElement(dialog, 'fd-ui-time-input input').click();
    redraw(harness);
    const hours = queryElement(document, '[role="group"][aria-label="WEEKLY_CHECK_IN.GOAL.REMINDER_HOURS"]');
    queryButton(hours, '19').click();
    redraw(harness);
    const minutes = queryElement(document, '[role="group"][aria-label="WEEKLY_CHECK_IN.GOAL.REMINDER_MINUTES"]');
    queryButton(minutes, '45').click();
    redraw(harness);
}

describe('Rendered weekly reminder creation', () => {
    it('preserves the chosen days and wall time on an HTTP failure and publishes the next local week on retry', async () => {
        const { harness, http, page } = await renderWeeklyPageAsync();
        const [report, selectedGoal, nextGoal] = expectSelectionReads(http, CURRENT_WEEK, NEXT_WEEK);
        report.flush(makeReport(CURRENT_DAYS_LOGGED));
        selectedGoal.flush(null);
        nextGoal.flush(null);
        await settleAsync(harness);
        queryButton(page, 'WEEKLY_CHECK_IN.GOAL.CREATE_NEXT').click();
        redraw(harness);

        const dialog = queryElement(document, 'fd-weekly-goal-dialog');
        const sevenDays = queryButton(dialog, 'WEEKLY_CHECK_IN.GOAL.TARGET_7');
        sevenDays.click();
        redraw(harness);
        pickReminderTime(dialog, harness);
        expect(dialog.querySelector<HTMLInputElement>('fd-ui-time-input input')?.value).toBe(REMINDER_TIME);
        const create = queryButton(dialog, 'WEEKLY_CHECK_IN.GOAL.CREATE');
        create.click();
        redraw(harness);
        const firstSave = http.expectOne({ method: 'PUT', url: GOALS_URL });
        const wire = {
            weekStart: NEXT_WEEK,
            targetDays: LONG_GOAL_DAYS,
            reminderEnabled: true,
            reminderTime: REMINDER_TIME,
            timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
        };
        expect(firstSave.request.body).toEqual(wire);
        expect(create.disabled).toBe(true);
        create.click();
        http.expectNone({ method: 'PUT', url: GOALS_URL });

        unavailable(firstSave);
        await settleAsync(harness);
        expect(dialog.querySelector('[role="alert"]')?.textContent).toContain('WEEKLY_CHECK_IN.GOAL.SAVE_ERROR');
        expect(sevenDays.getAttribute('aria-checked')).toBe('true');
        expect(dialog.querySelector<HTMLInputElement>('fd-ui-time-input input')?.value).toBe(REMINDER_TIME);
        expect(create.disabled).toBe(false);
        http.expectNone(request => request.method === 'GET');

        create.click();
        redraw(harness);
        const retry = http.expectOne({ method: 'PUT', url: GOALS_URL });
        expect(retry.request.body).toEqual(wire);
        const savedGoal = {
            ...makeGoal(NEXT_WEEK, LONG_GOAL_DAYS, 0),
            reminderEnabled: true,
            reminderTime: `${REMINDER_TIME}:00`,
            timeZoneOffsetMinutes: wire.timeZoneOffsetMinutes,
        } satisfies WeeklyGoalHttpResponse;
        retry.flush(savedGoal);
        await settleAsync(harness);
        expect(document.querySelector('fd-weekly-goal-dialog')).toBeNull();
        expectWeekRead(http, GOALS_URL, CURRENT_WEEK).flush(null);
        expectWeekRead(http, GOALS_URL, NEXT_WEEK).flush(savedGoal);
        await settleAsync(harness);
        const goalCard = queryElement(page, '[data-tour-id="weekly-check-in-suggestions"]');
        expect(goalCard.textContent).toContain('0 / 7');
        expectDaysLogged(page, CURRENT_DAYS_LOGGED);

        queryButton(goalCard, 'WEEKLY_CHECK_IN.GOAL.EDIT').click();
        redraw(harness);
        const savedDialog = queryElement(document, 'fd-weekly-goal-dialog');
        expect(queryButton(savedDialog, 'WEEKLY_CHECK_IN.GOAL.REMINDER_ARIA').getAttribute('aria-checked')).toBe('true');
        expect(savedDialog.querySelector<HTMLInputElement>('fd-ui-time-input input')?.value).toBe(REMINDER_TIME);
    });
});
