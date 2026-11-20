import { ChangeDetectionStrategy, Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ExportService } from '../../../shared/api/export.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../shared/development/development-tools';
import {
    BLEEDING_TYPE_BLEEDING,
    CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_MEDIUM,
    CYCLE_REPRODUCTIVE_STATE_CYCLING,
    CYCLE_REPRODUCTIVE_STATE_POSTPARTUM,
    CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
    CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    type CycleConsent,
    type CycleConsentPurpose,
    type CycleFactor,
    type CycleLogDay,
    type CycleResponse,
    MENSTRUAL_EPISODE_STATUS_CONFIRMED,
    MENSTRUAL_EPISODE_STATUS_INFERRED,
} from '../../../shared/models/cycle.data';
import { calendarDate, utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { CurrentLocalDayService } from '../../../shared/platform/current-local-day.service';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { CyclesService } from '../api/cycles.service';
import { MAX_BASAL_BODY_TEMPERATURE, MAX_CERVICAL_FLUID_LENGTH, MAX_CYCLE_NOTES_LENGTH } from '../lib/cycle-tracking.config';
import { CycleTrackingPageComponent } from './cycle-tracking-page';
import { CYCLE_TRACKING_TOUR } from './cycle-tracking-page-lib/cycle-tracking-tour';

const TODAY = '2026-08-17';
const TRACKING_START = '2026-08-01';
const CYCLE_ID = entityId<'cycle-profile'>('cycle-1');
const FACTOR_ID = entityId<'cycle-factor'>('factor-1');
const EPISODE_ID = entityId<'menstrual-episode'>('episode-1');
const API_ERROR = new Error('Service unavailable');
const RECENT_DAY_LIMIT = 5;
const CYCLE_ROUTE = '/cycle-tracking';
const OTHER_ROUTE = '/other';
const CONFIRM_DIALOG = 'fd-ui-confirm-dialog';

@Component({ template: '<h1>Another page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class CycleOtherRouteComponent {}

const getCurrent = vi.fn<CyclesService['getCurrent']>();
const create = vi.fn<CyclesService['create']>();
const upsertDay = vi.fn<CyclesService['upsertDay']>();
const clearDay = vi.fn<CyclesService['clearDay']>();
const getNutritionSummary = vi.fn<CyclesService['getNutritionSummary']>();
const updateSettings = vi.fn<CyclesService['updateSettings']>();
const updateConsent = vi.fn<CyclesService['updateConsent']>();
const deleteCycle = vi.fn<CyclesService['deleteCycle']>();
const upsertFactor = vi.fn<CyclesService['upsertFactor']>();
const updateMenstrualEpisode = vi.fn<CyclesService['updateMenstrualEpisode']>();
const deleteMenstrualEpisode = vi.fn<CyclesService['deleteMenstrualEpisode']>();
const confirmPeriodStart = vi.fn<CyclesService['confirmPeriodStart']>();
const exportCycle = vi.fn<ExportService['exportCycle']>();
const exportSensitiveCycle = vi.fn<ExportService['exportSensitiveCycle']>();
const startTour = vi.fn<FdTourService['start']>();

let fixture: ComponentFixture<CycleTrackingPageComponent>;
let page: HTMLElement;

beforeEach(() => {
    vi.resetAllMocks();
    vi.useFakeTimers({ toFake: ['Date'] });
    const today = new Date(`${TODAY}T12:00:00`);
    vi.setSystemTime(today);
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    getCurrent.mockReturnValue(of(cycleProfile()));
    getNutritionSummary.mockReturnValue(of(null));
    TestBed.configureTestingModule({
        imports: [CycleTrackingPageComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'cycle-tracking', component: CycleTrackingPageComponent },
                { path: 'other', component: CycleOtherRouteComponent },
            ]),
            { provide: CurrentLocalDayService, useValue: { today: (): Date => today } },
            { provide: FdTourService, useValue: { start: startTour } },
            { provide: LocalizedTourDefinitionService, useValue: { build: (): typeof CYCLE_TRACKING_TOUR => CYCLE_TRACKING_TOUR } },
            {
                provide: CyclesService,
                useValue: {
                    getCurrent,
                    create,
                    upsertDay,
                    clearDay,
                    getNutritionSummary,
                    updateSettings,
                    updateConsent,
                    deleteCycle,
                    upsertFactor,
                    updateMenstrualEpisode,
                    deleteMenstrualEpisode,
                    confirmPeriodStart,
                },
            },
            { provide: ExportService, useValue: { exportCycle, exportSensitiveCycle } },
        ],
    });
});

afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
});

describe('Cycle tracking setup', () => {
    it('keeps setup hidden while loading and after failure, then restores it through Retry', async () => {
        const pending = new Subject<CycleResponse | null>();
        getCurrent.mockReturnValueOnce(pending).mockReturnValueOnce(of(null));
        await renderAsync();

        expect(page.querySelector<HTMLElement>('.cycle-tracking__loader')?.hidden).toBe(false);
        expect(page.querySelector<HTMLElement>('.cycle-tracking__setup-card')?.hidden).toBe(true);

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(page.querySelector<HTMLElement>('.cycle-tracking__setup-card')?.hidden).toBe(true);
        click('CYCLE_TRACKING.RETRY_LOAD', page);
        await stabilizeAsync();

        expect(getCurrent).toHaveBeenCalledTimes(2);
        expect(page.querySelector<HTMLElement>('.cycle-tracking__loader')?.hidden).toBe(true);
        expect(page.querySelector<HTMLElement>('.cycle-tracking__setup-card')?.hidden).toBe(false);
    });

    it('requires explicit tracking consent and retains it when creating the cycle needs a retry', async () => {
        getCurrent.mockReturnValue(of(null));
        create.mockReturnValueOnce(throwError(() => API_ERROR)).mockReturnValueOnce(of(cycleProfile()));
        await renderAsync();

        click('CYCLE_TRACKING.SAVE_CYCLE', page);
        await stabilizeAsync();
        expect(page.textContent).toContain('CYCLE_TRACKING.CONSENT_REQUIRED');
        expect(create).not.toHaveBeenCalled();

        const consent = requiredElementOfType(page, 'input[type="checkbox"]', HTMLInputElement);
        consent.click();
        await stabilizeAsync();
        click('CYCLE_TRACKING.SAVE_CYCLE', page);
        await stabilizeAsync();
        expect(page.textContent).toContain('CYCLE_TRACKING.CREATE_CYCLE_FAILED');
        expect(consent.checked).toBe(true);
        expect(create).toHaveBeenCalledWith(
            expect.objectContaining({
                trackingStartDate: TODAY,
                cycleTrackingConsentGranted: true,
                nutritionInsightsConsentGranted: false,
                fertilitySignalsConsentGranted: false,
            }),
        );

        await waitAndClickAsync('CYCLE_TRACKING.SAVE_CYCLE', page);
        await stabilizeAsync();
        expect(create).toHaveBeenCalledTimes(2);
        expect(page.querySelector('.cycle-tracking__setup-card')).toBeNull();
        expect(page.querySelector('fd-cycle-overview-card')).not.toBeNull();
    });
});

