import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import type { WeightHistoryPageSummary } from '../models/weight-entry.data';
import { WeightHistoryFacade } from './weight-history.facade';

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
    getLatest: ReturnType<typeof vi.fn>;
    getPageSummary: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};
let userService: {
    getWeightGoal: ReturnType<typeof vi.fn>;
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

describe('WeightHistoryFacade loading', () => {
    it('loads entries, summary, desired weight, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(weightEntriesService.getPageSummary).toHaveBeenCalledTimes(1);
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
        facade.desiredWeightModel.set({ weight: `${UPDATED_TARGET_WEIGHT}` });

        facade.saveDesiredWeight();

        expect(userService.updateWeightGoal).toHaveBeenCalledWith(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeightKg()).toBe(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeightModel().weight).toBe(`${UPDATED_TARGET_WEIGHT}`);
    });

    it('cancels the active goal without form validation', () => {
        userService.updateWeightGoal.mockReturnValue(of({ desiredWeightKg: null, startWeightKg: null, startedAtUtc: null }));

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
