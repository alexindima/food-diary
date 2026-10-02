import { TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { finalize, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ExportService } from '../../../shared/api/export.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
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

const NOTE_LIMIT = 1024;
const CERVICAL_LIMIT = 128;
const TEMPERATURE_MIN = 34;
const TEMPERATURE_MAX = 42;
const EMOJI_NOTE_LIMIT = NOTE_LIMIT / 2;
const LOGGED_CYCLE_DAYS = 4;
const FRACTIONAL_DAY = 0.5;
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
    updateConsent: ReturnType<typeof vi.fn<CyclesService['updateConsent']>>;
    updateMenstrualEpisode: ReturnType<typeof vi.fn<CyclesService['updateMenstrualEpisode']>>;
};
let exportService: {
    exportCycle: ReturnType<typeof vi.fn<ExportService['exportCycle']>>;
    exportSensitiveCycle: ReturnType<typeof vi.fn<ExportService['exportSensitiveCycle']>>;
};

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
        updateConsent: vi.fn<CyclesService['updateConsent']>().mockReturnValue(of(createCycleResponse())),
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
        exportSensitiveCycle: vi.fn<ExportService['exportSensitiveCycle']>().mockReturnValue(of(void 0)),
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

describe('CycleTrackingFacade fertility validation', () => {
    it.each([TEMPERATURE_MIN - 1, TEMPERATURE_MAX + 1, Number.NaN, Number.POSITIVE_INFINITY])(
        'rejects invalid temperature %s before sending a day',
        async temperature => {
            facade.initialize();
            setValidDayForm();
            facade.dayModel.update(value => ({ ...value, basalBodyTemperatureCelsius: temperature }));
            expect(facade.dayForm.basalBodyTemperatureCelsius().invalid()).toBe(true);
            await submit(facade.dayForm);
            expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        },
    );

    it.each([null, TEMPERATURE_MIN, TEMPERATURE_MAX])('accepts optional and boundary temperatures %s', async temperature => {
        facade.initialize();
        setValidDayForm();
        facade.dayModel.update(value => ({ ...value, basalBodyTemperatureCelsius: temperature }));
        expect(facade.dayForm.basalBodyTemperatureCelsius().invalid()).toBe(false);
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
    });

    it('rejects overlong fluid and sends a corrected trimmed boundary', async () => {
        facade.initialize();
        setValidDayForm();
        facade.dayModel.update(value => ({ ...value, cervicalFluid: 'x'.repeat(CERVICAL_LIMIT + 1) }));
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        facade.dayModel.update(value => ({ ...value, cervicalFluid: `  ${'x'.repeat(CERVICAL_LIMIT)}  ` }));
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
        expect(cyclesService.upsertDay.mock.calls[0]?.[1].fertilitySignal?.cervicalFluid).toBe('x'.repeat(CERVICAL_LIMIT));
    });
});