describe('Cycle diary', () => {
    it('preserves a note after a failed save, blocks a duplicate request, and reopens the saved note for today', async () => {
        const pending = new Subject<CycleLogDay>();
        const note = 'Recorded in the diary';
        upsertDay.mockReturnValueOnce(pending).mockReturnValueOnce(
            of({
                cycleProfileId: CYCLE_ID,
                date: calendarDate(TODAY),
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: null,
                notes: note,
            }),
        );
        getCurrent
            .mockReturnValueOnce(of(cycleProfile()))
            .mockReturnValueOnce(of(cycleProfile({ dayNotes: [{ date: calendarDate(TODAY), notes: note }] })));
        await renderAsync();

        click('CYCLE_TRACKING.LOG_TODAY', page);
        await stabilizeAsync();
        input(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement), note);
        await stabilizeAsync();
        click('CYCLE_TRACKING.SAVE_DAY');
        await stabilizeAsync();
        expect(button('CYCLE_TRACKING.SAVE_DAY').disabled).toBe(true);
        click('CYCLE_TRACKING.SAVE_DAY');
        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ date: TODAY, notes: note }));

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(document.body.textContent).toContain('CYCLE_TRACKING.SAVE_DAY_FAILED');
        expect(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement).value).toBe(note);
        await waitAndClickAsync('CYCLE_TRACKING.SAVE_DAY');
        await stabilizeAsync();
        expect(upsertDay).toHaveBeenCalledTimes(2);
        await waitForDrawerCloseAsync('#cycle-day-editor-title');
        expect(page.textContent).toContain(note);

        click('CYCLE_TRACKING.EDIT_TODAY', page);
        await stabilizeAsync();
        expect(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement).value).toBe(note);
        click('COMMON.CANCEL');
        await stabilizeAsync();
        expect(document.querySelector('#cycle-day-editor-title')).toBeNull();
    });

    it('shows five recent days until expanded and removes only the confirmed day', async () => {
        const dates = ['2026-08-11', '2026-08-12', '2026-08-13', '2026-08-14', '2026-08-15', '2026-08-16'];
        const dayNotes = dates.map((date, index) => ({ date: calendarDate(date), notes: `Diary entry ${index + 1}` }));
        getCurrent.mockReturnValue(of(cycleProfile({ dayNotes })));
        clearDay.mockReturnValue(of(void 0));
        await renderAsync();

        expect(page.querySelectorAll('fd-cycle-day-item')).toHaveLength(RECENT_DAY_LIMIT);
        expect(page.textContent).not.toContain('Diary entry 1');
        click('CYCLE_TRACKING.SHOW_ALL_HISTORY', page);
        await stabilizeAsync();
        const oldest = Array.from(page.querySelectorAll('fd-cycle-day-item')).find(item => item.textContent.includes('Diary entry 1'));
        if (!(oldest instanceof HTMLElement)) {
            throw new Error('Expected the oldest history row to be visible.');
        }

        click('CYCLE_TRACKING.CLEAR_DAY', oldest);
        await stabilizeAsync();
        click('COMMON.CANCEL', requiredElement(document.body, '[role="dialog"]'));
        await stabilizeAsync();
        expect(clearDay).not.toHaveBeenCalled();
        expect(oldest.isConnected).toBe(true);

        click('CYCLE_TRACKING.CLEAR_DAY', oldest);
        await stabilizeAsync();
        getCurrent.mockReturnValue(of(cycleProfile({ dayNotes: dayNotes.slice(1) })));
        click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, '[role="dialog"]'));
        await stabilizeAsync();
        expect(clearDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, dates[0]);
        expect(oldest.isConnected).toBe(false);
        expect(page.querySelectorAll('fd-cycle-day-item')).toHaveLength(RECENT_DAY_LIMIT);
    });
});

describe('Cycle preferences and exports', () => {
    it('retains a changed cycle length after an unsuccessful save and reopens with the persisted setting', async () => {
        const updatedLength = 32;
        updateSettings
            .mockReturnValueOnce(throwError(() => API_ERROR))
            .mockReturnValueOnce(of(cycleProfile({ averageCycleLength: updatedLength })));
        await renderAsync();

        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        const length = requiredElementOfType(document.body, '.cycle-drawer input[type="number"]', HTMLInputElement);
        expect(length.value).toBe('28');
        input(length, String(updatedLength));
        await stabilizeAsync();
        click('CYCLE_TRACKING.SAVE_SETTINGS');
        await stabilizeAsync();
        expect(document.body.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        expect(length.value).toBe(String(updatedLength));

        await waitAndClickAsync('CYCLE_TRACKING.SAVE_SETTINGS');
        await stabilizeAsync();
        expect(updateSettings).toHaveBeenCalledTimes(2);
        expect(updateSettings).toHaveBeenLastCalledWith(CYCLE_ID, expect.objectContaining({ averageCycleLength: updatedLength }));
        await waitForDrawerCloseAsync('#cycle-settings-title');

        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        expect(requiredElementOfType(document.body, '.cycle-drawer input[type="number"]', HTMLInputElement).value).toBe(
            String(updatedLength),
        );
    });

    it('keeps the cycle after canceling deletion and returns to setup after confirmed deletion', async () => {
        deleteCycle.mockReturnValue(of(void 0));
        await renderAsync();
        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();

        click('CYCLE_TRACKING.DELETE_CYCLE');
        await stabilizeAsync();
        click('COMMON.CANCEL', requiredElement(document.body, 'fd-ui-confirm-dialog'));
        await stabilizeAsync();
        expect(deleteCycle).not.toHaveBeenCalled();
        expect(document.querySelector('#cycle-settings-title')).not.toBeNull();

        click('CYCLE_TRACKING.DELETE_CYCLE');
        await stabilizeAsync();
        click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, 'fd-ui-confirm-dialog'));
        await stabilizeAsync();
        expect(deleteCycle).toHaveBeenCalledExactlyOnceWith(CYCLE_ID);
        expect(document.querySelector('#cycle-settings-title')).toBeNull();
        expect(page.querySelector<HTMLElement>('.cycle-tracking__setup-card')?.hidden).toBe(false);
    });

    it('routes regular and password-protected exports separately and surfaces failures on the page', async () => {
        exportCycle.mockReturnValue(throwError(() => API_ERROR));
        exportSensitiveCycle.mockReturnValue(of(void 0));
        const range = { dateFrom: TRACKING_START, dateTo: TODAY, timeZoneOffsetMinutes: -new Date().getTimezoneOffset() };
        await renderAsync();

        click('CYCLE_TRACKING.EXPORT_CYCLE', page);
        await stabilizeAsync();
        click('CYCLE_TRACKING.EXPORT_CYCLE', requiredElement(document.body, 'fd-cycle-export-dialog'));
        await stabilizeAsync();
        expect(exportCycle).toHaveBeenCalledExactlyOnceWith(range);
        expect(page.textContent).toContain('CYCLE_TRACKING.EXPORT_FAILED');
        expect(exportSensitiveCycle).not.toHaveBeenCalled();

        click('CYCLE_TRACKING.SENSITIVE_EXPORT_TITLE', page);
        await stabilizeAsync();
        const dialog = requiredElement(document.body, 'fd-cycle-export-dialog');
        click('CYCLE_TRACKING.SENSITIVE_EXPORT_CONFIRM', dialog);
        await stabilizeAsync();
        expect(exportSensitiveCycle).not.toHaveBeenCalled();

        input(requiredElementOfType(dialog, 'input[type="password"]', HTMLInputElement), 'test-password');
        await stabilizeAsync();
        click('CYCLE_TRACKING.SENSITIVE_EXPORT_CONFIRM', dialog);
        await stabilizeAsync();
        expect(exportSensitiveCycle).toHaveBeenCalledExactlyOnceWith({ ...range, currentPassword: 'test-password' });
        expect(exportCycle).toHaveBeenCalledOnce();
        expect(page.textContent).not.toContain('CYCLE_TRACKING.EXPORT_FAILED');
    });
});

