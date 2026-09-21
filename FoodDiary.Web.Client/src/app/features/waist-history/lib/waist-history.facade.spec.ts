import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import type { WaistHistoryPageSummary } from '../models/waist-entry.data';
import { WaistHistoryFacade } from './waist-history.facade';

const FIXTURE_LOCALIZED_DECIMAL = 75.5;
const FIXTURE_YEAR = 2026;
const FIXTURE_RANGE_END_DAY = 7;

const TARGET_WAIST = 78;
const UPDATED_TARGET_WAIST = 77;
const EXPECTED_WHTR = 0.46;

let facade: WaistHistoryFacade;
let measurements: MeasurementSystemService;
let waistEntriesService: {
    create: ReturnType<typeof vi.fn>;
    getEntries: ReturnType<typeof vi.fn>;
    getHistoryPage: ReturnType<typeof vi.fn>;
    getLatest: ReturnType<typeof vi.fn>;
    getPageSummary: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};
let userService: {
    getWaistGoal: ReturnType<typeof vi.fn>;
    getWaistGoalHistoryPage: ReturnType<typeof vi.fn>;
    getWaistGoalHistory: ReturnType<typeof vi.fn>;
    getInfo: ReturnType<typeof vi.fn>;
    updateWaistGoal: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    waistEntriesService = createWaistEntriesServiceMock();
    userService = {
        getWaistGoal: vi.fn().mockReturnValue(of({ desiredWaistCm: TARGET_WAIST, startWaistCm: 84, startedAtUtc: '2026-03-01T00:00:00Z' })),
        getWaistGoalHistoryPage: vi.fn().mockReturnValue(of({ items: [], nextCursor: null })),
        getWaistGoalHistory: vi.fn().mockReturnValue(of([])),
        getInfo: vi.fn().mockReturnValue(of({ heightCm: 180 })),
        updateWaistGoal: vi
            .fn()
            .mockReturnValue(of({ desiredWaistCm: UPDATED_TARGET_WAIST, startWaistCm: 82, startedAtUtc: '2026-04-01T00:00:00Z' })),
    };

    TestBed.configureTestingModule({
        providers: [
            WaistHistoryFacade,
            { provide: WaistEntriesService, useValue: waistEntriesService },
            { provide: UserService, useValue: userService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                    getCurrentLang: vi.fn(() => 'en'),
                },
            },
        ],
    });

    facade = TestBed.inject(WaistHistoryFacade);
    measurements = TestBed.inject(MeasurementSystemService);
    measurements.setSystem('metric');
});

describe('WaistHistoryFacade entry history pagination', () => {
    it.each([undefined, '2026-03-29'])('loads entry history only on demand with date cursor %s', dateTo => {
        facade.initialize();
        TestBed.tick();
        expect(waistEntriesService.getHistoryPage).not.toHaveBeenCalled();
        const recentEntries = facade.entries();
        const historyEntries = [{ id: 'older-entry', userId: 'user-1', date: '2026-03-01T00:00:00Z', circumferenceCm: 80 }];
        waistEntriesService.getHistoryPage.mockReturnValueOnce(of(historyEntries));
        const next = vi.fn();

        facade.getEntryHistoryPage(dateTo).subscribe(next);

        expect(waistEntriesService.getHistoryPage).toHaveBeenCalledExactlyOnceWith(dateTo);
        expect(next).toHaveBeenCalledExactlyOnceWith(historyEntries);
        expect(facade.entries()).toEqual(recentEntries);
    });

    it('propagates history loading errors so the dialog can offer a retry', () => {
        const failure = new Error('History request failed');
        waistEntriesService.getHistoryPage.mockReturnValueOnce(throwError(() => failure));
        const next = vi.fn();
        const error = vi.fn();

        facade.getEntryHistoryPage('2026-03-29').subscribe({ next, error });

        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledExactlyOnceWith(failure);
    });
});