describe('CycleTrackingFacade loading recovery', () => {
    it('exposes failed initial loading and recovers on a new request', () => {
        cyclesService.getCurrent.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        facade.initialize();
        expect(facade.loadError()).toBe('CYCLE_TRACKING.LOAD_FAILED');
        expect(facade.isLoading()).toBe(false);
        expect(facade.cycle()).toBeNull();
        facade.initialize();
        expect(facade.loadError()).toBeNull();
        expect(facade.cycle()?.id).toBe('cycle-1');
    });

    it('retains the loaded profile and nutrition summary on reload failure', () => {
        facade.initialize();
        const cycle = facade.cycle();
        const nutrition = facade.nutritionSummary();
        cyclesService.getCurrent.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        facade.initialize();
        expect(facade.loadError()).toBe('CYCLE_TRACKING.LOAD_FAILED');
        expect(facade.cycle()).toEqual(cycle);
        expect(facade.nutritionSummary()).toEqual(nutrition);
    });

    it('ignores duplicate loading while the initial request is pending', () => {
        const pending = new Subject<CycleResponse | null>();
        cyclesService.getCurrent.mockReturnValue(pending);
        facade.initialize();
        facade.initialize();
        expect(cyclesService.getCurrent).toHaveBeenCalledOnce();
        pending.next(null);
        pending.complete();
        expect(facade.isLoading()).toBe(false);
        expect(facade.loadError()).toBeNull();
        expect(facade.cycle()).toBeNull();
    });
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

        await expect(facade.deleteCycleAsync()).resolves.toBeUndefined();
        expect(facade.settingsError()).toBe('CYCLE_TRACKING.DELETE_CYCLE_FAILED');

        expect(facade.cycle()?.id).toBe('cycle-1');
        expect(facade.nutritionSummary()).not.toBeNull();
        expect(facade.isDeletingCycle()).toBe(false);
    });

    it('creates a new cycle from form values', async () => {
        facade.startCycleModel.set({
            trackingStartDate: '2026-04-03',
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: null,
            averagePeriodLength: null,
            lutealLength: null,
            isRegular: true,
            showFertilityEstimates: false,
            discreetNotifications: false,
            goal: 0,
            reproductiveState: 0,
            hideFromDashboard: false,
            cycleTrackingConsentGranted: true,
            nutritionInsightsConsentGranted: true,
            fertilitySignalsConsentGranted: false,
        });

        facade.startCycle();

        expect(cyclesService.create).toHaveBeenCalledWith({
            trackingStartDate: '2026-04-03',
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: false,
            discreetNotifications: false,
            goal: 0,
            reproductiveState: 0,
            hideFromDashboard: false,
            cycleTrackingConsentGranted: true,
            nutritionInsightsConsentGranted: true,
            fertilitySignalsConsentGranted: false,
        });
        await vi.waitFor(() => {
            expect(facade.cycle()?.id).toBe('cycle-2');
        });
    });

    it('submits the start cycle form through Signal Forms submission', async () => {
        facade.startCycleModel.update(value => ({
            ...value,
            trackingStartDate: '2026-04-03',
            cycleTrackingConsentGranted: true,
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
        expect(payload.date).toBe('2026-04-02');
        expect(payload.bleeding).toEqual({
            type: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_MEDIUM,
            painImpact: 5,
            clearNotes: false,
        });
        expect(payload.notes).toBe('note');
        expect(payload.clearNotes).toBe(false);
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

describe('CycleTrackingFacade day save recovery', () => {
    it('retains a rejected draft and allows an unchanged retry', async () => {
        facade.initialize();
        setValidDayForm();
        const draft = facade.dayModel();
        cyclesService.upsertDay.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        await submit(facade.dayForm);
        expect(facade.dayModel()).toEqual(draft);
        expect(facade.isSavingDay()).toBe(false);
        expect(facade.daySaveRevision()).toBe(0);
        expect(facade.dayError()).toBe('CYCLE_TRACKING.SAVE_DAY_FAILED');
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).toHaveBeenCalledTimes(2);
        expect(facade.daySaveRevision()).toBe(1);
        expect(facade.dayError()).toBeNull();
    });

    it('locks the draft against duplicate saves, cancellation and editing while pending', async () => {
        facade.initialize();
        setValidDayForm();
        const pending = new Subject<CycleLogDay>();
        cyclesService.upsertDay.mockReturnValue(pending);
        const draft = facade.dayModel();
        facade.saveDay();
        expect(facade.dayForm.notes().disabled()).toBe(true);
        facade.saveDay();
        facade.cancelDayEdit();
        facade.editDay('2026-04-01');
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
        expect(facade.dayModel()).toEqual(draft);
        pending.error(new Error('unavailable'));
        await vi.waitFor(() => {
            expect(facade.isSavingDay()).toBe(false);
        });
    });

    it('retains a saved day when refreshing predictions fails and permits retry', async () => {
        facade.initialize();
        setValidDayForm();
        cyclesService.getCurrent.mockReturnValueOnce(throwError(() => new Error('refresh unavailable')));
        await submit(facade.dayForm);
        expect(facade.bleedingEntries()).toHaveLength(1);
        expect(facade.isSavingDay()).toBe(false);
        expect(facade.daySaveRevision()).toBe(0);
        expect(facade.dayError()).toBe('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
        await submit(facade.dayForm);
        expect(facade.daySaveRevision()).toBe(1);
    });

    it('recognizes the HTTP client empty fallback as a failed refresh after saving', async () => {
        facade.initialize();
        setValidDayForm();
        cyclesService.getCurrent.mockReturnValueOnce(of(null));
        await submit(facade.dayForm);
        expect(facade.bleedingEntries()).toHaveLength(1);
        expect(facade.dayError()).toBe('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
        expect(facade.daySaveRevision()).toBe(0);
        expect(facade.isSavingDay()).toBe(false);
    });
});

describe('CycleTrackingFacade day note validation', () => {
    it('retains an oversized day draft without saving through either entrypoint', async () => {
        facade.initialize();
        setValidDayForm();
        const notes = 'я'.repeat(NOTE_LIMIT + 1);
        facade.dayModel.update(value => ({ ...value, notes }));
        await submit(facade.dayForm);
        facade.saveDay();
        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        expect(facade.dayForm.notes().invalid()).toBe(true);
        expect(facade.dayForm.notes().touched()).toBe(true);
        expect(facade.dayModel().notes).toBe(notes);
        expect(facade.daySaveRevision()).toBe(0);
    });

    it.each([
        ['Cyrillic limit', 'я'.repeat(NOTE_LIMIT)],
        ['outer Unicode whitespace', `\u0085 ${'я'.repeat(NOTE_LIMIT)} \u0085`],
        ['emoji limit', '🙂'.repeat(EMOJI_NOTE_LIMIT)],
        ['whitespace only', ' '.repeat(NOTE_LIMIT + 1)],
    ])('saves a server-valid %s day note', async (_label, notes) => {
        facade.initialize();
        setValidDayForm();
        facade.dayModel.update(value => ({ ...value, notes }));
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
    });

    it('allows correcting a rejected day note and saving once', async () => {
        facade.initialize();
        setValidDayForm();
        facade.dayModel.update(value => ({ ...value, notes: `${'🙂'.repeat(EMOJI_NOTE_LIMIT)}x` }));
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        facade.dayModel.update(value => ({ ...value, notes: 'corrected day note' }));
        await submit(facade.dayForm);
        expect(cyclesService.upsertDay).toHaveBeenCalledOnce();
        expect(cyclesService.upsertDay).toHaveBeenCalledWith('cycle-1', expect.objectContaining({ notes: 'corrected day note' }));
    });
});

describe('CycleTrackingFacade duration validation', () => {
    it.each(['averageCycleLength', 'averagePeriodLength', 'lutealLength'] as const)(
        'rejects fractional %s when starting tracking',
        async field => {
            facade.startCycleModel.update(model => ({
                ...model,
                cycleTrackingConsentGranted: true,
                [field]: (model[field] ?? 0) + FRACTIONAL_DAY,
            }));
            expect(await submit(facade.startCycleForm)).toBe(false);
            expect(cyclesService.create).not.toHaveBeenCalled();
        },
    );
});

describe('CycleTrackingFacade settings cancellation', () => {
    it('discards every cancelled preference and clears validation and request feedback', () => {
        facade.initialize();
        const saved = facade.settingsModel();
        facade.settingsModel.update(model => ({
            ...model,
            averageCycleLength: null,
            isRegular: !model.isRegular,
            hideFromDashboard: !model.hideFromDashboard,
            nutritionInsightsConsentGranted: !model.nutritionInsightsConsentGranted,
            fertilitySignalsConsentGranted: !model.fertilitySignalsConsentGranted,
        }));
        facade.settingsForm.averageCycleLength().markAsTouched();
        facade.settingsError.set('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        facade.cancelSettingsEdit();
        expect(facade.settingsModel()).toEqual(saved);
        expect(facade.settingsForm.averageCycleLength().touched()).toBe(false);
        expect(facade.settingsForm().invalid()).toBe(false);
        expect(facade.settingsError()).toBeNull();
        expect(cyclesService.updateSettings).not.toHaveBeenCalled();
        expect(cyclesService.updateConsent).not.toHaveBeenCalled();
    });

    it.each(['isSavingSettings', 'isDeletingCycle'] as const)('does not discard a draft while %s', operation => {
        facade.initialize();
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: 30 }));
        const draft = facade.settingsModel();
        facade[operation].set(true);
        facade.cancelSettingsEdit();
        expect(facade.settingsModel()).toEqual(draft);
    });

    it('resets to the newest persisted settings after a later draft is cancelled', async () => {
        facade.initialize();
        const persisted = { ...createCycleResponse(), averageCycleLength: 30 };
        cyclesService.updateSettings.mockReturnValue(of(persisted));
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: persisted.averageCycleLength }));
        await submit(facade.settingsForm);
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: null }));
        facade.cancelSettingsEdit();
        expect(facade.settingsModel().averageCycleLength).toBe(persisted.averageCycleLength);
    });
});

