import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import type { WeightHistoryPageSummary } from '../models/weight-entry.data';
import { WeightHistoryFacade } from './weight-history.facade';

const FIXTURE_LOCALIZED_DECIMAL = 75.5;
const FIXTURE_YEAR = 2026;
const FIXTURE_RANGE_END_DAY = 7;

const TARGET_WEIGHT = 70;
const UPDATED_TARGET_WEIGHT = 69;
const EXPECTED_BMI = 22.9;
const UPDATED_ENTRY_WEIGHT = 73.8;
const LATEST_WEIGHT = 74.2;
const INDEPENDENT_LATEST_WEIGHT = 72.6;

let facade: WeightHistoryFacade;
let measurements: MeasurementSystemService;
let weightEntriesService: {
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
    getWeightGoal: ReturnType<typeof vi.fn>;
    getWeightGoalHistoryPage: ReturnType<typeof vi.fn>;
    getWeightGoalHistory: ReturnType<typeof vi.fn>;
    getInfo: ReturnType<typeof vi.fn>;
    updateWeightGoal: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    weightEntriesService = createWeightEntriesServiceMock();
    userService = {
        getWeightGoal: vi
            .fn()
            .mockReturnValue(of({ desiredWeightKg: TARGET_WEIGHT, startWeightKg: 75, startedAtUtc: '2026-01-01T00:00:00Z' })),
        getWeightGoalHistoryPage: vi.fn().mockReturnValue(of({ items: [], nextCursor: null })),
        getWeightGoalHistory: vi.fn().mockReturnValue(of([])),
        getInfo: vi.fn().mockReturnValue(of({ heightCm: 180 })),
        updateWeightGoal: vi
            .fn()
            .mockReturnValue(of({ desiredWeightKg: UPDATED_TARGET_WEIGHT, startWeightKg: 75, startedAtUtc: '2026-01-01T00:00:00Z' })),
    };

    TestBed.configureTestingModule({
        providers: [
            WeightHistoryFacade,
            { provide: WeightEntriesService, useValue: weightEntriesService },
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

    facade = TestBed.inject(WeightHistoryFacade);
    measurements = TestBed.inject(MeasurementSystemService);
    measurements.setSystem('metric');
});

describe('WeightHistoryFacade entry history pagination', () => {
    it.each([undefined, '2026-03-29'])('loads entry history only on demand with date cursor %s', dateTo => {
        facade.initialize();
        TestBed.tick();
        expect(weightEntriesService.getHistoryPage).not.toHaveBeenCalled();
        const recentEntries = facade.entries();
        const historyEntries = [{ id: 'older-entry', userId: 'user-1', date: '2026-03-01T00:00:00Z', weightKg: 80 }];
        weightEntriesService.getHistoryPage.mockReturnValueOnce(of(historyEntries));
        const next = vi.fn();

        facade.getEntryHistoryPage(dateTo).subscribe(next);

        expect(weightEntriesService.getHistoryPage).toHaveBeenCalledExactlyOnceWith(dateTo);
        expect(next).toHaveBeenCalledExactlyOnceWith(historyEntries);
        expect(facade.entries()).toEqual(recentEntries);
    });

    it('propagates history loading errors so the dialog can offer a retry', () => {
        const failure = new Error('History request failed');
        weightEntriesService.getHistoryPage.mockReturnValueOnce(throwError(() => failure));
        const next = vi.fn();
        const error = vi.fn();

        facade.getEntryHistoryPage('2026-03-29').subscribe({ next, error });

        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledExactlyOnceWith(failure);
    });
});

describe('WeightHistoryFacade loading', () => {
    it('loads entries, summary, desired weight, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        expect(weightEntriesService.getPageSummary).toHaveBeenCalledWith(expect.objectContaining({ entriesLimit: 6 }));
        expect(weightEntriesService.getEntries).not.toHaveBeenCalled();
        expect(weightEntriesService.getLatest).not.toHaveBeenCalled();
        expect(weightEntriesService.getSummary).not.toHaveBeenCalled();
        expect(userService.getWeightGoal).not.toHaveBeenCalled();
        expect(userService.getWeightGoalHistory).not.toHaveBeenCalled();
        expect(userService.getInfo).not.toHaveBeenCalled();
        expect(facade.entries()).toHaveLength(2);
        expect(facade.summaryPoints()).toHaveLength(1);
        expect(facade.desiredWeightKg()).toBe(TARGET_WEIGHT);
        expect(facade.latestWeight()).toBe(LATEST_WEIGHT);
        expect(facade.formModel().weight).toBe(LATEST_WEIGHT.toString());
        expect(facade.bmiViewModel()?.value).toBe(EXPECTED_BMI);
    });

    it('exposes the newest completed goal and keeps the empty-history state explicit', () => {
        expect(facade.lastCompletedWeightGoal()).toBeNull();

        weightEntriesService.getPageSummary.mockReturnValue(
            of({
                ...createWeightPageSummary(),
                goalHistory: [
                    {
                        id: 'latest',
                        targetWeightKg: 75,
                        startWeightKg: 113,
                        endWeightKg: 113,
                        startedAtUtc: '2026-08-06T10:00:00Z',
                        endedAtUtc: '2026-08-06T11:00:00Z',
                        status: 'Cancelled' as const,
                    },
                    {
                        id: 'older',
                        targetWeightKg: 74,
                        startWeightKg: 112,
                        endWeightKg: 111,
                        startedAtUtc: '2026-07-01T10:00:00Z',
                        endedAtUtc: '2026-07-02T10:00:00Z',
                        status: 'Replaced' as const,
                    },
                ],
            }),
        );

        facade.initialize();
        TestBed.tick();

        expect(facade.lastCompletedWeightGoal()?.id).toBe('latest');
    });

    it('keeps current weight independent from the entries selected for the chart range', () => {
        weightEntriesService.getPageSummary.mockReturnValue(of(createWeightPageSummary(INDEPENDENT_LATEST_WEIGHT)));

        facade.initialize();
        TestBed.tick();

        expect(facade.entriesDescending()[0]?.weightKg).toBe(INDEPENDENT_LATEST_WEIGHT);
        expect(facade.latestWeight()).toBe(INDEPENDENT_LATEST_WEIGHT);
        expect(facade.latestWeightDate()).toBe('2026-05-10T00:00:00Z');
    });
});

describe('WeightHistoryFacade measurement boundary', () => {
    it('converts imperial form input to the canonical API value', () => {
        measurements.setSystem('imperial');
        facade.formModel.set({ date: '2026-04-02', weight: '162.7' });

        facade.submit();

        expect(weightEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            weightKg: UPDATED_ENTRY_WEIGHT,
        });
    });
});

