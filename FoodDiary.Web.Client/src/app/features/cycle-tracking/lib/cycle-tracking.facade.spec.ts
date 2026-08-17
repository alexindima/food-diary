import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { of, throwError } from 'rxjs';
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
const SEVERE_SYMPTOM_INTENSITY = 9;

let facade: CycleTrackingFacade;
let cyclesService: {
    clearDay: ReturnType<typeof vi.fn<CyclesService['clearDay']>>;
    create: ReturnType<typeof vi.fn<CyclesService['create']>>;
    deleteCycle: ReturnType<typeof vi.fn<CyclesService['deleteCycle']>>;
    deleteMenstrualEpisode: ReturnType<typeof vi.fn<CyclesService['deleteMenstrualEpisode']>>;
    getCurrent: ReturnType<typeof vi.fn<CyclesService['getCurrent']>>;
    getNutritionSummary: ReturnType<typeof vi.fn<CyclesService['getNutritionSummary']>>;
    upsertDay: ReturnType<typeof vi.fn<CyclesService['upsertDay']>>;
    upsertFactor: ReturnType<typeof vi.fn<CyclesService['upsertFactor']>>;
    updateSettings: ReturnType<typeof vi.fn<CyclesService['updateSettings']>>;
    updateMenstrualEpisode: ReturnType<typeof vi.fn<CyclesService['updateMenstrualEpisode']>>;
};
let exportService: { exportCycle: ReturnType<typeof vi.fn<ExportService['exportCycle']>> };