describe('CycleTrackingFacade settings operations', () => {
    it('does not delete the cycle during an unresolved settings save', async () => {
        facade.initialize();
        const pending = new Subject<CycleResponse>();
        cyclesService.updateSettings.mockReturnValue(pending);
        const saving = submit(facade.settingsForm);
        await facade.deleteCycleAsync();
        expect(cyclesService.deleteCycle).not.toHaveBeenCalled();
        expect(facade.settingsForm().disabled()).toBe(true);
        pending.next(createCycleResponse());
        pending.complete();
        await saving;
        expect(facade.settingsForm().disabled()).toBe(false);
    });

    it('does not save settings during unresolved deletion', async () => {
        facade.initialize();
        const pending = new Subject<void>();
        cyclesService.deleteCycle.mockReturnValue(pending);
        const deleting = facade.deleteCycleAsync();
        await submit(facade.settingsForm);
        expect(cyclesService.updateSettings).not.toHaveBeenCalled();
        pending.next();
        pending.complete();
        await deleting;
        expect(facade.cycle()).toBeNull();
    });

    it('shows deletion failure and allows retry without losing the cycle', async () => {
        facade.initialize();
        cyclesService.deleteCycle.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        await expect(facade.deleteCycleAsync()).resolves.toBeUndefined();
        expect(facade.settingsError()).toBe('CYCLE_TRACKING.DELETE_CYCLE_FAILED');
        expect(facade.cycle()?.id).toBe('cycle-1');
        expect(facade.isDeletingCycle()).toBe(false);
        await facade.deleteCycleAsync();
        expect(cyclesService.deleteCycle).toHaveBeenCalledTimes(2);
        expect(facade.settingsError()).toBeNull();
        expect(facade.cycle()).toBeNull();
    });

    it('retains confirmed settings when a later consent request fails', async () => {
        facade.initialize();
        const persisted = { ...createCycleResponse(), averageCycleLength: 30 };
        cyclesService.updateSettings.mockReturnValue(of(persisted));
        cyclesService.updateConsent.mockReturnValue(throwError(() => new Error('unavailable')));
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: 30, nutritionInsightsConsentGranted: true }));
        await submit(facade.settingsForm);
        expect(facade.cycle()?.averageCycleLength).toBe(persisted.averageCycleLength);
        expect(facade.settingsSaveRevision()).toBe(0);
        expect(facade.settingsError()).toBe('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        facade.cancelSettingsEdit();
        expect(facade.settingsModel().averageCycleLength).toBe(persisted.averageCycleLength);
        expect(facade.settingsModel().nutritionInsightsConsentGranted).toBe(false);
    });
});