describe('Cycle episode history', () => {
    it('opens and focuses episode history for ambiguous predictions, then lets the user review, exclude and delete the episode', async () => {
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
            excludedFromPredictions: false,
        };
        const profile = cycleProfile({
            menstrualEpisodes: [episode],
            predictions: {
                nextPeriodStartFrom: null,
                nextPeriodStartTo: null,
                ovulationFrom: null,
                ovulationTo: null,
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Low',
                rationale: '',
                reasonCodes: ['ambiguous_episode_history'],
            },
        });
        getCurrent.mockReturnValue(of(profile));
        updateMenstrualEpisode.mockReturnValue(of({ ...profile, menstrualEpisodes: [{ ...episode, excludedFromPredictions: true }] }));
        deleteMenstrualEpisode.mockReturnValue(of({ ...profile, menstrualEpisodes: [] }));
        await renderAsync();

        const history = requiredElementOfType(page, 'details.cycle-tracking__details', HTMLDetailsElement);
        expect(history.open).toBe(false);
        click('CYCLE_TRACKING.CHECK_EPISODE_HISTORY', page);
        await stabilizeAsync();
        expect(history.open).toBe(true);
        expect(document.activeElement).toBe(history.querySelector('summary'));

        const card = requiredElement(page, '[data-tour-id="cycle-tracking-episode-history"]');
        click('CYCLE_TRACKING.EDIT_EPISODE', card);
        await stabilizeAsync();
        expect(card.querySelector('form')).not.toBeNull();
        click('COMMON.CANCEL', card);
        await stabilizeAsync();
        expect(card.querySelector('form')).toBeNull();
        expect(updateMenstrualEpisode).not.toHaveBeenCalled();

        click('CYCLE_TRACKING.EXCLUDE_EPISODE', card);
        await stabilizeAsync();
        expect(updateMenstrualEpisode).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, EPISODE_ID, {
            startDate: TRACKING_START,
            endDate: null,
            excludedFromPredictions: true,
        });
        expect(card.textContent).toContain('CYCLE_TRACKING.EPISODE_EXCLUDED');
        expect(button('CYCLE_TRACKING.INCLUDE_EPISODE', card).disabled).toBe(false);

        click('CYCLE_TRACKING.DELETE_EPISODE', card);
        await stabilizeAsync();
        click('CYCLE_TRACKING.DELETE_EPISODE', requiredElement(document.body, 'fd-ui-confirm-dialog'));
        await stabilizeAsync();
        expect(deleteMenstrualEpisode).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, EPISODE_ID);
        expect(card.textContent).toContain('CYCLE_TRACKING.EPISODE_HISTORY_EMPTY');
    });
});

describe('Cycle route dialog ownership', () => {
    afterEach(() => {
        TestBed.inject(FdUiDialogService).closeAll();
    });

    it('discards the open sensitive export form when the route changes', async () => {
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        click('CYCLE_TRACKING.SENSITIVE_EXPORT_TITLE', cycleRouteHost(harness));
        harness.detectChanges();
        const dialog = requiredElement(document.body, 'fd-cycle-export-dialog');
        const password = requiredElementOfType(dialog, 'input[type="password"]', HTMLInputElement);
        input(password, 'temporary-fixture-value');
        harness.detectChanges();

        await verifyRouteChangeClosesDialogAsync(harness, 'fd-cycle-export-dialog');
        expect(password.isConnected).toBe(false);
        expect(exportCycle).not.toHaveBeenCalled();
        expect(exportSensitiveCycle).not.toHaveBeenCalled();
    });

    it('dismisses day clearance during navigation before any diary write', async () => {
        getCurrent.mockReturnValue(of(cycleProfile({ dayNotes: [{ date: calendarDate(TODAY), notes: 'Saved entry' }] })));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const item = requiredElement(cycleRouteHost(harness), 'fd-cycle-day-item');
        click('CYCLE_TRACKING.CLEAR_DAY', item);
        harness.detectChanges();

        await verifyRouteChangeClosesDialogAsync(harness, CONFIRM_DIALOG);
        expect(clearDay).not.toHaveBeenCalled();
    });

    it('dismisses unconfirmed profile deletion when the owner route and settings close', async () => {
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        click('CYCLE_TRACKING.SETTINGS_TITLE', cycleRouteHost(harness));
        harness.detectChanges();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        click('CYCLE_TRACKING.DELETE_CYCLE', drawer);
        harness.detectChanges();

        await verifyRouteChangeClosesDialogAsync(harness, CONFIRM_DIALOG);
        expect(document.querySelector('#cycle-settings-title')).toBeNull();
        expect(deleteCycle).not.toHaveBeenCalled();
    });

    it('dismisses episode deletion without changing the episode when leaving history', async () => {
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
            excludedFromPredictions: false,
        };
        getCurrent.mockReturnValue(of(cycleProfile({ menstrualEpisodes: [episode] })));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);
        requiredElement(host, 'details.cycle-tracking__details summary').click();
        harness.detectChanges();
        click('CYCLE_TRACKING.DELETE_EPISODE', requiredElement(host, '[data-tour-id="cycle-tracking-episode-history"]'));
        harness.detectChanges();

        await verifyRouteChangeClosesDialogAsync(harness, CONFIRM_DIALOG);
        expect(deleteMenstrualEpisode).not.toHaveBeenCalled();
    });
});

function cycleRouteHost(harness: RouterTestingHarness): HTMLElement {
    if (harness.routeNativeElement === null) {
        throw new Error('Expected a rendered cycle test route');
    }
    return harness.routeNativeElement;
}

async function verifyRouteChangeClosesDialogAsync(harness: RouterTestingHarness, selector: string): Promise<void> {
    expect(document.querySelector(selector)).not.toBeNull();
    await harness.navigateByUrl(OTHER_ROUTE, CycleOtherRouteComponent);
    expect(cycleRouteHost(harness).textContent).toContain('Another page');
    expect(document.querySelector(selector)).toBeNull();
}

