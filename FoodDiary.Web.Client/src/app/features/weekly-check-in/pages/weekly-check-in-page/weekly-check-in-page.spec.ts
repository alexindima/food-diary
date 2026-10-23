import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { FdUiWeekPickerComponent } from 'fd-ui-kit';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import {
    CURRENT_WEEK_START,
    getButton,
    getElement,
    getGoalDialogData,
    GOAL,
    NEXT_WEEK_START,
    renderWeeklyPage,
    REVIEW,
    setupWeeklyPage,
    WEEK,
} from '../../../../../testing/weekly-check-in-page.test-helpers';
import { WeeklyGoalDialogComponent } from '../../dialogs/weekly-goal-dialog/weekly-goal-dialog';
import { WeeklyReviewDialogComponent } from '../../dialogs/weekly-review-dialog/weekly-review-dialog';
import type { UpsertWeeklyGoalPayload } from '../../models/weekly-goal.data';

const CURRENT_GOAL = '[data-tour-id="weekly-check-in-current-goal"]';
const NEXT_GOAL = '[data-tour-id="weekly-check-in-suggestions"]';
let context: ReturnType<typeof setupWeeklyPage>;

beforeEach(() => {
    context = setupWeeklyPage();
});

describe('WeeklyCheckInPageComponent loading and recovery', () => {
    it('initializes once and replaces the initial loader with the report', () => {
        context.facade.isLoading.set(true);
        context.facade.review.set(null);
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        expect(context.facade.initialize).toHaveBeenCalledExactlyOnceWith();
        expect(element.querySelector('.check-in')?.getAttribute('aria-busy')).toBe('true');
        expect(element.querySelector('.check-in__loader')?.textContent).toContain('WEEKLY_CHECK_IN.LOADING');
        expect(element.querySelector('.check-in__summary')).toBeNull();

        context.facade.isLoading.set(false);
        context.facade.review.set(REVIEW);
        fixture.detectChanges();
        expect(element.querySelector('.check-in__loader')).toBeNull();
        expect(element.querySelector('.check-in')?.getAttribute('aria-busy')).toBe('false');
        expect(element.querySelector('.check-in__summary')).not.toBeNull();
        expect(context.facade.initialize).toHaveBeenCalledTimes(1);
    });

    it('retains the report with a busy state while refreshing the selected week', () => {
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        context.facade.isRefreshing.set(true);
        fixture.detectChanges();
        expect(element.querySelector('.check-in--refreshing')?.getAttribute('aria-busy')).toBe('true');
        expect(element.querySelector('.check-in__summary')).not.toBeNull();
        expect(element.querySelector('.check-in__loader')).toBeNull();
        context.facade.isRefreshing.set(false);
        fixture.detectChanges();
        expect(element.querySelector('.check-in--refreshing')).toBeNull();
    });

    it('shows a failed initial load, delegates retry and removes the error after recovery', () => {
        context.facade.review.set(null);
        context.facade.hasDataError.set(true);
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        const errorWrapper = element.querySelector('fd-error-state')?.parentElement;
        expect(errorWrapper?.hidden).toBe(false);
        expect(element.querySelector('.check-in__summary')).toBeNull();
        getButton(element, 'fd-error-state button').click();
        expect(context.facade.retryData).toHaveBeenCalledExactlyOnceWith();
        context.facade.hasDataError.set(false);
        context.facade.review.set(REVIEW);
        fixture.detectChanges();
        expect(errorWrapper?.hidden).toBe(true);
        expect(element.querySelector('.check-in__summary')).not.toBeNull();
    });

    it('keeps the loaded report available alongside a refresh error', () => {
        context.facade.hasDataError.set(true);
        const element = getElement(renderWeeklyPage());
        expect(element.querySelector('fd-error-state')?.parentElement?.hidden).toBe(false);
        expect(element.querySelector('.check-in__metrics')).not.toBeNull();
        getButton(element, '.check-in__report-action button').click();
        expect(context.dialog.open).toHaveBeenCalledWith(WeeklyReviewDialogComponent, expect.anything());
    });
});