describe('WaistHistoryFacade loading', () => {
    it('loads entries, summary, desired waist, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(waistEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getPageSummary).toHaveBeenCalledWith(expect.objectContaining({ entriesLimit: 6 }));
        expect(waistEntriesService.getEntries).not.toHaveBeenCalled();
        expect(waistEntriesService.getLatest).not.toHaveBeenCalled();
        expect(waistEntriesService.getSummary).not.toHaveBeenCalled();
        expect(userService.getWaistGoal).not.toHaveBeenCalled();
        expect(userService.getWaistGoalHistory).not.toHaveBeenCalled();
        expect(userService.getInfo).not.toHaveBeenCalled();
        expect(facade.entries()).toHaveLength(2);
        expect(facade.summaryPoints()).toHaveLength(1);
        expect(facade.desiredWaistCm()).toBe(TARGET_WAIST);
        expect(facade.formModel().circumference).toBe('82');
        expect(facade.whtViewModel()?.value).toBe(EXPECTED_WHTR);
    });
});

describe('WaistHistoryFacade entries', () => {
    it('converts imperial form input to the canonical API value', () => {
        measurements.setSystem('imperial');
        facade.formModel.set({ date: '2026-04-02', circumference: '32.2' });

        facade.submit();

        expect(waistEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            circumferenceCm: 81.79,
        });
    });

    it('submits a new entry and reloads the list', async () => {
        facade.initialize();
        TestBed.tick();
        waistEntriesService.getPageSummary.mockClear();
        waistEntriesService.getSummary.mockClear();

        facade.formModel.set({
            date: '2026-04-02',
            circumference: '81.7',
        });

        facade.submit();

        expect(waistEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            circumferenceCm: 81.7,
        });
        await vi.waitFor(() => {
            expect(waistEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        });
    });

    it('does not submit invalid form', () => {
        facade.formModel.set({
            date: '',
            circumference: '',
        });

        facade.submit();

        expect(waistEntriesService.create).not.toHaveBeenCalled();
        expect(waistEntriesService.update).not.toHaveBeenCalled();
        expect(facade.form().touched()).toBe(true);
    });

    it('shows duplicate date error when entry already exists', async () => {
        waistEntriesService.create.mockReturnValueOnce(throwError(() => ({ error: { error: 'WaistEntry.AlreadyExists' } })));
        facade.formModel.set({
            date: '2026-04-02',
            circumference: '81.7',
        });

        facade.submit();

        await vi.waitFor(() => {
            expect(facade.entryError()).toBe('WAIST_HISTORY.ERROR_DUPLICATE_DATE');
        });
        expect(waistEntriesService.getEntries).not.toHaveBeenCalled();
        expect(waistEntriesService.getSummary).not.toHaveBeenCalled();
    });

    it('switches to edit mode and updates the existing entry', async () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 };

        facade.startEdit(entry);
        facade.submit();

        await vi.waitFor(() => {
            expect(facade.isEditing()).toBe(false);
        });
        expect(waistEntriesService.update).toHaveBeenCalledWith('entry-1', {
            date: '2026-04-01T00:00:00.000Z',
            circumferenceCm: 82,
        });
    });

    it('cancels editing and restores latest circumference in the form', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 };
        facade.entries.set([entry, { id: 'entry-2', userId: 'user-1', date: '2026-05-01T00:00:00Z', circumferenceCm: 80.5 }]);

        facade.startEdit(entry);
        facade.cancelEdit();

        expect(facade.isEditing()).toBe(false);
        expect(facade.formModel().circumference).toBe('80.5');
    });

    it('deletes entry and exits edit mode when edited entry is removed', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 };
        facade.startEdit(entry);

        facade.deleteEntry(entry);

        expect(waistEntriesService.remove).toHaveBeenCalledWith('entry-1');
        expect(facade.isEditing()).toBe(false);
        expect(waistEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
    });
});

describe('WaistHistoryFacade ranges', () => {
    it('keeps recent entries independent from the selected chart range', () => {
        facade.initialize();
        TestBed.tick();
        const recentEntries = facade.entries();
        waistEntriesService.getPageSummary.mockClear();

        facade.changeRange('quarter');
        TestBed.tick();

        expect(waistEntriesService.getPageSummary).not.toHaveBeenCalled();
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.entries()).toEqual(recentEntries);
    });

    it('ignores unsupported range values', () => {
        facade.changeRange('unsupported');

        expect(facade.selectedRange()).toBe('month');
    });

    it('initializes default custom range when custom range is selected', () => {
        facade.changeRange('custom');

        expect(facade.selectedRange()).toBe('custom');
        expect(facade.customRangeModel().range?.start).toBeInstanceOf(Date);
        expect(facade.customRangeModel().range?.end).toBeInstanceOf(Date);
    });
});