describe('Cycle factors', () => {
    it('discards an abandoned factor edit and can end the saved factor today', async () => {
        const factor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: 'Saved note',
        };
        const profile = cycleProfile({ factors: [factor] });
        getCurrent.mockReturnValue(of(profile));
        upsertFactor
            .mockReturnValueOnce(throwError(() => API_ERROR))
            .mockReturnValueOnce(of({ ...profile, factors: [{ ...factor, endDate: calendarDate(TODAY) }] }));
        await renderAsync();

        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const card = requiredElement(page, '[data-tour-id="cycle-tracking-factors"]');
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        await stabilizeAsync();
        const notes = requiredElementOfType(card, 'textarea', HTMLTextAreaElement);
        expect(notes.value).toBe(factor.notes);
        input(notes, 'Abandoned draft');
        click('CYCLE_TRACKING.CANCEL_EDIT', card);
        await stabilizeAsync();
        expect(upsertFactor).not.toHaveBeenCalled();
        expect(notes.value).toBe('');

        click('CYCLE_TRACKING.END_FACTOR', card);
        await stabilizeAsync();
        expect(card.textContent).toContain('CYCLE_TRACKING.END_FACTOR_FAILED');
        click('CYCLE_TRACKING.END_FACTOR', card);
        await stabilizeAsync();
        expect(upsertFactor).toHaveBeenCalledTimes(2);
        expect(upsertFactor).toHaveBeenLastCalledWith(
            CYCLE_ID,
            expect.objectContaining({ factorId: FACTOR_ID, endDate: TODAY, notes: factor.notes }),
        );
        expect(card.textContent).not.toContain('CYCLE_TRACKING.END_FACTOR_FAILED');
        expect(card.querySelector('button[aria-label="CYCLE_TRACKING.END_FACTOR"]')).toBeNull();
    });
});

describe('Cycle journal input validation', () => {
    it('shows advanced field errors and blocks a save until the day entries are valid', async () => {
        getCurrent.mockReturnValue(
            of(
                cycleProfile({
                    consents: [
                        {
                            id: entityId<'cycle-consent'>('fertility-consent'),
                            purpose: CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
                            grantedAtUtc: utcInstant('2026-08-01T10:00:00Z'),
                            isActive: true,
                        },
                    ],
                }),
            ),
        );
        await renderAsync();
        click('CYCLE_TRACKING.LOG_TODAY', page);
        await stabilizeAsync();
        click('CYCLE_TRACKING.SHOW_ADVANCED');
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const notes = requiredElementOfType(drawer, 'textarea', HTMLTextAreaElement);
        const temperature = requiredElementOfType(drawer, '.cycle-drawer__advanced-fields input[type="number"]', HTMLInputElement);
        const fluid = requiredElementOfType(drawer, '.cycle-drawer__advanced-fields input[type="text"]', HTMLInputElement);

        editField(notes, 'x'.repeat(MAX_CYCLE_NOTES_LENGTH + 1));
        editField(temperature, String(MAX_BASAL_BODY_TEMPERATURE + 1));
        editField(fluid, 'x'.repeat(MAX_CERVICAL_FLUID_LENGTH + 1));
        await stabilizeAsync();
        expect(drawer.textContent).toContain('CYCLE_TRACKING.DAY_NOTES_TOO_LONG');
        expect(drawer.textContent).toContain('CYCLE_TRACKING.TEMPERATURE_RANGE');
        expect(drawer.textContent).toContain('CYCLE_TRACKING.CERVICAL_FLUID_TOO_LONG');
        click('CYCLE_TRACKING.SAVE_DAY', drawer);
        await stabilizeAsync();
        expect(upsertDay).not.toHaveBeenCalled();

        const savedNote = 'Diary note';
        const savedTemperature = 36.72;
        editField(notes, savedNote);
        editField(temperature, String(savedTemperature));
        editField(fluid, '');
        await stabilizeAsync();
        expect(drawer.textContent).not.toContain('CYCLE_TRACKING.DAY_NOTES_TOO_LONG');
        expect(drawer.textContent).not.toContain('CYCLE_TRACKING.TEMPERATURE_RANGE');
        expect(drawer.textContent).not.toContain('CYCLE_TRACKING.CERVICAL_FLUID_TOO_LONG');

        upsertDay.mockReturnValueOnce(
            of({
                cycleProfileId: CYCLE_ID,
                date: calendarDate(TODAY),
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: {
                    id: entityId<'fertility-signal'>('saved-signal'),
                    cycleProfileId: CYCLE_ID,
                    date: calendarDate(TODAY),
                    basalBodyTemperatureCelsius: savedTemperature,
                },
                notes: savedNote,
            }),
        );
        click('CYCLE_TRACKING.SAVE_DAY', drawer);
        await waitForDrawerCloseAsync('#cycle-day-editor-title');

        expect(upsertDay).toHaveBeenCalledOnce();
        const [cycleId, payload] = upsertDay.mock.calls[0];
        expect(cycleId).toBe(CYCLE_ID);
        expect(payload).toMatchObject({
            date: TODAY,
            notes: savedNote,
            fertilitySignal: { basalBodyTemperatureCelsius: savedTemperature },
        });
        expect(page.textContent).toContain(savedNote);
        expect(page.textContent).toContain('CYCLE_TRACKING.BBT_SUMMARY');
    });
});

describe('Cycle factor input validation', () => {
    it('displays oversized factor notes and keeps the edit active without writing them', async () => {
        const factor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: null,
        };
        getCurrent.mockReturnValue(of(cycleProfile({ factors: [factor] })));
        await renderAsync();
        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const card = requiredElement(page, '[data-tour-id="cycle-tracking-factors"]');
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        await stabilizeAsync();
        const notes = requiredElementOfType(card, 'textarea', HTMLTextAreaElement);

        editField(notes, 'x'.repeat(MAX_CYCLE_NOTES_LENGTH + 1));
        await stabilizeAsync();
        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
        await stabilizeAsync();

        expect(card.textContent).toContain('CYCLE_TRACKING.FACTOR_NOTES_TOO_LONG');
        expect(notes.value).toHaveLength(MAX_CYCLE_NOTES_LENGTH + 1);
        expect(upsertFactor).not.toHaveBeenCalled();
    });
});

describe('Cycle factor date recovery', () => {
    it('keeps the displayed range when the start moves past its end and sends only the corrected days', async () => {
        const earlier = '2026-08-10';
        const previousEnd = '2026-08-12';
        const later = '2026-08-14';
        const correctedEnd = '2026-08-16';
        const factor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(earlier),
            endDate: calendarDate(previousEnd),
            notes: 'Saved notes',
        };
        getCurrent.mockReturnValue(of(cycleProfile({ factors: [factor] })));
        upsertFactor.mockReturnValue(
            of(cycleProfile({ factors: [{ ...factor, startDate: calendarDate(later), endDate: calendarDate(correctedEnd) }] })),
        );
        await renderAsync();
        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const card = requiredElement(page, '[data-tour-id="cycle-tracking-factors"]');
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        await stabilizeAsync();
        const start = requiredElement(card, 'form fd-ui-date-input:first-of-type');
        const end = requiredElement(card, 'form fd-ui-date-input:last-of-type');

        await pickDateAsync(start, later);
        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
        await stabilizeAsync();
        expect(card.textContent).toContain('CYCLE_TRACKING.FACTOR_DATE_ORDER');
        expect(upsertFactor).not.toHaveBeenCalled();

        await pickDateAsync(end, correctedEnd);
        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
        await stabilizeAsync();
        expect(upsertFactor).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({ factorId: FACTOR_ID, startDate: later, endDate: correctedEnd, notes: factor.notes }),
        );
        expect(card.textContent).not.toContain('CYCLE_TRACKING.FACTOR_DATE_ORDER');
    });
});