describe('WeightHistoryFacade entries', () => {
    it('submits a new entry and reloads the list', async () => {
        facade.initialize();
        TestBed.tick();
        weightEntriesService.getPageSummary.mockClear();
        weightEntriesService.getSummary.mockClear();

        facade.formModel.set({
            date: '2026-04-02',
            weight: '73.8',
        });

        facade.submit();

        expect(weightEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            weightKg: UPDATED_ENTRY_WEIGHT,
        });
        await vi.waitFor(() => {
            expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        });
    });

    it('does not submit invalid form', () => {
        facade.formModel.set({
            date: '',
            weight: '',
        });

        facade.submit();

        expect(weightEntriesService.create).not.toHaveBeenCalled();
        expect(weightEntriesService.update).not.toHaveBeenCalled();
        expect(facade.form().touched()).toBe(true);
    });

    it('shows duplicate date error when entry already exists', async () => {
        weightEntriesService.create.mockReturnValueOnce(throwError(() => ({ error: { error: 'WeightEntry.AlreadyExists' } })));
        facade.formModel.set({
            date: '2026-04-02',
            weight: '73.8',
        });

        facade.submit();

        await vi.waitFor(() => {
            expect(facade.entryError()).toBe('WEIGHT_HISTORY.ERROR_DUPLICATE_DATE');
        });
        expect(weightEntriesService.getEntries).not.toHaveBeenCalled();
        expect(weightEntriesService.getSummary).not.toHaveBeenCalled();
    });

    it('switches to edit mode and updates the existing entry', async () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 };

        facade.startEdit(entry);
        facade.submit();

        await vi.waitFor(() => {
            expect(facade.isEditing()).toBe(false);
        });
        expect(weightEntriesService.update).toHaveBeenCalledWith('entry-1', {
            date: '2026-04-01T00:00:00.000Z',
            weightKg: 74.2,
        });
    });

    it('cancels editing and restores latest weight in the form', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 };
        facade.entries.set([entry, { id: 'entry-2', userId: 'user-1', date: '2026-05-01T00:00:00Z', weightKg: 73.1 }]);
        facade.latestEntry.set({ id: 'entry-2', userId: 'user-1', date: '2026-05-01T00:00:00Z', weightKg: 73.1 });

        facade.startEdit(entry);
        facade.cancelEdit();

        expect(facade.isEditing()).toBe(false);
        expect(facade.formModel().weight).toBe('73.1');
    });

    it('deletes entry and exits edit mode when edited entry is removed', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 };
        facade.startEdit(entry);

        facade.deleteEntry(entry);

        expect(weightEntriesService.remove).toHaveBeenCalledWith('entry-1');
        expect(facade.isEditing()).toBe(false);
        expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
    });
});