describe('WeeklyCheckInPageComponent review', () => {
    it('renders coverage, the first three insights, remaining count and suggested focus', () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { WEEKLY_CHECK_IN: { MORE_INSIGHTS: 'Another {{count}} insights' } });
        translate.use('en');
        const element = getElement(renderWeeklyPage());
        expect(element.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe('5');
        expect(element.querySelectorAll('.check-in__segment--filled')).toHaveLength(REVIEW.daysLogged);
        expect(element.querySelector('.check-in__status')?.textContent).toContain('WEEKLY_CHECK_IN.STATUS.READY');
        expect([...element.querySelectorAll('.check-in__insight > .fd-ui-body-sm')].map(row => row.textContent.trim())).toEqual([
            'insight-first',
            'insight-second',
            'insight-third',
        ]);
        expect(element.textContent).not.toContain('insight-fourth');
        expect(element.querySelector('.check-in__insights-more')?.textContent).toContain('Another 2 insights');
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-title`)?.textContent).toContain(REVIEW.focusTitleKey);
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-count`)?.textContent).toContain('3 / 7');
        expect(element.querySelector('.check-in__metric:nth-child(2) strong')?.textContent.replaceAll(/\s+/g, ' ').trim()).toBe(
            '2,000 GENERAL.UNITS.KCAL',
        );
        expect(element.querySelector('.check-in__metric:nth-child(3) strong')?.textContent.replaceAll(/\s+/g, ' ').trim()).toBe(
            '105 GENERAL.UNITS.G',
        );
    });

    it('shows preliminary empty past-week copy instead of insights and unavailable metrics', () => {
        context.facade.isSelectedWeekPast.set(true);
        context.facade.review.set({ ...REVIEW, daysLogged: 0, hasEnoughData: false });
        context.facade.thisWeek.set({ ...WEEK, weightStart: null });
        const element = getElement(renderWeeklyPage());
        expect(element.querySelector('.check-in__status')?.textContent).toContain('WEEKLY_CHECK_IN.STATUS.PRELIMINARY');
        expect(element.querySelectorAll('.check-in__segment--filled')).toHaveLength(0);
        expect(element.querySelector('.check-in__insights')?.textContent).toContain('WEEKLY_CHECK_IN.SUMMARY.PAST_WEEK_NO_DATA');
        expect(element.querySelector('.check-in__insights-more')).toBeNull();
        expect(element.textContent).not.toContain('insight-first');
        const values = [...element.querySelectorAll('.check-in__metric strong')].map(value => value.textContent.trim());
        expect(values[0]).toBe('0 / 7');
        expect(values.slice(1).every(value => value.startsWith('—'))).toBe(true);
    });

    it('passes the full review, selected week and next goal to the detailed report', () => {
        const expectedWeek = { ...WEEK };
        const expectedReview = { ...REVIEW, insights: REVIEW.insights.map(insight => ({ ...insight })) };
        context.facade.weeklyGoal.set(GOAL);
        const element = getElement(renderWeeklyPage());
        getButton(element, '.check-in__report-action button').click();
        expect(context.dialog.open).toHaveBeenCalledExactlyOnceWith(WeeklyReviewDialogComponent, {
            preset: 'detail',
            data: { review: expectedReview, week: expectedWeek, goal: GOAL },
        });
    });

    it('does not open a report when its week summary is unavailable', () => {
        context.facade.thisWeek.set(undefined);
        const element = getElement(renderWeeklyPage());
        getButton(element, '.check-in__report-action button').click();
        expect(context.dialog.open).not.toHaveBeenCalled();
    });
});