describe('Cycle episode date validation', () => {
    it('keeps the editor open on a reversed range and sends only the corrected date keys', async () => {
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: calendarDate('2026-08-10'),
            endDate: calendarDate('2026-08-15'),
            status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
            excludedFromPredictions: false,
        };
        const profile = cycleProfile({ menstrualEpisodes: [episode] });
        getCurrent.mockReturnValue(of(profile));
        updateMenstrualEpisode.mockReturnValue(
            of({ ...profile, menstrualEpisodes: [{ ...episode, endDate: calendarDate('2026-08-13') }] }),
        );
        await renderAsync();
        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const card = requiredElement(page, '[data-tour-id="cycle-tracking-episode-history"]');
        click('CYCLE_TRACKING.EDIT_EPISODE', card);
        await stabilizeAsync();
        const endDate = requiredElement(card, 'form fd-ui-date-input:last-of-type');

        await pickDateAsync(endDate, '2026-08-09');
        click('CYCLE_TRACKING.SAVE_EPISODE', card);
        await stabilizeAsync();
        expect(card.textContent).toContain('CYCLE_TRACKING.EPISODE_DATE_ORDER');
        expect(updateMenstrualEpisode).not.toHaveBeenCalled();

        await pickDateAsync(endDate, '2026-08-13');
        click('CYCLE_TRACKING.SAVE_EPISODE', card);
        await stabilizeAsync();

        expect(updateMenstrualEpisode).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, EPISODE_ID, {
            startDate: '2026-08-10',
            endDate: '2026-08-13',
        });
        expect(card.querySelector('form')).toBeNull();
    });
});

describe('Cycle journal help', () => {
    it('starts the tour when requested from the page header', async () => {
        await renderAsync();
        click('CYCLE_TRACKING.TOUR.START_TOUR', page);

        expect(startTour).toHaveBeenCalledExactlyOnceWith(CYCLE_TRACKING_TOUR, { force: true });
    });
});

describe('Cycle history refresh after saving a journal day', () => {
    it.each(['network error', 'no profile'] as const)('keeps the saved entry and retries only the read after %s', async failure => {
        const date = calendarDate(TODAY);
        const note = 'Saved before prediction refresh';
        const refreshing = new Subject<CycleResponse | null>();
        const savedDay: CycleLogDay = {
            cycleProfileId: CYCLE_ID,
            date,
            bleedingEntries: [],
            symptoms: [],
            fertilitySignal: null,
            notes: note,
        };
        const refreshedProfile = cycleProfile({ dayNotes: [{ date, notes: note }] });
        upsertDay.mockReturnValueOnce(of(savedDay));
        getCurrent.mockReturnValueOnce(of(cycleProfile())).mockReturnValueOnce(refreshing).mockReturnValueOnce(of(refreshedProfile));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);

        click('CYCLE_TRACKING.LOG_TODAY', host);
        harness.detectChanges();
        input(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement), note);
        harness.detectChanges();
        click('CYCLE_TRACKING.SAVE_DAY');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(refreshing.observed).toBe(true);
        });
        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ date, notes: note }));
        expect(getCurrent).toHaveBeenCalledTimes(2);

        if (failure === 'network error') {
            refreshing.error(API_ERROR);
        } else {
            refreshing.next(null);
            refreshing.complete();
        }
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, 'fd-cycle-day-item').textContent).toContain(note);
            expect(host.textContent).toContain('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
            expect(document.querySelector('#cycle-day-editor-title')).toBeNull();
        });

        click('CYCLE_TRACKING.RETRY_LOAD', host);
        harness.detectChanges();
        const readsAfterRetry = 3;
        expect(getCurrent).toHaveBeenCalledTimes(readsAfterRetry);
        expect(upsertDay).toHaveBeenCalledOnce();
        expect(host.textContent).not.toContain('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
        expect(requiredElement(host, 'fd-cycle-day-item').textContent).toContain(note);

        click('CYCLE_TRACKING.EDIT_TODAY', host);
        harness.detectChanges();
        expect(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement).value).toBe(note);
    });
});

describe('Cycle history refresh after removal', () => {
    it('keeps the confirmed removal while retrying a refresh that returned no profile', async () => {
        const date = calendarDate('2026-08-16');
        const note = 'Entry to remove';
        getCurrent
            .mockReturnValueOnce(of(cycleProfile({ dayNotes: [{ date, notes: note }] })))
            .mockReturnValueOnce(of(null))
            .mockReturnValueOnce(of(cycleProfile()));
        clearDay.mockReturnValueOnce(of(void 0));
        await renderAsync();
        const row = requiredElement(page, 'fd-cycle-day-item');
        expect(row.textContent).toContain(note);

        click('CYCLE_TRACKING.CLEAR_DAY', row);
        await stabilizeAsync();
        click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, '[role="dialog"]'));
        await stabilizeAsync();
        expect(page.textContent).not.toContain(note);
        expect(page.textContent).toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');

        click('CYCLE_TRACKING.RETRY_LOAD', page);
        await stabilizeAsync();

        expect(clearDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, date);
        expect(page.textContent).not.toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
        expect(page.textContent).not.toContain(note);
    });
});

describe('Cycle journal deletion recovery', () => {
    it('locks both rows during deletion, then keeps the failed entry available for a confirmed retry', async () => {
        const targetDate = calendarDate('2026-08-16');
        const retainedNote = { date: calendarDate('2026-08-15'), notes: 'Retained journal entry' };
        const targetNote = { date: targetDate, notes: 'Journal entry to remove' };
        getCurrent
            .mockReturnValueOnce(of(cycleProfile({ dayNotes: [retainedNote, targetNote] })))
            .mockReturnValueOnce(of(cycleProfile({ dayNotes: [retainedNote] })));
        const pending = new Subject<void>();
        clearDay.mockReturnValueOnce(pending).mockReturnValueOnce(of(void 0));
        await renderAsync();
        const target = requiredElement(page, 'fd-cycle-day-item:first-of-type');
        const retained = requiredElement(page, 'fd-cycle-day-item:nth-of-type(2)');
        expect(target.textContent).toContain(targetNote.notes);
        expect(retained.textContent).toContain(retainedNote.notes);

        click('CYCLE_TRACKING.CLEAR_DAY', target);
        await stabilizeAsync();
        click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, '[role="dialog"]'));
        await stabilizeAsync();
        expect(clearDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, targetDate);
        expect(button('CYCLE_TRACKING.CLEAR_DAY', target).disabled).toBe(true);
        expect(button('CYCLE_TRACKING.CLEAR_DAY', retained).disabled).toBe(true);
        expect(button('CYCLE_TRACKING.EDIT_DAY', retained).disabled).toBe(true);

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(getCurrent).toHaveBeenCalledOnce();
        expect(target.isConnected).toBe(true);
        expect(retained.isConnected).toBe(true);
        expect(page.textContent).toContain('CYCLE_TRACKING.CLEAR_DAY_FAILED');
        expect(button('CYCLE_TRACKING.CLEAR_DAY', target).disabled).toBe(false);
        expect(button('CYCLE_TRACKING.EDIT_DAY', retained).disabled).toBe(false);

        click('CYCLE_TRACKING.CLEAR_DAY', target);
        await stabilizeAsync();
        click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, '[role="dialog"]'));
        await stabilizeAsync();
        expect(clearDay.mock.calls).toEqual([
            [CYCLE_ID, targetDate],
            [CYCLE_ID, targetDate],
        ]);
        expect(getCurrent).toHaveBeenCalledTimes(2);
        expect(target.isConnected).toBe(false);
        expect(retained.isConnected).toBe(true);
        expect(page.textContent).not.toContain('CYCLE_TRACKING.CLEAR_DAY_FAILED');
    });
});