describe('WeightHistoryFacade ranges', () => {
    it('keeps recent entries independent from the selected chart range', () => {
        facade.initialize();
        TestBed.tick();
        const recentEntries = facade.entries();
        weightEntriesService.getPageSummary.mockClear();

        facade.changeRange('quarter');
        TestBed.tick();

        expect(weightEntriesService.getPageSummary).not.toHaveBeenCalled();
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.entries()).toEqual(recentEntries);
    });

    it('ignores unsupported range values', () => {
        facade.changeRange('decade');

        expect(facade.selectedRange()).toBe('month');
    });

    it('initializes default custom range when custom range is selected', () => {
        facade.changeRange('custom');

        expect(facade.selectedRange()).toBe('custom');
        expect(facade.customRangeModel().range?.start).toBeInstanceOf(Date);
        expect(facade.customRangeModel().range?.end).toBeInstanceOf(Date);
    });
});

describe('WeightHistoryFacade desired weight', () => {
    it('saves desired weight after validation', () => {
        weightEntriesService.getPageSummary.mockReturnValue(
            of({
                ...createWeightPageSummary(),
                goal: { desiredWeightKg: UPDATED_TARGET_WEIGHT, startWeightKg: 75, startedAtUtc: '2026-01-01T00:00:00Z' },
            }),
        );
        facade.desiredWeightModel.set({ weight: `${UPDATED_TARGET_WEIGHT}` });

        facade.saveDesiredWeight();

        expect(userService.updateWeightGoal).toHaveBeenCalledWith(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeightKg()).toBe(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeightModel().weight).toBe(`${UPDATED_TARGET_WEIGHT}`);
    });

    it('cancels the active goal without form validation', () => {
        userService.updateWeightGoal.mockReturnValue(of({ desiredWeightKg: null, startWeightKg: null, startedAtUtc: null }));

        weightEntriesService.getPageSummary.mockReturnValue(
            of({ ...createWeightPageSummary(), goal: { desiredWeightKg: null, startWeightKg: null, startedAtUtc: null } }),
        );
        facade.cancelWeightGoal();

        expect(userService.updateWeightGoal).toHaveBeenCalledWith(null);
        expect(facade.weightGoal()).toEqual({ desiredWeightKg: null, startWeightKg: null, startedAtUtc: null });
        expect(facade.desiredWeightModel().weight).toBe('');
    });
});