describe('WaistHistoryFacade desired waist', () => {
    it('saves desired waist after validation', () => {
        waistEntriesService.getPageSummary.mockReturnValue(
            of({
                ...createWaistPageSummary(),
                goal: { desiredWaistCm: UPDATED_TARGET_WAIST, startWaistCm: 75, startedAtUtc: '2026-01-01T00:00:00Z' },
            }),
        );
        facade.desiredWaistModel.set({ circumference: `${UPDATED_TARGET_WAIST}` });

        facade.saveDesiredWaist();

        expect(userService.updateWaistGoal).toHaveBeenCalledWith(UPDATED_TARGET_WAIST);
        expect(facade.desiredWaistCm()).toBe(UPDATED_TARGET_WAIST);
        expect(facade.desiredWaistModel().circumference).toBe(`${UPDATED_TARGET_WAIST}`);
    });
});

function createWaistEntriesServiceMock(): typeof waistEntriesService {
    return {
        getPageSummary: vi.fn().mockReturnValue(of(createWaistPageSummary())),
        getHistoryPage: vi.fn().mockReturnValue(of([])),
        getEntries: vi.fn().mockReturnValue(
            of([
                { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 },
                { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', circumferenceCm: 83.5 },
            ]),
        ),
        getSummary: vi
            .fn()
            .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageCircumferenceCm: 82 }])),
        getLatest: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 })),
        create: vi.fn().mockReturnValue(of({ id: 'entry-3', userId: 'user-1', date: '2026-04-02T00:00:00Z', circumferenceCm: 81.7 })),
        update: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 })),
        remove: vi.fn().mockReturnValue(of(void 0)),
    };
}