describe('Cycle episode calendar dates', () => {
    it('keeps the same start and end days when the history language changes', async () => {
        const start = calendarDate(TRACKING_START);
        const end = calendarDate('2026-08-05');
        getCurrent.mockReturnValue(
            of(
                cycleProfile({
                    menstrualEpisodes: [
                        {
                            id: EPISODE_ID,
                            cycleProfileId: CYCLE_ID,
                            startDate: calendarDate(`${start}T00:00:00Z`),
                            endDate: calendarDate(`${end}T00:00:00Z`),
                            status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
                            excludedFromPredictions: false,
                        },
                    ],
                }),
            ),
        );
        const translate = TestBed.inject(TranslateService);
        const dateRange = { CYCLE_TRACKING: { EPISODE_DATE_RANGE: '{{ start }} — {{ end }}' } };
        translate.setTranslation('en', dateRange);
        translate.setTranslation('ru', dateRange);
        await firstValueFrom(translate.use('en'));
        await renderAsync();
        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const displayedRange = requiredElement(page, '.cycle-tracking__episode strong');
        expect(displayedRange.textContent.trim()).toBe(cycleEpisodeRange('en-US', start, end));

        await firstValueFrom(translate.use('ru'));
        await stabilizeAsync();

        expect(displayedRange.textContent.trim()).toBe(cycleEpisodeRange('ru-RU', start, end));
    });
});

describe('Cycle history period confirmation', () => {
    it('keeps the inferred day on failure and confirms the same calendar date on retry', async () => {
        const date = calendarDate('2026-08-16');
        const wireDate = calendarDate(`${date}T00:00:00Z`);
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: wireDate,
            endDate: null,
            status: MENSTRUAL_EPISODE_STATUS_INFERRED,
            excludedFromPredictions: false,
        };
        const inferred = cycleProfile({
            menstrualEpisodes: [episode],
            bleedingEntries: [
                {
                    id: entityId<'bleeding-entry'>('bleeding'),
                    cycleProfileId: CYCLE_ID,
                    date: wireDate,
                    type: BLEEDING_TYPE_BLEEDING,
                    flow: CYCLE_FLOW_MEDIUM,
                },
            ],
        });
        const confirmed = { ...inferred, menstrualEpisodes: [{ ...episode, status: MENSTRUAL_EPISODE_STATUS_CONFIRMED }] };
        const pending = new Subject<CycleResponse>();
        getCurrent.mockReturnValue(of(inferred));
        confirmPeriodStart.mockReturnValueOnce(pending).mockReturnValueOnce(of(confirmed));
        await renderAsync();
        const row = requiredElement(page, 'fd-cycle-day-item');
        expect(row.textContent).toContain('CYCLE_TRACKING.PERIOD_START_INFERRED');

        click('CYCLE_TRACKING.CONFIRM_PERIOD_START', row);
        fixture.detectChanges();
        expect(button('CYCLE_TRACKING.EDIT_DAY', row).disabled).toBe(true);
        expect(button('CYCLE_TRACKING.CLEAR_DAY', row).disabled).toBe(true);
        expect(confirmPeriodStart).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, date);

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(page.textContent).toContain('CYCLE_TRACKING.CONFIRM_PERIOD_START_FAILED');
        expect(row.textContent).toContain('CYCLE_TRACKING.PERIOD_START_INFERRED');
        await waitAndClickAsync('CYCLE_TRACKING.CONFIRM_PERIOD_START', row);
        await stabilizeAsync();

        expect(confirmPeriodStart).toHaveBeenLastCalledWith(CYCLE_ID, date);
        expect(confirmPeriodStart).toHaveBeenCalledTimes(2);
        expect(row.textContent).toContain('CYCLE_TRACKING.PERIOD_START_CONFIRMED');
        expect(page.textContent).not.toContain('CYCLE_TRACKING.CONFIRM_PERIOD_START_FAILED');
    });
});

describe('Cycle settings permission recovery', () => {
    it.each(['permissions only', 'duration and permissions', 'reproductive state and permissions'] as const)(
        'retries only the failed permission after editing %s',
        verifySettingsConsentRetryAsync,
    );
});

async function verifySettingsConsentRetryAsync(
    scope: 'permissions only' | 'duration and permissions' | 'reproductive state and permissions',
): Promise<void> {
    const tracking = cycleConsent('tracking', CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING);
    const fertility = cycleConsent('fertility', CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS);
    const nutrition = cycleConsent('nutrition', CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS);
    const initial = cycleProfile({ consents: [tracking, fertility] });
    const updatedLength = 30;
    const changesLength = scope === 'duration and permissions';
    const changesState = scope === 'reproductive state and permissions';
    const changesSettings = changesState || changesLength;
    const settingsSaved = {
        ...initial,
        averageCycleLength: changesLength ? updatedLength : initial.averageCycleLength,
        reproductiveState: changesState ? CYCLE_REPRODUCTIVE_STATE_POSTPARTUM : initial.reproductiveState,
        mode: changesState ? CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION : initial.mode,
    };
    const partial = { ...settingsSaved, consents: [tracking, fertility, nutrition] };
    const completed = { ...settingsSaved, consents: [tracking, { ...fertility, isActive: false }, nutrition] };
    getCurrent.mockReturnValue(of(initial));
    updateSettings.mockReturnValue(throwError(() => new Error('Settings endpoint unavailable')));
    if (changesSettings) {
        updateSettings.mockReturnValueOnce(of(settingsSaved));
    }
    updateConsent
        .mockReturnValueOnce(of(partial))
        .mockReturnValueOnce(throwError(() => API_ERROR))
        .mockReturnValueOnce(of(completed));
    await renderAsync();
    getNutritionSummary.mockClear();

    click('CYCLE_TRACKING.SETTINGS_TITLE', page);
    await stabilizeAsync();
    const drawer = requiredElement(document.body, '.cycle-drawer');
    await setCycleSettingsForConsentRetryAsync(drawer, settingsSaved, initial);
    const nutritionChoice = permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_NUTRITION');
    const fertilityChoice = permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_FERTILITY');
    expect(nutritionChoice.checked).toBe(false);
    expect(fertilityChoice.checked).toBe(true);
    nutritionChoice.click();
    await stabilizeAsync();
    fertilityChoice.click();
    await stabilizeAsync();
    expect(nutritionChoice.checked).toBe(true);
    expect(fertilityChoice.checked).toBe(false);
    click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
    const firstConsentWrites = [
        [CYCLE_ID, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS, { granted: true }],
        [CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false }],
    ];
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(updateConsent.mock.calls).toEqual(firstConsentWrites);
        expect(drawer.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
    });
    expect(nutritionChoice.checked).toBe(true);
    expect(fertilityChoice.checked).toBe(false);
    expect(getNutritionSummary).toHaveBeenCalled();
    const plannedSettingsWrites = changesSettings ? 1 : 0;
    expect(updateSettings).toHaveBeenCalledTimes(plannedSettingsWrites);

    await waitAndClickAsync('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
    await stabilizeAsync();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(updateConsent).toHaveBeenCalledTimes(firstConsentWrites.length + 1);
    });
    await waitForDrawerCloseAsync('#cycle-settings-title');
    expect(updateConsent).toHaveBeenLastCalledWith(CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false });
    expect(updateSettings).toHaveBeenCalledTimes(plannedSettingsWrites);
    await verifySavedSettingsAfterConsentRetryAsync(settingsSaved, changesState);
}