beforeEach(() => {
    cyclesService = {
        getCurrent: vi.fn<CyclesService['getCurrent']>().mockReturnValue(of(createCycleResponse())),
        getNutritionSummary: vi.fn<CyclesService['getNutritionSummary']>().mockReturnValue(of(createNutritionSummary())),
        clearDay: vi.fn<CyclesService['clearDay']>().mockReturnValue(of(void 0)),
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
        deleteCycle: vi.fn<CyclesService['deleteCycle']>().mockReturnValue(of(void 0)),
        deleteMenstrualEpisode: vi
            .fn<CyclesService['deleteMenstrualEpisode']>()
            .mockReturnValue(of({ ...createCycleResponse(), menstrualEpisodes: [] })),
        upsertDay: vi.fn<CyclesService['upsertDay']>().mockReturnValue(of(createCycleLogDay())),
        updateSettings: vi.fn<CyclesService['updateSettings']>().mockReturnValue(of(createCycleResponse())),
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
        updateMenstrualEpisode: vi.fn<CyclesService['updateMenstrualEpisode']>().mockReturnValue(
            of({
                ...createCycleResponse(),
                menstrualEpisodes: [
                    {
                        id: 'episode-1',
                        cycleProfileId: 'cycle-1',
                        startDate: '2026-04-01T00:00:00.000Z',
                        endDate: '2026-04-05T00:00:00.000Z',
                        status: 1,
                        excludedFromPredictions: true,
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

    it('deletes the current cycle and returns to the empty state', async () => {
        facade.initialize();

        await facade.deleteCycleAsync();

        expect(cyclesService.deleteCycle).toHaveBeenCalledWith('cycle-1');
        expect(facade.cycle()).toBeNull();
        expect(facade.nutritionSummary()).toBeNull();
        expect(facade.isDeletingCycle()).toBe(false);
    });

    it('keeps the current cycle when deletion fails', async () => {
        cyclesService.deleteCycle.mockReturnValueOnce(throwError(() => new Error('delete failed')));
        facade.initialize();

        await expect(facade.deleteCycleAsync()).rejects.toThrow('delete failed');

        expect(facade.cycle()?.id).toBe('cycle-1');
        expect(facade.nutritionSummary()).not.toBeNull();
        expect(facade.isDeletingCycle()).toBe(false);
    });

    it('creates a new cycle from form values', async () => {
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
        await vi.waitFor(() => {
            expect(facade.cycle()?.id).toBe('cycle-2');
        });
    });

    it('submits the start cycle form through Signal Forms submission', async () => {
        facade.startCycleModel.update(value => ({
            ...value,
            trackingStartDate: '2026-04-03',
        }));

        const success = await submit(facade.startCycleForm);

        expect(success).toBe(true);
        expect(cyclesService.create).toHaveBeenCalledOnce();
    });

    it('marks start cycle form as touched when invalid', () => {
        facade.startCycleModel.update(value => ({ ...value, trackingStartDate: null }));

        facade.startCycle();

        expect(cyclesService.create).not.toHaveBeenCalled();
        expect(facade.startCycleForm.trackingStartDate().touched()).toBe(true);
    });
});

describe('CycleTrackingFacade day saving', () => {
    it('upserts a day and merges it into the current profile', async () => {
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
        await vi.waitFor(() => {
            expect(facade.bleedingEntries()).toHaveLength(1);
        });
        expect(facade.bleedingEntries()[0].id).toBe('bleeding-1');
        expect(facade.daySaveRevision()).toBe(1);
        expect(cyclesService.getNutritionSummary).toHaveBeenCalledTimes(2);
    });

    it('submits the day form through Signal Forms submission', async () => {
        facade.initialize();
        setValidDayForm();

        const success = await submit(facade.dayForm);

        expect(success).toBe(true);
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
    });

    it('does not save a day when current cycle is missing', () => {
        facade.saveDay();

        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade settings saving', () => {
    it('publishes a successful settings save for drawer orchestration', async () => {
        facade.initialize();

        const success = await submit(facade.settingsForm);

        expect(success).toBe(true);
        expect(cyclesService.updateSettings).toHaveBeenCalledOnce();
        expect(facade.settingsSaveRevision()).toBe(1);
    });
});

describe('CycleTrackingFacade day editing', () => {
    it('discards unsaved day changes when editing is cancelled', () => {
        facade.dayModel.update(value => ({ ...value, nausea: SEVERE_SYMPTOM_INTENSITY, notes: 'unsaved' }));

        facade.cancelDayEdit();

        expect(facade.dayModel().nausea).toBe(0);
        expect(facade.dayModel().notes).toBeNull();
        expect(facade.editingDayDate()).toBeNull();
    });

    it('clears a day and removes its logs from current profile', () => {
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
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
                fertilitySignals: [
                    {
                        id: 'signal-1',
                        cycleProfileId: 'cycle-1',
                        date: '2026-04-02T00:00:00.000Z',
                        basalBodyTemperatureCelsius: 36.62,
                        ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
                        cervicalFluid: 'egg white',
                        hadSex: true,
                        notes: null,
                    },
                ],
            }),
        );
        facade.initialize();

        facade.clearDay('2026-04-02T00:00:00.000Z');

        expect(cyclesService.clearDay).toHaveBeenCalledWith('cycle-1', '2026-04-02T00:00:00.000Z');
        expect(facade.bleedingEntries()).toEqual([]);
        expect(facade.symptoms()).toEqual([]);
        expect(facade.fertilitySignals()).toEqual([]);
        expect(cyclesService.getNutritionSummary).toHaveBeenCalledTimes(2);
    });
});

describe('CycleTrackingFacade day form editing', () => {
    it('loads an existing day into the day form for editing', () => {
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                bleedingEntries: [createBleedingEntry('bleeding-1', '2026-04-02T00:00:00.000Z')],
                symptoms: [
                    {
                        id: 'symptom-1',
                        cycleProfileId: 'cycle-1',
                        date: '2026-04-02T00:00:00.000Z',
                        category: 1,
                        intensity: 4,
                        tags: [],
                        note: null,
                    },
                ],
                fertilitySignals: [
                    {
                        id: 'signal-1',
                        cycleProfileId: 'cycle-1',
                        date: '2026-04-02T00:00:00.000Z',
                        basalBodyTemperatureCelsius: 36.62,
                        ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
                        cervicalFluid: 'egg white',
                        hadSex: true,
                        notes: null,
                    },
                ],
            }),
        );
        facade.initialize();

        facade.editDay('2026-04-02T00:00:00.000Z');

        expect(facade.editingDayDate()).toBe('2026-04-02T00:00:00.000Z');
        expect(facade.dayModel()).toMatchObject({
            date: '2026-04-02',
            isBleeding: true,
            pain: 5,
            mood: 4,
            basalBodyTemperatureCelsius: 36.62,
            ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
            cervicalFluid: 'egg white',
            hadSex: true,
            notes: 'note',
        });
    });
});

describe('CycleTrackingFacade bleeding editing', () => {
    it('requests scoped bleeding removal when editing a day with bleeding turned off', async () => {
        const date = '2026-04-02T00:00:00.000Z';
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                bleedingEntries: [createBleedingEntry('bleeding-1', date)],
            }),
        );
        cyclesService.upsertDay.mockReturnValue(
            of({
                cycleProfileId: 'cycle-1',
                date,
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: null,
            }),
        );
        facade.initialize();
        facade.editDay(date);
        facade.dayModel.update(value => ({ ...value, isBleeding: false }));

        facade.saveDay();

        expect(cyclesService.upsertDay.mock.calls[0][1]).toMatchObject({
            bleeding: null,
            clearBleeding: true,
        });
        await vi.waitFor(() => {
            expect(facade.bleedingEntries()).toEqual([]);
        });
    });
});