function createWeightEntriesServiceMock(): typeof weightEntriesService {
    return {
        getPageSummary: vi.fn().mockReturnValue(of(createWeightPageSummary())),
        getLatest: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 })),
        getHistoryPage: vi.fn().mockReturnValue(of([])),
        getEntries: vi.fn().mockReturnValue(
            of([
                { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 },
                { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', weightKg: 75.1 },
            ]),
        ),
        getSummary: vi
            .fn()
            .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageWeightKg: 74.2 }])),
        create: vi
            .fn()
            .mockReturnValue(of({ id: 'entry-3', userId: 'user-1', date: '2026-04-02T00:00:00Z', weightKg: UPDATED_ENTRY_WEIGHT })),
        update: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weightKg: 74.2 })),
        remove: vi.fn().mockReturnValue(of(void 0)),
    };
}

function createWeightPageSummary(latestWeight = LATEST_WEIGHT): WeightHistoryPageSummary {
    return {
        entries: [
            {
                id: 'entry-1',
                userId: 'user-1',
                date: latestWeight === INDEPENDENT_LATEST_WEIGHT ? '2026-05-10T00:00:00Z' : '2026-04-01T00:00:00Z',
                weightKg: latestWeight,
            },
            { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', weightKg: 75.1 },
        ],
        summary: [{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageWeightKg: 74.2 }],
        heightCm: 180,
        goal: { desiredWeightKg: TARGET_WEIGHT, startWeightKg: 75, startedAtUtc: '2026-01-01T00:00:00Z' },
        goalHistory: [],
    };
}

describe('Facade boundary regressions', () => {
    it('sends localized decimal as finite canonical number', async () => {
        facade.formModel.set({ date: '2026-04-02', weight: '75,5' });
        facade.submit();
        await vi.waitFor(() => {
            expect(weightEntriesService.create).toHaveBeenCalledWith({ date: '2026-04-02T00:00:00.000Z', weightKg: 75.5 });
        });
    });
    it.each(['not-a-date', '2026-02-30', '2026-13-01'])('rejects invalid calendar date %s', date => {
        facade.formModel.set({ date, weight: '75' });
        facade.submit();
        expect(weightEntriesService.create).not.toHaveBeenCalled();
        expect(facade.isSaving()).toBe(false);
    });
    it.each(['abc', '-1', '0', '100000'])('rejects out-of-range entry %s', value => {
        facade.formModel.set({ date: '2026-04-02', weight: value });
        facade.submit();
        expect(weightEntriesService.create).not.toHaveBeenCalled();
        expect(facade.form().touched()).toBe(true);
    });
    it('initializes once and handles an empty page', () => {
        weightEntriesService.getPageSummary.mockReturnValue(
            of({
                ...createWeightPageSummary(),
                entries: [],
                summary: [],
                heightCm: null,
                goal: { desiredWeightKg: null, startWeightKg: null, startedAtUtc: null },
            }),
        );
        facade.initialize();
        facade.initialize();
        TestBed.tick();
        expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
        expect(facade.latestWeight()).toBeNull();
        expect(facade.latestWeightDate()).toBeNull();
        expect(facade.formModel().weight).toBe('');
        expect(facade.bmiViewModel()).toBeNull();
        expect(facade.chartPoints()).toEqual([]);
        expect(facade.hasCompletedWeightGoals()).toBe(false);
    });
    it('keeps pending save open and preserves edit input on failure', async () => {
        const pending = new Subject<unknown>();
        weightEntriesService.update.mockReturnValue(pending);
        facade.startEdit({ id: 'editing', userId: 'u', date: '2026-04-02', weightKg: 75 });
        facade.submit();
        expect(facade.isSaving()).toBe(true);
        expect(facade.entrySaveVersion()).toBe(0);
        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            expect(facade.entryError()).toBe('FORM_ERRORS.UNKNOWN');
        });
        expect(facade.isSaving()).toBe(false);
        expect(facade.isEditing()).toBe(true);
        expect(facade.formModel()).toEqual({ date: '2026-04-02', weight: '75' });
        expect(facade.entrySaveVersion()).toBe(0);
    });
    it('refreshes rolling-month summary after a non-month mutation', async () => {
        facade.initialize();
        TestBed.tick();
        facade.changeRange('year');
        TestBed.tick();
        weightEntriesService.getSummary.mockClear();
        facade.formModel.set({ date: '2026-04-02', weight: '75' });
        facade.submit();
        await vi.waitFor(() => {
            expect(facade.entrySaveVersion()).toBe(1);
        });
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(facade.rollingMonthSummaryPoints()).toEqual(facade.summaryPoints());
    });
});
describe('Facade editing and goal boundaries', () => {
    it('preserves edited value when background page refresh completes', () => {
        const pending = new Subject<ReturnType<typeof createWeightPageSummary>>();
        weightEntriesService.getPageSummary.mockReturnValue(pending);
        facade.initialize();
        facade.startEdit({ id: 'editing', userId: 'u', date: '2026-04-02', weightKg: 80 });
        pending.next(createWeightPageSummary());
        pending.complete();
        TestBed.tick();
        expect(facade.formModel().weight).toBe('80');
        expect(facade.isLoading()).toBe(false);
        expect(facade.isSummaryLoading()).toBe(false);
    });
    it('converts edited canonical data and goal when units change', () => {
        facade.initialize();
        TestBed.tick();
        const entry = { id: 'old-outside-preview', userId: 'u', date: '2020-01-01', weightKg: 90 };
        facade.startEdit(entry);
        measurements.setSystem('imperial');
        TestBed.tick();
        expect(Number(facade.formModel().weight)).toBe(measurements.displayWeight(entry.weightKg));
        expect(Number(facade.desiredWeightModel().weight)).toBe(measurements.displayWeight(facade.desiredWeightKg() ?? 0));
        measurements.setSystem('metric');
        TestBed.tick();
        expect(Number(facade.formModel().weight)).toBe(entry.weightKg);
    });
    it.each(['abc', '-1', '100000'])('does not save invalid goal %s', value => {
        facade.desiredWeightModel.set({ weight: value });
        facade.saveDesiredWeight();
        expect(userService.updateWeightGoal).not.toHaveBeenCalled();
        expect(facade.isDesiredWeightSaving()).toBe(false);
    });
    it('treats blank goal as cancellation and converts comma decimals', () => {
        facade.desiredWeightModel.set({ weight: '  ' });
        facade.saveDesiredWeight();
        expect(userService.updateWeightGoal).toHaveBeenLastCalledWith(null);
        facade.desiredWeightModel.set({ weight: '75,5' });
        facade.saveDesiredWeight();
        expect(userService.updateWeightGoal).toHaveBeenLastCalledWith(FIXTURE_LOCALIZED_DECIMAL);
        expect(facade.desiredWeightSaveVersion()).toBe(2);
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
        const pending = new Subject<ReturnType<typeof createWeightPageSummary>>();
        weightEntriesService.getPageSummary.mockReturnValue(pending);
        facade.initialize();
        expect(facade.isLoading()).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.isLoading()).toBe(false);
    });
});

