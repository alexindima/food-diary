import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ExportService } from '../../../shared/api/export.service';
import { CyclesService } from '../api/cycles.service';
import {
    BLEEDING_TYPE_BLEEDING,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleLogDay,
    type CycleNutritionSummary,
    type CycleResponse,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../models/cycle.data';
import { CycleTrackingFacade } from './cycle-tracking.facade';

const LOGGED_CYCLE_DAYS = 4;

let facade: CycleTrackingFacade;
let cyclesService: {
    create: ReturnType<typeof vi.fn<CyclesService['create']>>;
    getCurrent: ReturnType<typeof vi.fn<CyclesService['getCurrent']>>;
    getNutritionSummary: ReturnType<typeof vi.fn<CyclesService['getNutritionSummary']>>;
    upsertDay: ReturnType<typeof vi.fn<CyclesService['upsertDay']>>;
    upsertFactor: ReturnType<typeof vi.fn<CyclesService['upsertFactor']>>;
};
let exportService: { exportCycle: ReturnType<typeof vi.fn<ExportService['exportCycle']>> };

beforeEach(() => {
    cyclesService = {
        getCurrent: vi.fn<CyclesService['getCurrent']>().mockReturnValue(of(createCycleResponse())),
        getNutritionSummary: vi.fn<CyclesService['getNutritionSummary']>().mockReturnValue(of(createNutritionSummary())),
        create: vi.fn<CyclesService['create']>().mockReturnValue(
            of({
                ...createCycleResponse(),
                id: 'cycle-2',
                trackingStartDate: '2026-04-03T00:00:00Z',
                averageCycleLength: 30,
                averagePeriodLength: 6,
                lutealLength: 15,
                predictions: null,
            }),
        ),
        upsertDay: vi.fn<CyclesService['upsertDay']>().mockReturnValue(of(createCycleLogDay())),
        upsertFactor: vi.fn<CyclesService['upsertFactor']>().mockReturnValue(
            of({
                ...createCycleResponse(),
                factors: [
                    {
                        id: 'factor-1',
                        cycleProfileId: 'cycle-1',
                        type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
                        startDate: '2026-04-01T00:00:00.000Z',
                        endDate: null,
                        notes: 'pill',
                    },
                ],
            }),
        ),
    };
    exportService = {
        exportCycle: vi.fn<ExportService['exportCycle']>().mockReturnValue(of(void 0)),
    };

    TestBed.configureTestingModule({
        providers: [
            CycleTrackingFacade,
            { provide: CyclesService, useValue: cyclesService },
            { provide: ExportService, useValue: exportService },
        ],
    });

    facade = TestBed.inject(CycleTrackingFacade);
});

describe('CycleTrackingFacade current cycle', () => {
    it('loads current cycle on initialize', () => {
        facade.initialize();

        expect(cyclesService.getCurrent).toHaveBeenCalledTimes(1);
        expect(facade.cycle()?.id).toBe('cycle-1');
        expect(cyclesService.getNutritionSummary).toHaveBeenCalledTimes(1);
        expect(facade.nutritionSummary()?.loggedCycleDays).toBe(LOGGED_CYCLE_DAYS);
    });

    it('creates a new cycle from form values', () => {
        facade.startCycleModel.set({
            trackingStartDate: '2026-04-03',
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 15,
            isRegular: true,
            showFertilityEstimates: true,
            discreetNotifications: false,
        });

        facade.startCycle();

        expect(cyclesService.create).toHaveBeenCalledWith({
            trackingStartDate: '2026-04-03T00:00:00.000Z',
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 15,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: false,
        });
        expect(facade.cycle()?.id).toBe('cycle-2');
    });

    it('marks start cycle form as touched when invalid', () => {
        facade.startCycleModel.update(value => ({ ...value, trackingStartDate: null }));

        facade.startCycle();

        expect(cyclesService.create).not.toHaveBeenCalled();
        expect(facade.startCycleForm.trackingStartDate().touched()).toBe(true);
    });
});

describe('CycleTrackingFacade days', () => {
    it('upserts a day and merges it into the current profile', () => {
        facade.initialize();
        setValidDayForm();

        facade.saveDay();

        const payload = cyclesService.upsertDay.mock.calls[0][1];
        expect(cyclesService.upsertDay).toHaveBeenCalledWith('cycle-1', expect.any(Object));
        expect(payload.date).toBe('2026-04-02T00:00:00.000Z');
        expect(payload.bleeding).toEqual({
            type: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_MEDIUM,
            painImpact: 5,
            notes: 'note',
            clearNotes: false,
        });
        expect(payload.symptoms).toContainEqual({ category: 0, intensity: 5, tags: [], note: null, clearNote: false });
        expect(payload.symptoms).toContainEqual({ category: 1, intensity: 3, tags: [], note: null, clearNote: false });
        expect(payload.symptoms).toContainEqual({ category: 3, intensity: 6, tags: [], note: null, clearNote: false });
        expect(payload.fertilitySignal).toEqual({
            basalBodyTemperatureCelsius: 36.62,
            ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
            cervicalFluid: 'egg white',
            hadSex: true,
            notes: undefined,
            clearNotes: false,
        });
        expect(facade.bleedingEntries()).toHaveLength(1);
        expect(facade.bleedingEntries()[0].id).toBe('bleeding-1');
    });

    it('does not save a day when current cycle is missing', () => {
        facade.saveDay();

        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade symptom values', () => {
    it('clamps symptom values before saving a day', () => {
        facade.initialize();
        facade.dayModel.set({
            date: '2026-04-02',
            isBleeding: true,
            bleedingType: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_MEDIUM,
            pain: -1,
            mood: 99,
            energy: Number.NaN,
            sleepQuality: 6,
            bloating: 2,
            headache: 4,
            libido: 2,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: false,
            notes: null,
        });

        facade.saveDay();

        const payload = cyclesService.upsertDay.mock.calls[0][1];
        expect(payload.bleeding?.painImpact).toBe(0);
        expect(payload.symptoms).toContainEqual({ category: 1, intensity: 10, tags: [], note: null, clearNote: false });
        expect(payload.symptoms).toContainEqual({ category: 2, intensity: 0, tags: [], note: null, clearNote: false });
    });
});

describe('CycleTrackingFacade factors', () => {
    it('upserts a factor and replaces current cycle state', () => {
        facade.initialize();
        facade.factorModel.set({
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-01',
            endDate: null,
            notes: 'pill',
        });

        facade.saveFactor();

        expect(cyclesService.upsertFactor).toHaveBeenCalledWith('cycle-1', {
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-01T00:00:00.000Z',
            endDate: null,
            notes: 'pill',
            clearNotes: false,
        });
        expect(facade.factors()).toHaveLength(1);
        expect(facade.factors()[0].id).toBe('factor-1');
    });

    it('does not save a factor when current cycle is missing', () => {
        facade.saveFactor();

        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade export', () => {
    it('exports the current cycle from tracking start to today', () => {
        facade.initialize();

        facade.exportCycle();

        const request = exportService.exportCycle.mock.calls[0][0];
        expect(request.dateFrom).toBe(toLocalStartOfDayIso('2026-04-01T00:00:00Z'));
        expect(typeof request.timeZoneOffsetMinutes).toBe('number');
        expect(facade.isExportingCycle()).toBe(false);
    });

    it('skips export when current cycle is missing', () => {
        facade.exportCycle();

        expect(exportService.exportCycle).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade day ordering', () => {
    it('replaces existing entries by returned date', () => {
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                bleedingEntries: [
                    createBleedingEntry('old-entry', '2026-04-02T00:00:00.000Z'),
                    createBleedingEntry('later-entry', '2026-04-03T00:00:00.000Z'),
                ],
                symptoms: [],
                predictions: null,
            }),
        );
        facade.initialize();
        facade.dayModel.update(value => ({ ...value, date: '2026-04-02', isBleeding: true }));

        facade.saveDay();

        expect(facade.bleedingEntries().map(entry => entry.id)).toEqual(['later-entry', 'bleeding-1']);
    });
});

function createCycleResponse(): CycleResponse {
    return {
        id: 'cycle-1',
        userId: 'user-1',
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        confidence: 1,
        trackingStartDate: '2026-04-01T00:00:00Z',
        averageCycleLength: 28,
        averagePeriodLength: 5,
        lutealLength: 14,
        isRegular: true,
        isOnboardingComplete: true,
        showFertilityEstimates: true,
        discreetNotifications: true,
        bleedingEntries: [],
        symptoms: [],
        factors: [],
        fertilitySignals: [],
        predictions: {
            nextPeriodStartFrom: '2026-04-29T00:00:00Z',
            nextPeriodStartTo: '2026-05-01T00:00:00Z',
            ovulationFrom: null,
            ovulationTo: null,
            pmsWindowStart: null,
            pmsWindowEnd: null,
            confidence: 'Moderate',
            rationale: 'Based on recent bleeding entries.',
        },
    };
}

function createCycleLogDay(): CycleLogDay {
    return {
        cycleProfileId: 'cycle-1',
        date: '2026-04-02T00:00:00.000Z',
        bleedingEntries: [createBleedingEntry('bleeding-1', '2026-04-02T00:00:00.000Z')],
        symptoms: [
            {
                id: 'symptom-1',
                cycleProfileId: 'cycle-1',
                date: '2026-04-02T00:00:00.000Z',
                category: 0,
                intensity: 5,
                tags: [],
                note: null,
            },
        ],
        fertilitySignal: null,
    };
}

function createNutritionSummary(): CycleNutritionSummary {
    return {
        dateFrom: '2026-04-01T00:00:00.000Z',
        dateTo: '2026-04-30T23:59:59.999Z',
        loggedCycleDays: LOGGED_CYCLE_DAYS,
        daysWithMeals: 3,
        bleedingDays: 2,
        averageCaloriesOnBleedingDays: 2100,
        averageCaloriesOnNonBleedingCycleDays: 1800,
        averageFiberOnBleedingDays: 18,
        averageFiberOnNonBleedingCycleDays: 28,
        averagePainImpactOnDaysWithMeals: 6,
    };
}

function createBleedingEntry(id: string, date: string): CycleLogDay['bleedingEntries'][number] {
    return {
        id,
        cycleProfileId: 'cycle-1',
        date,
        type: BLEEDING_TYPE_BLEEDING,
        flow: CYCLE_FLOW_MEDIUM,
        painImpact: 5,
        notes: 'note',
    };
}

function setValidDayForm(): void {
    facade.dayModel.set({
        date: '2026-04-02',
        isBleeding: true,
        bleedingType: BLEEDING_TYPE_BLEEDING,
        flow: CYCLE_FLOW_MEDIUM,
        pain: 5,
        mood: 3,
        energy: 4,
        sleepQuality: 6,
        bloating: 1,
        headache: 2,
        libido: 2,
        basalBodyTemperatureCelsius: 36.62,
        ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
        cervicalFluid: 'egg white',
        hadSex: true,
        notes: 'note',
    });
}

function toLocalStartOfDayIso(value: string): string {
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date.toISOString();
}