describe('CycleTrackingFacade settings saving', () => {
    it.each(['averageCycleLength', 'averagePeriodLength', 'lutealLength'] as const)(
        'rejects fractional and empty %s before sending settings',
        async field => {
            facade.initialize();
            const original = facade.settingsModel()[field] ?? 0;
            for (const value of [original + FRACTIONAL_DAY, null]) {
                facade.settingsModel.update(model => ({ ...model, [field]: value }));
                expect(await submit(facade.settingsForm)).toBe(false);
                expect(facade.settingsForm[field]().invalid()).toBe(true);
            }
            expect(cyclesService.updateSettings).not.toHaveBeenCalled();
            expect(facade.settingsSaveRevision()).toBe(0);
        },
    );

    it('retains the draft and permits an unchanged retry after a failed save', async () => {
        facade.initialize();
        facade.settingsModel.update(model => ({ ...model, averageCycleLength: 30 }));
        const draft = facade.settingsModel();
        cyclesService.updateSettings.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        await submit(facade.settingsForm);
        expect(facade.settingsError()).toBe('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        expect(facade.settingsModel()).toEqual(draft);
        expect(facade.settingsSaveRevision()).toBe(0);
        expect(facade.isSavingSettings()).toBe(false);
        expect(facade.settingsForm().invalid()).toBe(false);
        await submit(facade.settingsForm);
        expect(cyclesService.updateSettings).toHaveBeenCalledTimes(2);
        expect(facade.settingsError()).toBeNull();
        expect(facade.settingsSaveRevision()).toBe(1);
    });

    it('publishes a successful settings save for drawer orchestration', async () => {
        facade.initialize();

        const success = await submit(facade.settingsForm);

        expect(success).toBe(true);
        expect(cyclesService.updateSettings).toHaveBeenCalledOnce();
        expect(facade.settingsSaveRevision()).toBe(1);
    });
});

describe('CycleTrackingFacade day clear recovery', () => {
    it('refreshes episode history and predictions after clearing a day', () => {
        facade.initialize();
        cyclesService.getCurrent.mockReturnValue(of({ ...createCycleResponse(), menstrualEpisodes: [], predictions: null }));

        facade.clearDay('2026-04-02');

        expect(cyclesService.getCurrent).toHaveBeenCalledTimes(2);
        expect(facade.cycle()?.menstrualEpisodes).toEqual([]);
        expect(facade.cycle()?.predictions).toBeNull();
    });

    it('keeps mutation guards active until the cleared profile has refreshed', () => {
        facade.initialize();
        setValidDayForm();
        const refreshed = new Subject<CycleResponse | null>();
        cyclesService.getCurrent.mockReturnValue(refreshed);

        facade.clearDay('2026-04-02');
        facade.clearDay('2026-04-02');
        facade.saveDay();

        expect(facade.clearingDayDate()).toBe('2026-04-02');
        expect(cyclesService.clearDay).toHaveBeenCalledOnce();
        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        refreshed.next({ ...createCycleResponse(), menstrualEpisodes: [], predictions: null });
        refreshed.complete();
        expect(facade.clearingDayDate()).toBeNull();
    });

    it('offers a read retry when the day was cleared but refreshing failed', () => {
        facade.initialize();
        cyclesService.getCurrent.mockReturnValueOnce(throwError(() => new Error('unavailable')));

        facade.clearDay('2026-04-02');

        expect(facade.bleedingEntries()).toEqual([]);
        expect(facade.dayClearError()).toBeNull();
        expect(facade.loadError()).toBe('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
        expect(facade.clearingDayDate()).toBeNull();
        cyclesService.getCurrent.mockReturnValue(
            of({ ...createCycleResponse(), bleedingEntries: [], menstrualEpisodes: [], predictions: null }),
        );
        facade.initialize();
        expect(facade.loadError()).toBeNull();
        expect(facade.cycle()?.menstrualEpisodes).toEqual([]);
        expect(cyclesService.clearDay).toHaveBeenCalledOnce();
    });

    it('retains all records on failed clear and removes them only after a successful retry', () => {
        cyclesService.getCurrent.mockReturnValue(of({ ...createCycleResponse(), dayNotes: [{ date: '2026-04-02', notes: 'saved note' }] }));
        facade.initialize();
        const original = facade.cycle();
        cyclesService.clearDay.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        facade.clearDay('2026-04-02');
        expect(facade.dayClearError()).toBe('CYCLE_TRACKING.CLEAR_DAY_FAILED');
        expect(facade.cycle()).toEqual(original);
        expect(facade.clearingDayDate()).toBeNull();
        facade.clearDay('2026-04-02');
        expect(cyclesService.clearDay).toHaveBeenCalledTimes(2);
        expect(facade.dayClearError()).toBeNull();
        expect(facade.cycle()?.dayNotes).toEqual([]);
    });

    it('prevents duplicate clear, save and edit operations while clearing', () => {
        facade.initialize();
        setValidDayForm();
        const draft = facade.dayModel();
        const pending = new Subject<void>();
        cyclesService.clearDay.mockReturnValue(pending);
        facade.clearDay('2026-04-02');
        facade.clearDay('2026-04-02');
        facade.saveDay();
        facade.editDay('2026-04-01');
        expect(cyclesService.clearDay).toHaveBeenCalledOnce();
        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
        expect(facade.dayModel()).toEqual(draft);
        pending.next();
        pending.complete();
        expect(facade.clearingDayDate()).toBeNull();
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

        expect(cyclesService.clearDay).toHaveBeenCalledWith('cycle-1', '2026-04-02');
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
            startDate: '2026-04-01',
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
            factorId: 'factor-1',
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-01',
            notes: 'pill',
            clearNotes: false,
        });
        expect(typeof payload.endDate).toBe('string');
    });
});