describe('CycleTrackingFacade fertility editing', () => {
    it('requests scoped fertility removal and preserves other day observations', async () => {
        const date = '2026-04-02T00:00:00.000Z';
        const bleeding = createBleedingEntry('bleeding-1', date);
        const symptom = {
            id: 'symptom-1',
            cycleProfileId: 'cycle-1',
            date,
            category: 1 as const,
            intensity: 4,
            tags: [],
            note: null,
        };
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                bleedingEntries: [bleeding],
                symptoms: [symptom],
                fertilitySignals: [
                    {
                        id: 'signal-1',
                        cycleProfileId: 'cycle-1',
                        date,
                        basalBodyTemperatureCelsius: 36.62,
                        ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
                        cervicalFluid: 'egg white',
                        hadSex: true,
                        notes: null,
                    },
                ],
            }),
        );
        cyclesService.upsertDay.mockReturnValue(
            of({
                cycleProfileId: 'cycle-1',
                date,
                bleedingEntries: [bleeding],
                symptoms: [symptom],
                fertilitySignal: null,
            }),
        );
        facade.initialize();
        facade.editDay(date);
        facade.dayModel.update(value => ({
            ...value,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: false,
        }));

        facade.saveDay();

        expect(cyclesService.upsertDay.mock.calls[0][1]).toMatchObject({
            fertilitySignal: null,
            clearFertilitySignal: true,
        });
        await vi.waitFor(() => {
            expect(facade.fertilitySignals()).toEqual([]);
        });
        expect(facade.bleedingEntries()).toEqual([bleeding]);
        expect(facade.symptoms()).toEqual([symptom]);
    });
});

