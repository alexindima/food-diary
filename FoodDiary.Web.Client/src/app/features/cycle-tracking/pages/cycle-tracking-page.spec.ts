import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdTourService } from 'fd-tour';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ExportService } from '../../../shared/api/export.service';
import {
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_REPRODUCTIVE_STATE_CYCLING,
    CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleFactor,
    type CycleLogDay,
    type CycleResponse,
    MENSTRUAL_EPISODE_STATUS_CONFIRMED,
} from '../../../shared/models/cycle.data';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { CurrentLocalDayService } from '../../../shared/platform/current-local-day.service';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { CyclesService } from '../api/cycles.service';
import { CycleTrackingPageComponent } from './cycle-tracking-page';
import { CYCLE_TRACKING_TOUR } from './cycle-tracking-page-lib/cycle-tracking-tour';

const TODAY = '2026-08-17';
const TRACKING_START = '2026-08-01';
const CYCLE_ID = entityId<'cycle-profile'>('cycle-1');
const FACTOR_ID = entityId<'cycle-factor'>('factor-1');
const EPISODE_ID = entityId<'menstrual-episode'>('episode-1');
const API_ERROR = new Error('Service unavailable');
const RECENT_DAY_LIMIT = 5;

const getCurrent = vi.fn<CyclesService['getCurrent']>();
const create = vi.fn<CyclesService['create']>();
const upsertDay = vi.fn<CyclesService['upsertDay']>();
const clearDay = vi.fn<CyclesService['clearDay']>();
const getNutritionSummary = vi.fn<CyclesService['getNutritionSummary']>();
const updateSettings = vi.fn<CyclesService['updateSettings']>();
const deleteCycle = vi.fn<CyclesService['deleteCycle']>();
const upsertFactor = vi.fn<CyclesService['upsertFactor']>();
const updateMenstrualEpisode = vi.fn<CyclesService['updateMenstrualEpisode']>();
const deleteMenstrualEpisode = vi.fn<CyclesService['deleteMenstrualEpisode']>();
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
            provideRouter([]),
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
                    deleteCycle,
                    upsertFactor,
                    updateMenstrualEpisode,
                    deleteMenstrualEpisode,
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