describe('CycleTrackingFacade factor drafts', () => {
    it('rejects an oversized edited note without losing the draft or sending a request', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        const notes = 'я'.repeat(NOTE_LIMIT + 1);
        facade.factorModel.update(value => ({ ...value, notes }));
        await submit(facade.factorForm);
        expect(facade.factorForm.notes().invalid()).toBe(true);
        expect(facade.factorForm.notes().touched()).toBe(true);
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
        expect(facade.factorModel().notes).toBe(notes);
        expect(facade.editingFactorId()).toBe('factor-1');
    });

    it.each([
        ['limit', 'a'.repeat(NOTE_LIMIT)],
        ['trimmed limit', `  ${'я'.repeat(NOTE_LIMIT)}  `],
        ['Unicode whitespace at the limit', `\u0085${'я'.repeat(NOTE_LIMIT)}\u0085`],
        ['empty whitespace', ' '.repeat(NOTE_LIMIT + 1)],
        ['emoji limit', '🙂'.repeat(EMOJI_NOTE_LIMIT)],
    ])('accepts %s notes that the server accepts', async (_label, notes) => {
        facade.initialize();
        facade.factorModel.update(value => ({ ...value, notes }));
        expect(facade.factorForm.notes().invalid()).toBe(false);
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
    });

    it('allows correcting a rejected draft and then saves once', async () => {
        facade.initialize();
        facade.factorModel.update(value => ({ ...value, notes: `${'🙂'.repeat(EMOJI_NOTE_LIMIT)}x` }));
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
        facade.factorModel.update(value => ({ ...value, notes: 'valid note' }));
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
        expect(cyclesService.upsertFactor).toHaveBeenCalledWith('cycle-1', expect.objectContaining({ notes: 'valid note' }));
    });
});