function createWaistPageSummary(): WaistHistoryPageSummary {
    return {
        entries: [
            { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumferenceCm: 82 },
            { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', circumferenceCm: 83.5 },
        ],
        summary: [{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageCircumferenceCm: 82 }],
        heightCm: 180,
        goal: { desiredWaistCm: TARGET_WAIST, startWaistCm: 84, startedAtUtc: '2026-03-01T00:00:00Z' },
        goalHistory: [],
    };
}

describe('Facade boundary regressions', () => {
    it('sends localized decimal as finite canonical number', async () => {
        facade.formModel.set({ date: '2026-04-02', circumference: '75,5' });
        facade.submit();
        await vi.waitFor(() => {
            expect(waistEntriesService.create).toHaveBeenCalledWith({ date: '2026-04-02T00:00:00.000Z', circumferenceCm: 75.5 });
        });
    });
    it.each(['not-a-date', '2026-02-30', '2026-13-01'])('rejects invalid calendar date %s', date => {
        facade.formModel.set({ date, circumference: '75' });
        facade.submit();
        expect(waistEntriesService.create).not.toHaveBeenCalled();
        expect(facade.isSaving()).toBe(false);
    });
    it.each(['abc', '-1', '0', '100000'])('rejects out-of-range entry %s', value => {
        facade.formModel.set({ date: '2026-04-02', circumference: value });
        facade.submit();
        expect(waistEntriesService.create).not.toHaveBeenCalled();
        expect(facade.form().touched()).toBe(true);
    });
    it('initializes once and handles an empty page', () => {
        waistEntriesService.getPageSummary.mockReturnValue(
            of({
                ...createWaistPageSummary(),
                entries: [],
                summary: [],
                heightCm: null,
                goal: { desiredWaistCm: null, startWaistCm: null, startedAtUtc: null },
            }),
        );
        facade.initialize();
        facade.initialize();
        TestBed.tick();
        expect(waistEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        expect(facade.latestWaist()).toBeNull();
        expect(facade.latestWaistDate()).toBeNull();
        expect(facade.formModel().circumference).toBe('');
        expect(facade.whtViewModel()).toBeNull();
        expect(facade.chartPoints()).toEqual([]);
        expect(facade.hasCompletedWaistGoals()).toBe(false);
    });
    it('keeps pending save open and preserves edit input on failure', async () => {
        const pending = new Subject<unknown>();
        waistEntriesService.update.mockReturnValue(pending);
        facade.startEdit({ id: 'editing', userId: 'u', date: '2026-04-02', circumferenceCm: 75 });
        facade.submit();
        expect(facade.isSaving()).toBe(true);
        expect(facade.entrySaveVersion()).toBe(0);
        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            expect(facade.entryError()).toBe('FORM_ERRORS.UNKNOWN');
        });
        expect(facade.isSaving()).toBe(false);
        expect(facade.isEditing()).toBe(true);
        expect(facade.formModel()).toEqual({ date: '2026-04-02', circumference: '75' });
        expect(facade.entrySaveVersion()).toBe(0);
    });
    it('refreshes rolling-month summary after a non-month mutation', async () => {
        facade.initialize();
        TestBed.tick();
        facade.changeRange('year');
        TestBed.tick();
        waistEntriesService.getSummary.mockClear();
        facade.formModel.set({ date: '2026-04-02', circumference: '75' });
        facade.submit();
        await vi.waitFor(() => {
            expect(facade.entrySaveVersion()).toBe(1);
        });
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.rollingMonthSummaryPoints()).toEqual(facade.summaryPoints());
    });
});
describe('Facade editing and goal boundaries', () => {
    it('preserves edited value when background page refresh completes', () => {
        const pending = new Subject<ReturnType<typeof createWaistPageSummary>>();
        waistEntriesService.getPageSummary.mockReturnValue(pending);
        facade.initialize();
        facade.startEdit({ id: 'editing', userId: 'u', date: '2026-04-02', circumferenceCm: 80 });
        pending.next(createWaistPageSummary());
        pending.complete();
        TestBed.tick();
        expect(facade.formModel().circumference).toBe('80');
        expect(facade.isLoading()).toBe(false);
        expect(facade.isSummaryLoading()).toBe(false);
    });
    it('converts edited canonical data and goal when units change', () => {
        facade.initialize();
        TestBed.tick();
        const entry = { id: 'old-outside-preview', userId: 'u', date: '2020-01-01', circumferenceCm: 90 };
        facade.startEdit(entry);
        measurements.setSystem('imperial');
        TestBed.tick();
        expect(Number(facade.formModel().circumference)).toBe(measurements.displayLength(entry.circumferenceCm));
        expect(Number(facade.desiredWaistModel().circumference)).toBe(measurements.displayLength(facade.desiredWaistCm() ?? 0));
        measurements.setSystem('metric');
        TestBed.tick();
        expect(Number(facade.formModel().circumference)).toBe(entry.circumferenceCm);
    });
    it.each(['abc', '-1', '100000'])('does not save invalid goal %s', value => {
        facade.desiredWaistModel.set({ circumference: value });
        facade.saveDesiredWaist();
        expect(userService.updateWaistGoal).not.toHaveBeenCalled();
        expect(facade.isDesiredWaistSaving()).toBe(false);
    });
    it('treats blank goal as cancellation and converts comma decimals', () => {
        facade.desiredWaistModel.set({ circumference: '  ' });
        facade.saveDesiredWaist();
        expect(userService.updateWaistGoal).toHaveBeenLastCalledWith(null);
        facade.desiredWaistModel.set({ circumference: '75,5' });
        facade.saveDesiredWaist();
        expect(userService.updateWaistGoal).toHaveBeenLastCalledWith(FIXTURE_LOCALIZED_DECIMAL);
        expect(facade.desiredWaistSaveVersion()).toBe(2);
    });
    it('preserves a complete custom range and clears it when leaving custom mode', () => {
        const range = { start: new Date(FIXTURE_YEAR, 0, 2), end: new Date(FIXTURE_YEAR, 0, FIXTURE_RANGE_END_DAY) };
        facade.customRangeModel.set({ range });
        facade.changeRange('custom');
        expect(facade.customRangeModel().range).toEqual(range);
        facade.changeRange('week');
        expect(facade.customRangeModel().range).toBeNull();
        expect(facade.currentRange().start).toBeInstanceOf(Date);
    });
    it('releases a pending read on facade destruction', () => {
        const pending = new Subject<ReturnType<typeof createWaistPageSummary>>();
        waistEntriesService.getPageSummary.mockReturnValue(pending);
        facade.initialize();
        expect(facade.isLoading()).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.isLoading()).toBe(false);
    });
});