describe('WeeklyCheckInPageComponent week selection and display', () => {
    it('forwards the real week picker previous and next selections to facade state', () => {
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        getButton(element, 'button[aria-label="WEEKLY_CHECK_IN.WEEK_PICKER.PREVIOUS"]').click();
        fixture.detectChanges();
        expect(context.facade.selectedWeek()).toEqual(new Date('2026-03-09T00:00:00'));
        getButton(element, 'button[aria-label="WEEKLY_CHECK_IN.WEEK_PICKER.NEXT"]').click();
        fixture.detectChanges();
        expect(context.facade.selectedWeek()).toEqual(new Date('2026-03-16T00:00:00'));
        expect(context.facade.initialize).toHaveBeenCalledTimes(1);
    });

    it('keeps the picker value and locale synchronized with page state', () => {
        const fixture = renderWeeklyPage();
        const picker = fixture.debugElement.query(By.directive(FdUiWeekPickerComponent)).componentInstance as FdUiWeekPickerComponent;
        const selectedWeek = new Date('2026-02-23T00:00:00');
        context.facade.selectedWeek.set(selectedWeek);
        TestBed.inject(TranslateService).use('ru');
        fixture.detectChanges();
        expect(picker.value()).toEqual(selectedWeek);
        expect(picker.locale()).toBe('ru');
        expect(picker.max()).toBeInstanceOf(Date);
    });

    it('does not advance beyond the current local week', () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2026-03-16T12:00:00'));
        try {
            const selectedWeek = new Date('2026-03-16T00:00:00');
            const selectedTimestamp = selectedWeek.getTime();
            context.facade.selectedWeek.set(selectedWeek);
            const fixture = renderWeeklyPage();
            const element = getElement(fixture);
            const next = getButton(element, 'button[aria-label="WEEKLY_CHECK_IN.WEEK_PICKER.NEXT"]');
            const picker = fixture.debugElement.query(By.directive(FdUiWeekPickerComponent)).componentInstance as FdUiWeekPickerComponent;
            expect(picker.max()).toEqual(new Date('2026-03-16T12:00:00'));
            expect(next.disabled).toBe(true);
            next.click();
            fixture.detectChanges();
            expect(context.facade.selectedWeek().getTime()).toBe(selectedTimestamp);
        } finally {
            vi.useRealTimers();
        }
    });

    it('updates weight difference and units without changing stored week values', () => {
        const originalWeek = { ...WEEK };
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        const weight = element.querySelector('.check-in__metric:last-child strong');
        expect(weight?.textContent).toContain('−1.0');
        expect(weight?.textContent).toContain('GENERAL.UNITS.KG');
        context.measurements.set('imperial');
        fixture.detectChanges();
        expect(weight?.textContent).toContain('−2.2');
        expect(weight?.textContent).toContain('GENERAL.UNITS.LB');
        expect(context.facade.thisWeek()).toEqual(originalWeek);
        context.facade.thisWeek.set({ ...WEEK, weightEnd: null });
        fixture.detectChanges();
        expect(weight?.textContent.trim().startsWith('—')).toBe(true);
    });

    it('starts the localized page tour from the rendered help action', () => {
        const element = getElement(renderWeeklyPage());
        getButton(element, '[data-tour-id="weekly-check-in-tour-help"] button').click();
        expect(context.tour.start).toHaveBeenCalledTimes(1);
        const [definition, options] = context.tour.start.mock.calls[0];
        expect(definition.id).toBe('weekly-check-in');
        expect(definition.steps.map(step => step.title)).toContain('WEEKLY_CHECK_IN.TOUR.TRENDS_TITLE');
        expect(options).toEqual({ force: true });
    });
});