describe('CycleTrackingFacade factor drafts', () => {
    it('sends the selected id when changing the start date', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        facade.factorModel.update(value => ({ ...value, startDate: '2026-04-02' }));
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).toHaveBeenCalledWith(
            'cycle-1',
            expect.objectContaining({ factorId: 'factor-1', startDate: '2026-04-02' }),
        );
    });

    it('retains the draft and explains an identity conflict', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        facade.factorModel.update(value => ({ ...value, startDate: '2026-04-02', notes: 'draft' }));
        cyclesService.upsertFactor.mockReturnValue(throwError(() => ({ error: { error: 'Cycle.FactorIdentityConflict' } })));
        await submit(facade.factorForm);
        expect(facade.factorError()).toBe('CYCLE_TRACKING.FACTOR_IDENTITY_CONFLICT');
        expect(facade.editingFactorId()).toBe('factor-1');
        expect(facade.factorModel().notes).toBe('draft');
    });

    it('discards a cancelled draft and resets field interaction state', () => {
        facade.initialize();
        facade.editFactor('factor-1');
        facade.factorModel.update(value => ({ ...value, notes: 'cancelled', startDate: null }));
        facade.factorForm().markAsTouched();

        facade.cancelFactorEdit();

        expect(facade.editingFactorId()).toBeNull();
        expect(facade.factorModel().notes).toBeNull();
        expect(facade.factorModel().startDate).not.toBeNull();
        expect(facade.factorForm().touched()).toBe(false);
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
    });

    it('clears an existing note when the editor is emptied', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        facade.factorModel.update(value => ({ ...value, notes: '   ' }));

        await submit(facade.factorForm);

        expect(cyclesService.upsertFactor).toHaveBeenCalledWith('cycle-1', expect.objectContaining({ clearNotes: true }));
    });

    it('resets the new-factor form after a successful save', async () => {
        facade.initialize();
        facade.factorModel.update(value => ({ ...value, notes: 'saved note' }));

        await submit(facade.factorForm);

        expect(facade.factorModel().notes).toBeNull();
        expect(facade.editingFactorId()).toBeNull();
    });

    it('rejects an end date before the start date without sending a request', async () => {
        facade.initialize();
        facade.factorModel.update(value => ({ ...value, startDate: '2026-04-02', endDate: '2026-04-01' }));

        expect(facade.factorForm().invalid()).toBe(true);
        expect(await submit(facade.factorForm)).toBe(false);
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade factor save lifecycle', () => {
    it('retains a failed draft and resolves submission so the user can retry', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        facade.factorModel.update(value => ({ ...value, notes: 'retry note' }));
        cyclesService.upsertFactor.mockReturnValueOnce(throwError(() => new Error('offline')));

        await expect(submit(facade.factorForm)).resolves.toBe(true);

        expect(facade.isSavingFactor()).toBe(false);
        expect(facade.editingFactorId()).toBe('factor-1');
        expect(facade.factorModel().notes).toBe('retry note');
        expect(facade.factorError()).toBe('CYCLE_TRACKING.SAVE_FACTOR_FAILED');
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).toHaveBeenCalledTimes(2);
        expect(facade.factorError()).toBeNull();
    });

    it('keeps the pending editor stable and prevents overlapping saves', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        const pending = new Subject<CycleResponse>();
        cyclesService.upsertFactor.mockReturnValue(pending);
        const submission = submit(facade.factorForm);

        facade.cancelFactorEdit();
        facade.editFactor('missing-factor');
        facade.saveFactor();

        expect(facade.editingFactorId()).toBe('factor-1');
        expect(facade.factorForm.notes().disabled()).toBe(true);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
        pending.next(createCycleResponse());
        pending.complete();
        await submission;
    });
});