describe('Form actions, period readiness and goal lifecycle', () => {
    it('saves through the configured Signal Forms action', async () => {
        facade.formModel.set({ date: '2026-04-02', circumference: '75,5' });
        await submit(facade.form);
        expect(waistEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            circumferenceCm: FIXTURE_LOCALIZED_DECIMAL,
        });
        expect(facade.entrySaveVersion()).toBe(1);
    });
    it('waits for both custom dates and avoids duplicate requests for the same range', () => {
        facade.initialize();
        TestBed.tick();
        waistEntriesService.getSummary.mockClear();
        facade.selectedRange.set('custom');
        facade.customRangeModel.set({ range: { start: new Date('2026-01-01T12:00:00'), end: null } });
        TestBed.tick();
        expect(waistEntriesService.getSummary).not.toHaveBeenCalled();
        facade.customRangeModel.set({ range: { start: new Date('2026-01-01T12:00:00'), end: new Date('2026-01-07T12:00:00') } });
        TestBed.tick();
        expect(waistEntriesService.getSummary).toHaveBeenCalledOnce();
        facade.customRangeModel.set({ ...facade.customRangeModel() });
        TestBed.tick();
        expect(waistEntriesService.getSummary).toHaveBeenCalledOnce();
    });
    it('keeps cancellation pending until the server acknowledges it', () => {
        facade.initialize();
        TestBed.tick();
        const pending = new Subject<{ desiredWaistCm: null; startWaistCm: null; startedAtUtc: null }>();
        userService.updateWaistGoal.mockReturnValue(pending);
        waistEntriesService.getPageSummary.mockReturnValue(
            of({ ...createWaistPageSummary(), goal: { desiredWaistCm: null, startWaistCm: null, startedAtUtc: null } }),
        );
        facade.cancelWaistGoal();
        expect(facade.isDesiredWaistSaving()).toBe(true);
        expect(facade.desiredWaistSaveVersion()).toBe(0);
        pending.next({ desiredWaistCm: null, startWaistCm: null, startedAtUtc: null });
        pending.complete();
        expect(facade.isDesiredWaistSaving()).toBe(false);
        expect(facade.desiredWaistCm()).toBeNull();
        expect(facade.desiredWaistModel().circumference).toBe('');
        expect(facade.desiredWaistSaveVersion()).toBe(1);
        expect(userService.getWaistGoalHistory).not.toHaveBeenCalled();
        expect(waistEntriesService.getPageSummary).toHaveBeenCalledTimes(2);
    });
    it('identifies completed goals without treating the active goal as completed', () => {
        const active = {
            id: 'active',
            targetWaistCm: 75,
            startWaistCm: 80,
            endWaistCm: null,
            startedAtUtc: '2026-01-01',
            endedAtUtc: null,
            status: 'Active' as const,
        };
        const completed = { ...active, id: 'completed', status: 'Cancelled' as const, endedAtUtc: '2026-02-01' };
        facade.waistGoalHistory.set([active]);
        expect(facade.hasCompletedWaistGoals()).toBe(false);
        expect(facade.lastCompletedWaistGoal()).toBeNull();
        facade.waistGoalHistory.set([active, completed]);
        expect(facade.hasCompletedWaistGoals()).toBe(true);
        expect(facade.lastCompletedWaistGoal()).toEqual(completed);
    });
    it('resets editing when the edited record is deleted', () => {
        const entry = { id: 'editing', userId: 'u', date: '2026-04-02', circumferenceCm: 80 };
        facade.startEdit(entry);
        facade.deleteEntry(entry);
        expect(facade.isEditing()).toBe(false);
        expect(waistEntriesService.remove).toHaveBeenCalledWith(entry.id);
    });
});

describe('Summary and display resets', () => {
    it('refreshes the rolling month when returning from another period', () => {
        facade.initialize();
        TestBed.tick();
        facade.changeRange('year');
        TestBed.tick();
        const points = [{ startDate: '2026-04-01', endDate: '2026-04-01', averageCircumferenceCm: 80 }];
        waistEntriesService.getSummary.mockReturnValue(of(points));
        facade.changeRange('month');
        TestBed.tick();
        expect(facade.rollingMonthSummaryPoints()).toEqual(points);
        expect(facade.summaryPoints()).toEqual(points);
    });
    it('clears edit input when no latest measurement exists', () => {
        facade.cancelEdit();
        expect(facade.formModel().circumference).toBe('');
        measurements.setSystem('imperial');
        TestBed.tick();
        expect(facade.formModel().circumference).toBe('');
    });
});

it('forwards history cursors only when requested by the dialog', () => {
    facade.initialize();
    expect(userService.getWaistGoalHistoryPage).not.toHaveBeenCalled();
    facade.getGoalHistoryPage('next').subscribe();
    expect(userService.getWaistGoalHistoryPage).toHaveBeenCalledWith('next');
});