describe('Form actions, period readiness and goal lifecycle', () => {
    it('saves through the configured Signal Forms action', async () => {
        facade.formModel.set({ date: '2026-04-02', weight: '75,5' });
        await submit(facade.form);
        expect(weightEntriesService.create).toHaveBeenCalledWith({ date: '2026-04-02T00:00:00.000Z', weightKg: FIXTURE_LOCALIZED_DECIMAL });
        expect(facade.entrySaveVersion()).toBe(1);
    });
    it('waits for both custom dates and avoids duplicate requests for the same range', () => {
        facade.initialize();
        TestBed.tick();
        weightEntriesService.getSummary.mockClear();
        facade.selectedRange.set('custom');
        facade.customRangeModel.set({ range: { start: new Date('2026-01-01T12:00:00'), end: null } });
        TestBed.tick();
        expect(weightEntriesService.getSummary).not.toHaveBeenCalled();
        facade.customRangeModel.set({ range: { start: new Date('2026-01-01T12:00:00'), end: new Date('2026-01-07T12:00:00') } });
        TestBed.tick();
        expect(weightEntriesService.getSummary).toHaveBeenCalledOnce();
        facade.customRangeModel.set({ ...facade.customRangeModel() });
        TestBed.tick();
        expect(weightEntriesService.getSummary).toHaveBeenCalledOnce();
    });
    it('keeps cancellation pending until the server acknowledges it', () => {
        facade.initialize();
        TestBed.tick();
        const pending = new Subject<{ desiredWeightKg: null; startWeightKg: null; startedAtUtc: null }>();
        userService.updateWeightGoal.mockReturnValue(pending);
        weightEntriesService.getPageSummary.mockReturnValue(
            of({ ...createWeightPageSummary(), goal: { desiredWeightKg: null, startWeightKg: null, startedAtUtc: null } }),
        );
        facade.cancelWeightGoal();
        expect(facade.isDesiredWeightSaving()).toBe(true);
        expect(facade.desiredWeightSaveVersion()).toBe(0);
        pending.next({ desiredWeightKg: null, startWeightKg: null, startedAtUtc: null });
        pending.complete();
        expect(facade.isDesiredWeightSaving()).toBe(false);
        expect(facade.desiredWeightKg()).toBeNull();
        expect(facade.desiredWeightModel().weight).toBe('');
        expect(facade.desiredWeightSaveVersion()).toBe(1);
        expect(userService.getWeightGoalHistory).not.toHaveBeenCalled();
        expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(2);
    });
    it('identifies completed goals without treating the active goal as completed', () => {
        const active = {
            id: 'active',
            targetWeightKg: 75,
            startWeightKg: 80,
            endWeightKg: null,
            startedAtUtc: '2026-01-01',
            endedAtUtc: null,
            status: 'Active' as const,
        };
        const completed = { ...active, id: 'completed', status: 'Cancelled' as const, endedAtUtc: '2026-02-01' };
        facade.weightGoalHistory.set([active]);
        expect(facade.hasCompletedWeightGoals()).toBe(false);
        expect(facade.lastCompletedWeightGoal()).toBeNull();
        facade.weightGoalHistory.set([active, completed]);
        expect(facade.hasCompletedWeightGoals()).toBe(true);
        expect(facade.lastCompletedWeightGoal()).toEqual(completed);
    });
    it('resets editing when the edited record is deleted', () => {
        const entry = { id: 'editing', userId: 'u', date: '2026-04-02', weightKg: 80 };
        facade.startEdit(entry);
        facade.deleteEntry(entry);
        expect(facade.isEditing()).toBe(false);
        expect(weightEntriesService.remove).toHaveBeenCalledWith(entry.id);
    });
});

describe('Summary and display resets', () => {
    it('refreshes the rolling month when returning from another period', () => {
        facade.initialize();
        TestBed.tick();
        facade.changeRange('year');
        TestBed.tick();
        const points = [{ startDate: '2026-04-01', endDate: '2026-04-01', averageWeightKg: 80 }];
        weightEntriesService.getSummary.mockReturnValue(of(points));
        facade.changeRange('month');
        TestBed.tick();
        expect(facade.rollingMonthSummaryPoints()).toEqual(points);
        expect(facade.summaryPoints()).toEqual(points);
    });
    it('clears edit input when no latest measurement exists', () => {
        facade.cancelEdit();
        expect(facade.formModel().weight).toBe('');
        measurements.setSystem('imperial');
        TestBed.tick();
        expect(facade.formModel().weight).toBe('');
    });
});

it('forwards history cursors only when requested by the dialog', () => {
    facade.initialize();
    expect(userService.getWeightGoalHistoryPage).not.toHaveBeenCalled();
    facade.getGoalHistoryPage('next').subscribe();
    expect(userService.getWeightGoalHistoryPage).toHaveBeenCalledWith('next');
});