async function verifySavedSettingsAfterConsentRetryAsync(settingsSaved: CycleResponse, changesState: boolean): Promise<void> {
    click('CYCLE_TRACKING.SETTINGS_TITLE', page);
    await stabilizeAsync();
    const reopened = requiredElement(document.body, '.cycle-drawer');
    expect(permissionCheckbox(reopened, 'CYCLE_TRACKING.CONSENT_NUTRITION').checked).toBe(true);
    expect(permissionCheckbox(reopened, 'CYCLE_TRACKING.CONSENT_FERTILITY').checked).toBe(false);
    expect(requiredElementOfType(reopened, 'input[aria-label="CYCLE_TRACKING.AVG_LENGTH"]', HTMLInputElement).value).toBe(
        String(settingsSaved.averageCycleLength),
    );
    if (changesState) {
        expect(cyclePreferenceControl(reopened, 'CYCLE_TRACKING.REPRODUCTIVE_STATE').textContent).toContain(
            'CYCLE_TRACKING.STATE_POSTPARTUM',
        );
    }
    click('COMMON.CLOSE', requiredElement(reopened, '.cycle-drawer__header'));
    await waitForDrawerCloseAsync('#cycle-settings-title');
}

async function setCycleSettingsForConsentRetryAsync(drawer: HTMLElement, draft: CycleResponse, initial: CycleResponse): Promise<void> {
    if (draft.averageCycleLength !== initial.averageCycleLength) {
        const length = requiredElementOfType(drawer, 'input[aria-label="CYCLE_TRACKING.AVG_LENGTH"]', HTMLInputElement);
        editField(length, String(draft.averageCycleLength));
        await stabilizeAsync();
    }
    if (draft.reproductiveState !== initial.reproductiveState) {
        await selectCyclePreferenceAsync(drawer, 'CYCLE_TRACKING.REPRODUCTIVE_STATE', 'CYCLE_TRACKING.STATE_POSTPARTUM');
    }
}

describe('Cycle settings validation and selections', () => {
    it.each([
        { field: 'PERIOD_LENGTH', invalid: '0', corrected: 7, expected: { averagePeriodLength: 7 } },
        { field: 'LUTEAL_LENGTH', invalid: '13.5', corrected: 15, expected: { lutealLength: 15 } },
    ])('announces the $field error and saves the corrected value', async ({ field, invalid, corrected, expected }) => {
        updateSettings.mockReturnValue(of(cycleProfile(expected)));
        await renderAsync();
        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const control = requiredElementOfType(drawer, `input[aria-label="CYCLE_TRACKING.${field}"]`, HTMLInputElement);
        editField(control, invalid);
        await stabilizeAsync();
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await stabilizeAsync();

        const alert = requiredElement(drawer, '.fd-ui-input__error[role="alert"]');
        expect(alert.textContent).toContain(`CYCLE_TRACKING.${field}_ERROR`);
        expect(control.getAttribute('aria-invalid')).toBe('true');
        expect(control.getAttribute('aria-describedby')).toContain(alert.id);
        expect(updateSettings).not.toHaveBeenCalled();

        editField(control, String(corrected));
        await stabilizeAsync();
        expect(alert.isConnected).toBe(false);
        expect(control.hasAttribute('aria-invalid')).toBe(false);
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await waitForDrawerCloseAsync('#cycle-settings-title');
        expect(updateSettings).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining(expected));
    });

    it('renders and persists the chosen goal and reproductive state', async () => {
        const goal = CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE;
        const reproductiveState = CYCLE_REPRODUCTIVE_STATE_POSTPARTUM;
        updateSettings.mockReturnValue(of(cycleProfile({ goal, reproductiveState, mode: CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION })));
        await renderAsync();
        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const goalOption = 'CYCLE_TRACKING.GOAL_TRYING_TO_CONCEIVE';
        const reproductiveStateOption = 'CYCLE_TRACKING.STATE_POSTPARTUM';
        await selectCyclePreferenceAsync(drawer, 'CYCLE_TRACKING.GOAL', goalOption);
        await selectCyclePreferenceAsync(drawer, 'CYCLE_TRACKING.REPRODUCTIVE_STATE', reproductiveStateOption);
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await waitForDrawerCloseAsync('#cycle-settings-title');
        expect(updateSettings).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ goal, reproductiveState }));
        expect(updateConsent).not.toHaveBeenCalled();

        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        const reopened = requiredElement(document.body, '.cycle-drawer');
        expect(cyclePreferenceControl(reopened, 'CYCLE_TRACKING.GOAL').textContent).toContain(goalOption);
        expect(cyclePreferenceControl(reopened, 'CYCLE_TRACKING.REPRODUCTIVE_STATE').textContent).toContain(reproductiveStateOption);
    });
});

describe('Cycle settings request ownership', () => {
    it('locks the drawer during the first consent update and drops the second update when leaving the page', async () => {
        const pendingConsent = new Subject<CycleResponse>();
        const grant = cycleConsent('consented-later', CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS);
        updateSettings.mockReturnValue(of(cycleProfile()));
        updateConsent.mockReturnValue(pendingConsent);
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        click('CYCLE_TRACKING.SETTINGS_TITLE', cycleRouteHost(harness));
        harness.detectChanges();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_NUTRITION').click();
        permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_FERTILITY').click();
        harness.detectChanges();
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(pendingConsent.observed).toBe(true);
            expect(drawer.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
        });
        expect(updateConsent).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS, { granted: true });
        expect(button('COMMON.CANCEL', drawer).disabled).toBe(true);
        drawer.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        harness.detectChanges();
        expect(drawer.isConnected).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE, CycleOtherRouteComponent);
        expect(cycleRouteHost(harness).textContent).toContain('Another page');
        expect(drawer.isConnected).toBe(false);
        expect(pendingConsent.observed).toBe(false);
        pendingConsent.next(cycleProfile({ consents: [grant] }));
        pendingConsent.complete();
        harness.detectChanges();
        expect(updateConsent).toHaveBeenCalledTimes(1);
    });
});