describe('CycleTrackingFacade ending factors', () => {
    it('does not send an invalid end date for a factor starting tomorrow', async () => {
        const now = new Date();
        const tomorrow = formatDateInputValue(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1));
        const cycle = createCycleResponse();
        cyclesService.getCurrent.mockReturnValue(of({ ...cycle, factors: [{ ...cycle.factors[0], startDate: tomorrow }] }));
        facade.initialize();
        await facade.endFactorTodayAsync('factor-1');
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
        expect(facade.factorError()).toBeNull();
    });

    it('does not repeat ending a factor that already ends today', async () => {
        const cycle = createCycleResponse();
        cyclesService.getCurrent.mockReturnValue(
            of({ ...cycle, factors: [{ ...cycle.factors[0], endDate: formatDateInputValue(new Date()) }] }),
        );
        facade.initialize();
        await facade.endFactorTodayAsync('factor-1');
        expect(cyclesService.upsertFactor).not.toHaveBeenCalled();
    });

    it('shows a recoverable error and leaves the factor unchanged when ending fails', async () => {
        facade.initialize();
        cyclesService.upsertFactor.mockReturnValueOnce(throwError(() => new Error('offline')));

        await facade.endFactorTodayAsync('factor-1');

        expect(facade.factorError()).toBe('CYCLE_TRACKING.END_FACTOR_FAILED');
        expect(facade.factors()[0].endDate).toBeNull();
        expect(facade.isSavingFactor()).toBe(false);
        await facade.endFactorTodayAsync('factor-1');
        expect(cyclesService.upsertFactor).toHaveBeenCalledTimes(2);
        expect(facade.factorError()).toBeNull();
    });

    it('protects the editor and prevents saving while a factor is being ended', async () => {
        facade.initialize();
        facade.editFactor('factor-1');
        const pending = new Subject<CycleResponse>();
        cyclesService.upsertFactor.mockReturnValue(pending);
        const ending = facade.endFactorTodayAsync('factor-1');

        facade.cancelFactorEdit();
        facade.saveFactor();

        expect(facade.editingFactorId()).toBe('factor-1');
        expect(facade.factorForm.notes().disabled()).toBe(true);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
        pending.next(createCycleResponse());
        pending.complete();
        await ending;
        expect(facade.editingFactorId()).toBeNull();
        expect(facade.factorModel().notes).toBeNull();
    });

    it('accepts the same start and end day', async () => {
        facade.initialize();
        facade.factorModel.update(value => ({ ...value, startDate: '2026-04-01', endDate: '2026-04-01' }));

        expect(facade.factorForm().invalid()).toBe(false);
        await submit(facade.factorForm);
        expect(cyclesService.upsertFactor).toHaveBeenCalledOnce();
    });

    it('does not clear an existing note through a blank new-factor form', async () => {
        facade.initialize();

        await submit(facade.factorForm);

        expect(cyclesService.upsertFactor).toHaveBeenCalledWith('cycle-1', expect.objectContaining({ clearNotes: false }));
    });
});