describe('CycleTrackingFacade symptom values', () => {
    it('requests scoped symptom removal when an existing symptom is reset to zero', () => {
        const date = '2026-04-02T00:00:00.000Z';
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                symptoms: [
                    {
                        id: 'symptom-1',
                        cycleProfileId: 'cycle-1',
                        date,
                        category: 1,
                        intensity: 5,
                        tags: [],
                    },
                    {
                        id: 'symptom-2',
                        cycleProfileId: 'cycle-1',
                        date,
                        category: 2,
                        intensity: 4,
                        tags: [],
                    },
                ],
            }),
        );
        facade.initialize();
        facade.editDay(date);
        facade.dayModel.update(value => ({ ...value, mood: 0 }));

        facade.saveDay();

        const payload = cyclesService.upsertDay.mock.calls[0][1];
        expect(payload.clearSymptomCategories).toEqual([1]);
        expect(payload.symptoms).toContainEqual(expect.objectContaining({ category: 2, intensity: 4 }));
    });

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
            appetite: 0,
            craving: 0,
            bloating: 2,
            headache: 4,
            skin: 0,
            stool: 0,
            nausea: 0,
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
        expect(payload.symptoms).not.toContainEqual(expect.objectContaining({ category: 2 }));
        expect(payload.clearSymptomCategories).toEqual([]);
    });

    it('saves the additional symptom categories supported by the API', () => {
        facade.initialize();
        setValidDayForm();
        facade.dayModel.update(value => ({ ...value, skin: 6, nausea: 9 }));

        facade.saveDay();

        const payload = cyclesService.upsertDay.mock.calls[0][1];
        expect(payload.symptoms).toContainEqual({ category: 8, intensity: 6, tags: [], note: null, clearNote: false });
        expect(payload.symptoms).toContainEqual({ category: 10, intensity: 9, tags: [], note: null, clearNote: false });
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

    it('submits the factor form through Signal Forms submission', async () => {
        facade.initialize();
        facade.factorModel.update(value => ({
            ...value,
            startDate: '2026-04-01',
        }));

        const success = await submit(facade.factorForm);

        expect(success).toBe(true);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
    });

    it('does not save a factor when current cycle is missing', () => {
        facade.saveFactor();

        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
    });

    it('loads a factor into the factor form for editing', () => {
        facade.initialize();

        facade.editFactor('factor-1');

        expect(facade.editingFactorId()).toBe('factor-1');
        expect(facade.factorModel()).toEqual({
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-01',
            endDate: null,
            notes: 'pill',
        });
    });

    it('ends an active factor today', () => {
        facade.initialize();

        facade.endFactorToday('factor-1');

        const payload = cyclesService.upsertFactor.mock.calls[0][1];
        expect(cyclesService.upsertFactor).toHaveBeenCalledWith('cycle-1', payload);
        expect(payload).toMatchObject({
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-01T00:00:00.000Z',
            notes: 'pill',
            clearNotes: false,
        });
        expect(typeof payload.endDate).toBe('string');
    });
});

describe('CycleTrackingFacade menstrual episodes', () => {
    it('toggles prediction exclusion and applies the returned cycle', async () => {
        facade.initialize();

        await facade.toggleMenstrualEpisodePredictionAsync('episode-1');

        expect(cyclesService.updateMenstrualEpisode).toHaveBeenCalledWith('cycle-1', 'episode-1', {
            startDate: '2026-04-01T00:00:00.000Z',
            endDate: '2026-04-05T00:00:00.000Z',
            excludedFromPredictions: true,
        });
        expect(facade.menstrualEpisodes()[0]?.excludedFromPredictions).toBe(true);
    });

    it('deletes a confirmed episode and applies the returned cycle', async () => {
        facade.initialize();

        await facade.deleteMenstrualEpisodeAsync('episode-1');

        expect(cyclesService.deleteMenstrualEpisode).toHaveBeenCalledWith('cycle-1', 'episode-1');
        expect(facade.menstrualEpisodes()).toEqual([]);
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
    it('replaces existing entries by returned date', async () => {
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

        await vi.waitFor(() => {
            expect(facade.bleedingEntries().map(entry => entry.id)).toEqual(['later-entry', 'bleeding-1']);
        });
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
        fertilitySignals: [],
        menstrualEpisodes: [
            {
                id: 'episode-1',
                cycleProfileId: 'cycle-1',
                startDate: '2026-04-01T00:00:00.000Z',
                endDate: '2026-04-05T00:00:00.000Z',
                status: 1,
                excludedFromPredictions: false,
            },
        ],
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
        hasEnoughNutritionData: true,
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
        appetite: 0,
        craving: 0,
        bloating: 1,
        headache: 2,
        skin: 0,
        stool: 0,
        nausea: 0,
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