describe('Cycle settings draft registration', () => {
    it('declines the settings action when the profile does not exist and detaches with the page', async () => {
        getCurrent.mockReturnValue(of(null));
        const { attach, detach } = registerCycleScreen();
        await renderAsync();
        const screen = attachedCycleScreen(attach);

        expect(screen.read()).toEqual({ loading: false, error: null, hasCycle: false, settingsOpen: false, settings: null });
        expect(() => {
            screen.openDraft();
        }).toThrow('An idle existing cycle is required to open its settings.');
        expect(document.querySelector('#cycle-settings-title')).toBeNull();
        fixture.destroy();
        expect(detach).toHaveBeenCalledOnce();
    });

    it('reports a native settings edit as dirty until the draft is cancelled', async () => {
        const { attach } = registerCycleScreen();
        await renderAsync();
        const screen = attachedCycleScreen(attach);
        expect(screen.read()).toMatchObject({ hasCycle: true, settingsOpen: false, settings: { averageCycleLength: 28 } });
        expect(screen.isDirty()).toBe(false);

        screen.openDraft();
        await stabilizeAsync();
        expect(screen.read()).toMatchObject({ settingsOpen: true });
        const drawer = requiredElement(document.body, '.cycle-drawer');
        input(requiredElementOfType(drawer, 'input[type="number"]', HTMLInputElement), '32');
        await stabilizeAsync();
        expect(screen.isDirty()).toBe(true);

        click('COMMON.CANCEL', drawer);
        await waitForDrawerCloseAsync('#cycle-settings-title');
        expect(screen.isDirty()).toBe(false);
        expect(screen.read()).toMatchObject({ settingsOpen: false });
    });
});

function cycleEpisodeRange(locale: string, start: string, end: string): string {
    const format = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });
    return `${format.format(new Date(`${start}T00:00:00Z`))} — ${format.format(new Date(`${end}T00:00:00Z`))}`;
}

function cycleConsent(id: string, purpose: CycleConsentPurpose): CycleConsent {
    return { id: entityId<'cycle-consent'>(id), purpose, isActive: true, grantedAtUtc: utcInstant('2026-08-01T10:00:00Z') };
}

function cyclePreferenceControl(drawer: HTMLElement, label: string): HTMLButtonElement {
    const control = Array.from(drawer.querySelectorAll<HTMLElement>('fd-ui-select')).find(
        item => item.querySelector('.fd-ui-select__label-text')?.textContent.trim() === label,
    );
    if (control === undefined) {
        throw new Error(`Could not find the ${label} cycle preference.`);
    }
    return requiredElementOfType(control, 'button[aria-haspopup="listbox"]', HTMLButtonElement);
}

async function selectCyclePreferenceAsync(drawer: HTMLElement, label: string, option: string): Promise<void> {
    if (Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView') === undefined) {
        Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
        onTestFinished(() => {
            Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
        });
    }
    const control = cyclePreferenceControl(drawer, label);
    control.click();
    await stabilizeAsync();
    const listbox = requiredElement(document.body, '[role="listbox"]');
    click(option, listbox);
    await stabilizeAsync();
    expect(control.textContent).toContain(option);
}

function permissionCheckbox(drawer: HTMLElement, label: string): HTMLInputElement {
    const control = Array.from(drawer.querySelectorAll<HTMLElement>('fd-ui-checkbox')).find(
        item => item.querySelector('.fd-ui-checkbox__label-text')?.textContent.trim() === label,
    );
    if (control === undefined) {
        throw new Error(`Could not find the ${label} preference.`);
    }
    return requiredElementOfType(control, 'input[type="checkbox"]', HTMLInputElement);
}

function registerCycleScreen(): {
    attach: ReturnType<typeof vi.fn<(screen: DevelopmentScreen) => () => void>>;
    detach: ReturnType<typeof vi.fn>;
} {
    const detach = vi.fn();
    const attach = vi.fn<(screen: DevelopmentScreen) => () => void>().mockReturnValue(detach);
    TestBed.configureTestingModule({ providers: [{ provide: DEVELOPMENT_SCREENS, useValue: { attach } }] });
    return { attach, detach };
}

function attachedCycleScreen(
    attach: ReturnType<typeof vi.fn<(screen: DevelopmentScreen) => () => void>>,
): Omit<DevelopmentScreen, 'openDraft' | 'isDirty'> & { openDraft: () => void; isDirty: () => boolean } {
    const screen = attach.mock.lastCall?.[0];
    if (screen?.openDraft === undefined || screen.isDirty === undefined) {
        throw new Error('Cycle settings draft action was not registered.');
    }
    return { ...screen, openDraft: screen.openDraft, isDirty: screen.isDirty };
}

function editField(control: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    input(control, value);
    control.dispatchEvent(new FocusEvent('blur'));
}

async function pickDateAsync(host: HTMLElement, date: string): Promise<void> {
    const trigger = requiredElementOfType(host, 'button[aria-haspopup="dialog"]', HTMLButtonElement);
    const popupId = trigger.getAttribute('aria-controls');
    if (popupId === null) {
        throw new Error('Date picker trigger has no popup');
    }
    trigger.click();
    await stabilizeAsync();
    const popup = document.getElementById(popupId);
    if (popup === null) {
        throw new Error('Date picker did not open');
    }
    const day = requiredElementOfType(popup, `[data-date="${date}"]`, HTMLButtonElement);
    expect(day.disabled).toBe(false);
    day.click();
    await stabilizeAsync();
}

async function renderAsync(): Promise<void> {
    fixture = TestBed.createComponent(CycleTrackingPageComponent);
    page = fixture.nativeElement as HTMLElement;
    await stabilizeAsync();
}

async function stabilizeAsync(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
}

function requiredElement(root: HTMLElement, selector: string): HTMLElement {
    return requiredElementOfType(root, selector, HTMLElement);
}

function requiredElementOfType<T extends HTMLElement>(root: HTMLElement, selector: string, type: abstract new () => T): T {
    const element = root.querySelector(selector);
    if (!(element instanceof type)) {
        throw new Error(`Could not find ${selector}.`);
    }
    return element;
}

function button(label: string, root: HTMLElement = document.body): HTMLButtonElement {
    const match = Array.from(root.querySelectorAll('button')).find(
        candidate => candidate.getAttribute('aria-label') === label || candidate.textContent.trim().endsWith(label),
    );
    if (match === undefined) {
        throw new Error(`Could not find the ${label} button.`);
    }
    return match;
}

function click(label: string, root: HTMLElement = document.body): void {
    button(label, root).click();
}

async function waitAndClickAsync(label: string, root: HTMLElement = document.body): Promise<void> {
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(button(label, root).disabled).toBe(false);
    });
    click(label, root);
}

async function waitForDrawerCloseAsync(selector: string): Promise<void> {
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(document.querySelector(selector)).toBeNull();
    });
}

function input(control: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

function cycleProfile(overrides: Partial<CycleResponse> = {}): CycleResponse {
    return {
        id: CYCLE_ID,
        userId: entityId<'user'>('user-1'),
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        goal: CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
        reproductiveState: CYCLE_REPRODUCTIVE_STATE_CYCLING,
        hideFromDashboard: false,
        confidence: 1,
        trackingStartDate: calendarDate(TRACKING_START),
        averageCycleLength: 28,
        averagePeriodLength: 5,
        lutealLength: 14,
        isRegular: true,
        isOnboardingComplete: true,
        showFertilityEstimates: false,
        discreetNotifications: true,
        bleedingEntries: [],
        symptoms: [],
        factors: [],
        fertilitySignals: [],
        menstrualEpisodes: [],
        predictions: null,
        ...overrides,
    };
}