describe('CycleTrackingFacade menstrual episodes', () => {
    it('toggles prediction exclusion and applies the returned cycle', async () => {
        facade.initialize();

        await facade.toggleMenstrualEpisodePredictionAsync('episode-1');

        expect(cyclesService.updateMenstrualEpisode).toHaveBeenCalledWith('cycle-1', 'episode-1', {
            startDate: '2026-04-01',
            endDate: '2026-04-05',
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
    it('shows export failures and clears feedback when retrying', () => {
        facade.initialize();
        exportService.exportCycle.mockReturnValueOnce(throwError(() => new Error('offline')));

        facade.exportCycle();

        expect(facade.exportError()).toBe('CYCLE_TRACKING.EXPORT_FAILED');
        expect(facade.isExportingCycle()).toBe(false);
        facade.exportCycle();
        expect(facade.exportError()).toBeNull();
    });

    it('shows private export feedback when password verification fails', () => {
        facade.initialize();
        exportService.exportSensitiveCycle.mockReturnValueOnce(throwError(() => new Error('invalid password')));

        facade.exportSensitiveCycle('invalid-password');

        expect(facade.exportError()).toBe('CYCLE_TRACKING.SENSITIVE_EXPORT_FAILED');
        expect(facade.isExportingCycle()).toBe(false);
        facade.exportSensitiveCycle('correct-password');
        expect(facade.exportError()).toBeNull();
    });

    it('exports the current cycle from tracking start to today', () => {
        facade.initialize();

        facade.exportCycle();

        const request = exportService.exportCycle.mock.calls[0][0];
        expect(request.dateFrom).toBe('2026-04-01');
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
        goal: 0,
        reproductiveState: 0,
        hideFromDashboard: false,
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

it('cancels initial loading when the facade scope is destroyed and ignores a late response', () => {
    const pending = new Subject<CycleResponse | null>();
    const cancelled = vi.fn();
    cyclesService.getCurrent.mockReturnValue(pending.pipe(finalize(cancelled)));
    facade.initialize();
    expect(facade.isLoading()).toBe(true);

    TestBed.resetTestingModule();
    pending.next(createCycleResponse());

    expect(cancelled).toHaveBeenCalledOnce();
    expect(facade.isLoading()).toBe(false);
    expect(facade.cycle()).toBeNull();
});

describe('Cycle day notes without clinical entries', () => {
    it('sends a notes-only day, restores it for editing and explicitly clears it', async () => {
        cyclesService.upsertDay.mockReturnValue(
            of({
                cycleProfileId: 'cycle-1',
                date: '2026-04-02',
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: null,
                notes: 'Quiet day',
            }),
        );
        facade.initialize();
        facade.dayModel.update(value => ({ ...value, date: '2026-04-02', notes: '  Quiet day  ' }));
        facade.saveDay();
        await vi.waitFor(() => {
            expect(facade.daySaveRevision()).toBe(1);
        });
        expect(cyclesService.upsertDay).toHaveBeenCalledWith(
            'cycle-1',
            expect.objectContaining({
                notes: 'Quiet day',
                clearNotes: false,
                bleeding: null,
                symptoms: [],
                fertilitySignal: null,
            }),
        );
        facade.editDay('2026-04-02');
        expect(facade.dayModel().notes).toBe('Quiet day');
        cyclesService.upsertDay.mockReturnValue(
            of({
                cycleProfileId: 'cycle-1',
                date: '2026-04-02',
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: null,
                notes: null,
            }),
        );
        facade.dayModel.update(value => ({ ...value, notes: '' }));
        facade.saveDay();
        await vi.waitFor(() => {
            expect(facade.daySaveRevision()).toBe(2);
        });
        expect(cyclesService.upsertDay).toHaveBeenLastCalledWith(
            'cycle-1',
            expect.objectContaining({ notes: undefined, clearNotes: true }),
        );
        expect(facade.cycle()?.dayNotes).toEqual([]);
    });
    it('loads a persisted note and removes it when clearing the day', () => {
        cyclesService.getCurrent.mockReturnValue(of({ ...createCycleResponse(), dayNotes: [{ date: '2026-04-02', notes: 'Persisted' }] }));
        facade.initialize();
        facade.editDay('2026-04-02');
        expect(facade.dayModel().notes).toBe('Persisted');
        facade.clearDay('2026-04-02');
        expect(facade.cycle()?.dayNotes).toEqual([]);
    });
});