describe('WeeklyCheckInPageComponent goal dialogs', () => {
    it.each([
        { selector: CURRENT_GOAL, weekStart: CURRENT_WEEK_START, titleKey: 'WEEKLY_CHECK_IN.GOAL.CURRENT_DIALOG_TITLE' },
        { selector: NEXT_GOAL, weekStart: NEXT_WEEK_START, titleKey: 'WEEKLY_CHECK_IN.GOAL.DIALOG_TITLE' },
    ])('creates a goal for $weekStart through the corresponding action', ({ selector, weekStart, titleKey }) => {
        const element = getElement(renderWeeklyPage());
        getButton(element, `${selector} button`).click();
        expect(context.dialog.open.mock.calls[0]?.[0]).toBe(WeeklyGoalDialogComponent);
        expect(context.dialog.open.mock.calls[0]?.[1]?.preset).toBe('form');
        expect(getGoalDialogData(context.dialog)).toMatchObject({ weekStart, titleKey, goal: null });
    });

    it.each([CURRENT_GOAL, NEXT_GOAL])('edits the existing goal in %s', selector => {
        const currentGoal = { ...GOAL, id: 'current-goal', weekStart: CURRENT_WEEK_START, targetDays: 3 };
        context.facade.selectedWeekGoal.set(currentGoal);
        context.facade.weeklyGoal.set(GOAL);
        const element = getElement(renderWeeklyPage());
        expect(getButton(element, `${selector} button`).textContent).toContain('WEEKLY_CHECK_IN.GOAL.EDIT');
        getButton(element, `${selector} button`).click();
        expect(getGoalDialogData(context.dialog).goal).toEqual(selector === CURRENT_GOAL ? currentGoal : GOAL);
        expect(element.querySelector(`${CURRENT_GOAL} .check-in__focus-count`)?.textContent).toContain('2 / 3');
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-count`)?.textContent).toContain('2 / 5');
    });

    it('forwards dialog saves and reloads goals when the successful dialog closes', async () => {
        const element = getElement(renderWeeklyPage());
        getButton(element, `${NEXT_GOAL} button`).click();
        const payload: UpsertWeeklyGoalPayload = {
            weekStart: NEXT_WEEK_START,
            targetDays: 7,
            reminderEnabled: true,
            reminderTime: '20:30',
            timeZoneOffsetMinutes: 240,
        };
        await expect(getGoalDialogData(context.dialog).saveGoalAsync(payload)).resolves.toEqual(GOAL);
        expect(context.facade.saveGoalAsync).toHaveBeenCalledExactlyOnceWith(payload);
        expect(context.facade.reloadGoal).not.toHaveBeenCalled();
        context.closed.next(GOAL);
        await waitForAsyncTasksAsync();
        expect(context.facade.reloadGoal).toHaveBeenCalledExactlyOnceWith();
    });

    it.each([null, undefined])('does not reload after a dismissed dialog (%s)', async result => {
        const element = getElement(renderWeeklyPage());
        getButton(element, `${CURRENT_GOAL} button`).click();
        context.closed.next(result);
        await waitForAsyncTasksAsync();
        expect(context.facade.saveGoalAsync).not.toHaveBeenCalled();
        expect(context.facade.reloadGoal).not.toHaveBeenCalled();
    });

    it('returns save failures to the dialog without reloading existing goals', async () => {
        const failure = new Error('Goal save failed');
        context.facade.saveGoalAsync.mockRejectedValueOnce(failure);
        const element = getElement(renderWeeklyPage());
        getButton(element, `${CURRENT_GOAL} button`).click();
        const payload: UpsertWeeklyGoalPayload = {
            weekStart: CURRENT_WEEK_START,
            targetDays: 5,
            reminderEnabled: false,
            reminderTime: null,
            timeZoneOffsetMinutes: null,
        };
        await expect(getGoalDialogData(context.dialog).saveGoalAsync(payload)).rejects.toBe(failure);
        expect(context.facade.reloadGoal).not.toHaveBeenCalled();
    });
});

describe('WeeklyCheckInPageComponent independent goal errors', () => {
    it.each([
        { selector: CURRENT_GOAL, error: 'hasSelectedWeekGoalError', retry: 'retrySelectedWeekGoal' },
        { selector: NEXT_GOAL, error: 'hasGoalError', retry: 'retryNextWeekGoal' },
    ] as const)('retries only the failed goal in $selector', ({ selector, error, retry }) => {
        context.facade[error].set(true);
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        expect(element.querySelector(`${selector} .check-in__focus-title`)).toBeNull();
        expect(element.querySelector('.check-in__summary')).not.toBeNull();
        getButton(element, `${selector} fd-error-state button`).click();
        expect(context.facade[retry]).toHaveBeenCalledExactlyOnceWith();
        expect(context.facade.retryData).not.toHaveBeenCalled();
        const otherRetry = retry === 'retryNextWeekGoal' ? 'retrySelectedWeekGoal' : 'retryNextWeekGoal';
        expect(context.facade[otherRetry]).not.toHaveBeenCalled();
        context.facade[error].set(false);
        fixture.detectChanges();
        expect(element.querySelector(`${selector} fd-error-state`)).toBeNull();
        expect(element.querySelector(`${selector} .check-in__focus-title`)).not.toBeNull();
    });

    it.each([
        { selector: CURRENT_GOAL, loading: 'isSelectedWeekGoalLoading' },
        { selector: NEXT_GOAL, loading: 'isGoalLoading' },
    ] as const)('blocks opening a goal while $loading', ({ selector, loading }) => {
        context.facade[loading].set(true);
        const fixture = renderWeeklyPage();
        const element = getElement(fixture);
        const button = getButton(element, `${selector} button`);
        expect(button.disabled).toBe(true);
        button.click();
        expect(context.dialog.open).not.toHaveBeenCalled();
        context.facade[loading].set(false);
        fixture.detectChanges();
        button.click();
        expect(context.dialog.open).toHaveBeenCalledOnce();
    });
});

describe('WeeklyCheckInPageComponent closed goal periods', () => {
    it('disables past selected-week edits while leaving next-week goal creation available', () => {
        context.facade.isSelectedWeekPast.set(true);
        const element = getElement(renderWeeklyPage());
        const currentButton = getButton(element, `${CURRENT_GOAL} button`);
        expect(currentButton.disabled).toBe(true);
        expect(currentButton.textContent).toContain('WEEKLY_CHECK_IN.GOAL.CLOSED_ACTION');
        currentButton.click();
        expect(context.dialog.open).not.toHaveBeenCalled();
        getButton(element, `${NEXT_GOAL} button`).click();
        expect(context.dialog.open).toHaveBeenCalledOnce();
    });

    it('hides suggested progress and disables goal creation after the next goal period closes', () => {
        context.facade.isGoalPeriodClosed.set(true);
        const element = getElement(renderWeeklyPage());
        const button = getButton(element, `${NEXT_GOAL} button`);
        expect(button.disabled).toBe(true);
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-title`)?.textContent).toContain('WEEKLY_CHECK_IN.GOAL.CLOSED_TITLE');
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-count`)?.parentElement?.hidden).toBe(true);
        button.click();
        expect(context.dialog.open).not.toHaveBeenCalled();
    });

    it('retains achieved progress and closed copy for an existing goal after its period closes', () => {
        context.facade.isSelectedWeekPast.set(true);
        context.facade.isGoalPeriodClosed.set(true);
        context.facade.weeklyGoal.set(GOAL);
        context.facade.selectedWeekGoal.set({ ...GOAL, weekStart: CURRENT_WEEK_START });
        const element = getElement(renderWeeklyPage());
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-count`)?.parentElement?.hidden).toBe(false);
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-count`)?.textContent).toContain('2 / 5');
        expect(element.querySelector(`${NEXT_GOAL} .check-in__focus-copy`)?.textContent).toContain('WEEKLY_CHECK_IN.GOAL.CLOSED_COPY');
        expect(element.querySelector(`${CURRENT_GOAL} .check-in__focus-copy`)?.textContent).toContain('WEEKLY_CHECK_IN.GOAL.CLOSED_COPY');
        expect(element.querySelectorAll(`${NEXT_GOAL} .check-in__focus-segment--filled`)).toHaveLength(GOAL.progressDays);
    });
});
