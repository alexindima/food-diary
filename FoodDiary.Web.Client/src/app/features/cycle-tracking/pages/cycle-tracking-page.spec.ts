import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { WebmcpScreenRegistry } from 'fd-webmcp';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ExportService } from '../../../shared/api/export.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../shared/development/development-tools';
import {
    BLEEDING_TYPE_BLEEDING,
    BLEEDING_TYPE_SPOTTING,
    type BleedingEntry,
    CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_HEAVY,
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
    type CycleNutritionSummary,
    type CycleResponse,
    type CycleSymptomEntry,
    type FertilitySignal,
    MENSTRUAL_EPISODE_STATUS_CONFIRMED,
    MENSTRUAL_EPISODE_STATUS_INFERRED,
    OVULATION_TEST_RESULT_NEGATIVE,
    OVULATION_TEST_RESULT_UNKNOWN,
    type OvulationTestResult,
} from '../../../shared/models/cycle.data';
import { calendarDate, utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId, type MenstrualEpisodeId } from '../../../shared/models/semantics/entity-id';
import { CurrentLocalDayService } from '../../../shared/platform/current-local-day.service';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { CyclesService } from '../api/cycles.service';
import { MAX_BASAL_BODY_TEMPERATURE, MAX_CERVICAL_FLUID_LENGTH, MAX_CYCLE_NOTES_LENGTH } from '../lib/cycle-tracking.config';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';
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

describe('Cycle onboarding preferences', () => {
    it('starts tracking with the selected day and preferences and prevents a second submit while saving', async () => {
        const startDate = calendarDate('2026-08-15');
        const pending = new Subject<CycleResponse>();
        getCurrent.mockReturnValue(of(null));
        create.mockReturnValue(pending);
        await renderAsync();

        const setup = requiredElement(page, '.cycle-tracking__setup-card');
        const dateInput = requiredElement(setup, 'fd-ui-date-input');
        expectRequiredDatePicker(dateInput);
        await pickDateAsync(dateInput, startDate);
        await selectCyclePreferenceAsync(setup, 'CYCLE_TRACKING.GOAL', 'CYCLE_TRACKING.GOAL_TRYING_TO_CONCEIVE');
        await selectCyclePreferenceAsync(setup, 'CYCLE_TRACKING.REPRODUCTIVE_STATE', 'CYCLE_TRACKING.STATE_POSTPARTUM');
        requiredElementOfType(setup, 'input[type="checkbox"]', HTMLInputElement).click();
        await stabilizeAsync();

        const submit = button('CYCLE_TRACKING.SAVE_CYCLE', setup);
        submit.click();
        await stabilizeAsync();
        expect(submit.disabled).toBe(true);
        submit.click();
        expect(create).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                trackingStartDate: startDate,
                mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
                goal: CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE,
                reproductiveState: CYCLE_REPRODUCTIVE_STATE_POSTPARTUM,
                cycleTrackingConsentGranted: true,
            }),
        );

        pending.next(
            cycleProfile({
                trackingStartDate: startDate,
                mode: CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
                goal: CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE,
                reproductiveState: CYCLE_REPRODUCTIVE_STATE_POSTPARTUM,
            }),
        );
        pending.complete();
        await stabilizeAsync();
        expect(setup.isConnected).toBe(false);
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
        const cancel = button('COMMON.CANCEL', requiredElement(document.body, '.cycle-drawer'));
        click('CYCLE_TRACKING.SAVE_DAY');
        expect(pending.observed).toBe(true);
        expect(cancel.disabled).toBe(false);
        cancel.click();
        await stabilizeAsync();
        expect(cancel.isConnected).toBe(true);
        expect(cancel.disabled).toBe(true);
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
});

describe('Cycle diary history', () => {
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

describe('Cycle preferences', () => {
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

    it('keeps the cycle after cancellation and locks its settings while confirmed deletion is pending', async () => {
        const pending = new Subject<void>();
        deleteCycle.mockReturnValue(pending);
        await renderAsync();
        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();

        click('CYCLE_TRACKING.DELETE_CYCLE');
        await stabilizeAsync();
        click('COMMON.CANCEL', requiredElement(document.body, 'fd-ui-confirm-dialog'));
        await stabilizeAsync();
        expect(deleteCycle).not.toHaveBeenCalled();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const deleteButton = button('CYCLE_TRACKING.DELETE_CYCLE', drawer);
        const backdrop = requiredElementOfType(drawer, '.cycle-drawer__backdrop', HTMLButtonElement);

        deleteButton.click();
        await stabilizeAsync();
        click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, CONFIRM_DIALOG));
        expect(pending.observed).toBe(true);
        expect(deleteButton.disabled).toBe(false);
        deleteButton.click();
        backdrop.click();
        await stabilizeAsync();

        expect(deleteCycle).toHaveBeenCalledExactlyOnceWith(CYCLE_ID);
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        expect(drawer.isConnected).toBe(true);
        expect(deleteButton.disabled).toBe(true);

        pending.next(undefined);
        pending.complete();
        await waitForDrawerCloseAsync('#cycle-settings-title');
        expect(document.querySelector('#cycle-settings-title')).toBeNull();
        expect(page.querySelector<HTMLElement>('.cycle-tracking__setup-card')?.hidden).toBe(false);
    });
});

describe('Cycle profile deletion while a refresh replaces its owner', () => {
    it('does not let a confirmation for the previous profile remove its replacement', async () => {
        const { harness, host, http, reload, replacement } = await openCycleDeletionWithPendingRetryAsync();
        const oldConfirmation = requiredElement(document.body, CONFIRM_DIALOG);
        const oldDelete = button('CYCLE_TRACKING.DELETE_CYCLE', oldConfirmation);
        loadReplacementCycle(reload, replacement, harness);
        expect(cycleHistoryDayWithNote(host, REPLACEMENT_DELETION_NOTE).textContent).toContain(REPLACEMENT_DELETION_NOTE);
        expect(oldDelete.isConnected).toBe(true);
        expect(oldDelete.disabled).toBe(false);

        oldDelete.click();
        const replacementRequest = cycleDeletionRequest(replacement.id);
        http.expectNone(replacementRequest, 'a confirmation for the previous cycle must not delete the replacement');
        await harness.fixture.whenStable();
        harness.detectChanges();
        confirmCycleDeletion(harness);
        http.expectOne(replacementRequest).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
        http.verify();
    });

    it.each(['success', 'failure'] as const)('keeps replacement state after a previous DELETE returns %s', async outcome => {
        const { harness, host, http, reload, original, replacement } = await openCycleDeletionWithPendingRetryAsync();
        click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, CONFIRM_DIALOG));
        const pending = http.expectOne(cycleDeletionRequest(original.id));
        loadReplacementCycle(reload, replacement, harness);
        expect(cycleHistoryDayWithNote(host, REPLACEMENT_DELETION_NOTE).textContent).toContain(REPLACEMENT_DELETION_NOTE);

        if (outcome === 'success') {
            pending.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        } else {
            pending.flush({ error: 'Cycle.NotFound' }, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
        }
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(cycleHistoryDayWithNote(host, REPLACEMENT_DELETION_NOTE).textContent).toContain(REPLACEMENT_DELETION_NOTE);
        const facade = harness.routeDebugElement?.injector.get(CycleTrackingFacade);
        expect(facade?.settingsError()).toBeNull();
        http.expectNone(cycleDeletionRequest(replacement.id));
        http.verify();
    });

    it('keeps replacement deletion locked when a previous DELETE finishes', async () => {
        const { harness, host, http, reload, original, replacement } = await openCycleDeletionWithPendingRetryAsync();
        click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, CONFIRM_DIALOG));
        const previous = http.expectOne(cycleDeletionRequest(original.id));
        loadReplacementCycle(reload, replacement, harness);
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        expect(document.querySelector('.cycle-drawer')).not.toBeNull();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const deleteButton = button('CYCLE_TRACKING.DELETE_CYCLE', drawer);
        deleteButton.click();
        harness.detectChanges();
        click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, CONFIRM_DIALOG));
        const current = http.expectOne(cycleDeletionRequest(replacement.id));
        harness.detectChanges();
        expect(deleteButton.disabled).toBe(true);

        previous.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(cycleHistoryDayWithNote(host, REPLACEMENT_DELETION_NOTE).textContent).toContain(REPLACEMENT_DELETION_NOTE);
        expect(drawer.isConnected).toBe(true);
        expect(deleteButton.disabled).toBe(true);

        current.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
        http.verify();
    });
});

const REPLACEMENT_DELETION_NOTE = 'Replacement journal survives deletion of the old profile';

async function openCycleDeletionWithPendingRetryAsync(): Promise<{
    harness: RouterTestingHarness;
    host: HTMLElement;
    http: HttpTestingController;
    reload: Subject<CycleResponse>;
    original: CycleResponse;
    replacement: CycleResponse;
}> {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const transport = TestBed.runInInjectionContext(() => new CyclesService());
    deleteCycle.mockImplementation(id => transport.deleteCycle(id));
    const removedNote = 'Journal entry removed ahead of the cycle retry';
    const original = cycleProfile({
        id: entityId<'cycle-profile'>('00000000-0000-4000-8000-000000000014'),
        dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }],
    });
    const replacement = cycleProfile({
        id: entityId<'cycle-profile'>('00000000-0000-4000-8000-000000000015'),
        dayNotes: [{ date: calendarDate(TODAY), notes: REPLACEMENT_DELETION_NOTE }],
    });
    const reload = new Subject<CycleResponse>();
    getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
    clearDay.mockReturnValueOnce(of(void 0));
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(reload.observed).toBe(true);
    harness.detectChanges();
    click('CYCLE_TRACKING.SETTINGS_TITLE', host);
    harness.detectChanges();
    click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, '.cycle-drawer'));
    harness.detectChanges();
    return { harness, host, http, reload, original, replacement };
}

function cycleDeletionRequest(id: CycleResponse['id']): { method: string; url: string } {
    return { method: 'DELETE', url: `${environment.apiUrls.cycles}/${id}` };
}

it('keeps an episode deletion confirmation on the profile that opened it', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const transport = TestBed.runInInjectionContext(() => new CyclesService());
    deleteMenstrualEpisode.mockImplementation((cycleId, episodeId) => transport.deleteMenstrualEpisode(cycleId, episodeId));
    const removedNote = 'Journal entry removed before retrying Cycle history';
    const replacementNote = 'Replacement profile has its own confirmed episode';
    const original = profileForEpisodeDeletion(
        '00000000-0000-4000-8000-000000000016',
        '00000000-0000-4000-8000-000000000018',
        removedNote,
        '2026-08-16',
    );
    const replacement = profileForEpisodeDeletion(
        '00000000-0000-4000-8000-000000000017',
        '00000000-0000-4000-8000-000000000019',
        replacementNote,
        TODAY,
    );
    const reload = new Subject<CycleResponse>();
    getCurrent.mockReturnValueOnce(of(original.profile)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
    clearDay.mockReturnValueOnce(of(void 0));
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(reload.observed).toBe(true);

    const history = openRoutedEpisodeHistory(harness);
    click('CYCLE_TRACKING.DELETE_EPISODE', history);
    harness.detectChanges();
    const oldDelete = button('CYCLE_TRACKING.DELETE_EPISODE', requiredElement(document.body, CONFIRM_DIALOG));
    loadReplacementCycle(reload, replacement.profile, harness);
    expect(cycleHistoryDayWithNote(host, replacementNote).textContent).toContain(replacementNote);
    expect(oldDelete.isConnected).toBe(true);
    expect(oldDelete.disabled).toBe(false);

    oldDelete.click();
    const wrongRequests = http.match(episodeDeletionRequest(replacement.profile.id, original.episodeId));
    for (const request of wrongRequests) {
        request.flush(
            { error: 'Validation.Invalid', errors: { menstrualEpisodeId: ['Menstrual episode was not found for this cycle profile.'] } },
            { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' },
        );
    }
    await harness.fixture.whenStable();
    harness.detectChanges();
    const visibleError = history.querySelector('fd-ui-form-error:not([hidden])')?.textContent.trim() ?? null;

    click('CYCLE_TRACKING.DELETE_EPISODE', history);
    harness.detectChanges();
    click('CYCLE_TRACKING.DELETE_EPISODE', requiredElement(document.body, CONFIRM_DIALOG));
    http.expectOne(episodeDeletionRequest(replacement.profile.id, replacement.episodeId)).flush({
        ...replacement.profile,
        menstrualEpisodes: [],
    });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(history.textContent).toContain('CYCLE_TRACKING.EPISODE_HISTORY_EMPTY');
    expect(visibleError).toBeNull();
    expect(wrongRequests).toHaveLength(0);
    http.verify();
});

function profileForEpisodeDeletion(
    rawProfileId: string,
    rawEpisodeId: string,
    note: string,
    date: string,
): { profile: CycleResponse; episodeId: MenstrualEpisodeId } {
    const profileId = entityId<'cycle-profile'>(rawProfileId);
    const episodeId = entityId<'menstrual-episode'>(rawEpisodeId);
    return {
        profile: cycleProfile({
            id: profileId,
            dayNotes: [{ date: calendarDate(date), notes: note }],
            menstrualEpisodes: [
                {
                    id: episodeId,
                    cycleProfileId: profileId,
                    startDate: calendarDate(TRACKING_START),
                    endDate: null,
                    status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
                    excludedFromPredictions: false,
                },
            ],
        }),
        episodeId,
    };
}

function episodeDeletionRequest(profileId: CycleResponse['id'], episodeId: MenstrualEpisodeId): { method: string; url: string } {
    return { method: 'DELETE', url: `${environment.apiUrls.cycles}/${profileId}/menstrual-episodes/${episodeId}` };
}

describe('Cycle exports', () => {
    it('routes regular and password-protected exports separately and surfaces failures on the page', async () => {
        const pending = new Subject<void>();
        exportCycle.mockReturnValue(pending);
        exportSensitiveCycle.mockReturnValue(of(void 0));
        const range = { dateFrom: TRACKING_START, dateTo: TODAY, timeZoneOffsetMinutes: -new Date().getTimezoneOffset() };
        await renderAsync();

        click('CYCLE_TRACKING.EXPORT_CYCLE', page);
        await stabilizeAsync();
        const sensitiveExport = button('CYCLE_TRACKING.SENSITIVE_EXPORT_TITLE', page);
        click('CYCLE_TRACKING.EXPORT_CYCLE', requiredElement(document.body, 'fd-cycle-export-dialog'));
        expect(pending.observed).toBe(true);
        expect(sensitiveExport.disabled).toBe(false);
        sensitiveExport.click();
        await stabilizeAsync();

        expect(exportCycle).toHaveBeenCalledExactlyOnceWith(range);
        expect(document.querySelector('fd-cycle-export-dialog')).toBeNull();
        expect(sensitiveExport.disabled).toBe(true);
        expect(exportSensitiveCycle).not.toHaveBeenCalled();

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(page.textContent).toContain('CYCLE_TRACKING.EXPORT_FAILED');
        expect(sensitiveExport.disabled).toBe(false);

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
        const scrollHistory = vi.fn();
        Object.defineProperty(history, 'scrollIntoView', { value: scrollHistory });
        expect(history.open).toBe(false);
        click('CYCLE_TRACKING.CHECK_EPISODE_HISTORY', page);
        await stabilizeAsync();
        expect(history.open).toBe(true);
        expect(scrollHistory).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ block: 'start' }));
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

describe('Rendered Cycle Factor controls during profile replacement', () => {
    it('keeps a stale factor Update and Edit bound to the profile that rendered them', async () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const http = TestBed.inject(HttpTestingController);
        const transport = TestBed.runInInjectionContext(() => new CyclesService());
        upsertFactor.mockImplementation((id, payload) => transport.upsertFactor(id, payload));
        const removedNote = 'Journal entry removed while switching cycle profiles';
        const oldFactor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: 'Previous profile factor',
        };
        const replacementId = entityId<'cycle-profile'>('00000000-0000-4000-8000-000000000011');
        const replacementFactor = {
            ...oldFactor,
            id: entityId<'cycle-factor'>('00000000-0000-4000-8000-000000000012'),
            cycleProfileId: replacementId,
            notes: 'Replacement profile factor',
        };
        const original = cycleProfile({
            dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }],
            factors: [oldFactor],
        });
        const replacement = cycleProfile({ id: replacementId, factors: [replacementFactor] });
        const reload = new Subject<CycleResponse>();
        getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
        clearDay.mockReturnValueOnce(of(void 0));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = await retryAfterJournalRemovalAsync(harness, removedNote);
        expect(reload.observed).toBe(true);
        requiredElement(host, 'details.cycle-tracking__details summary').click();
        const oldCard = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        const oldEdit = button('CYCLE_TRACKING.EDIT_FACTOR', oldCard);
        oldEdit.click();
        harness.detectChanges();
        const oldUpdate = button('CYCLE_TRACKING.UPDATE_FACTOR', oldCard);
        const request = { method: 'PUT', url: `${environment.apiUrls.cycles}/${replacementId}/factors` };

        reload.next(replacement);
        reload.complete();
        expect(oldEdit.isConnected).toBe(true);
        expect(oldEdit.disabled).toBe(false);
        expect(oldUpdate.isConnected).toBe(true);
        expect(oldUpdate.disabled).toBe(false);
        oldEdit.click();
        oldUpdate.click();
        http.expectNone(request, 'the previous profile’s Update button must not create a factor in its replacement');
        harness.detectChanges();

        const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        const notes = requiredElementOfType(card, 'textarea', HTMLTextAreaElement);
        expect(notes.value).toBe('');
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        harness.detectChanges();
        expect(notes.value).toBe(replacementFactor.notes);
        const newNotes = 'Edited replacement factor';
        input(notes, newNotes);
        harness.detectChanges();
        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
        const saved = http.expectOne(request);
        expect(saved.request.body).toMatchObject({ factorId: replacementFactor.id, notes: newNotes });
        saved.flush({ ...replacement, factors: [{ ...replacementFactor, notes: newNotes }] });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(notes.value).toBe('');
        http.verify();
    });
});

describe('Cycle factor draft during profile replacement', () => {
    it('starts a fresh factor after Retry loads a replacement profile', async () => {
        const removedNote = 'Journal entry to remove before replacing the profile';
        const oldFactor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: 'A saved factor',
        };
        const oldProfile = cycleProfile({
            factors: [oldFactor],
            dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }],
        });
        const replacementId = entityId<'cycle-profile'>('factor-draft-replacement-cycle');
        const replacementNote = 'Replacement profile journal';
        const replacement = cycleProfile({
            id: replacementId,
            dayNotes: [{ date: calendarDate(TODAY), notes: replacementNote }],
            factors: [
                {
                    ...oldFactor,
                    id: entityId<'cycle-factor'>('replacement-factor-1'),
                    cycleProfileId: replacementId,
                    notes: 'B saved factor',
                },
            ],
        });
        const retry = new Subject<CycleResponse>();
        getCurrent.mockReturnValueOnce(of(oldProfile)).mockReturnValueOnce(of(null)).mockReturnValueOnce(retry);
        clearDay.mockReturnValueOnce(of(void 0));
        upsertFactor.mockReturnValueOnce(of(replacement));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = await retryAfterJournalRemovalAsync(harness, removedNote);
        expect(retry.observed).toBe(true);
        requiredElement(host, 'details.cycle-tracking__details summary').click();
        const oldCard = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        click('CYCLE_TRACKING.EDIT_FACTOR', oldCard);
        harness.detectChanges();
        const oldDraft = 'A unsaved factor notes';
        input(requiredElementOfType(oldCard, 'textarea', HTMLTextAreaElement), oldDraft);
        harness.detectChanges();
        expect(button('CYCLE_TRACKING.UPDATE_FACTOR', oldCard).disabled).toBe(false);

        loadReplacementCycle(retry, replacement, harness);
        await harness.fixture.whenStable();
        harness.detectChanges();
        const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        expect(cycleHistoryDayWithNote(host, replacementNote).textContent).toContain(replacementNote);
        const notes = requiredElementOfType(card, 'textarea', HTMLTextAreaElement);
        const save = requiredElementOfType(card, 'button[type="submit"]', HTMLButtonElement);
        const draftAfterRetry = { notes: notes.value, action: save.textContent.trim() };

        const newNote = 'New factor notes for B';
        input(notes, newNote);
        harness.detectChanges();
        save.click();
        await vi.waitFor(() => {
            expect(upsertFactor).toHaveBeenCalledOnce();
        });

        expect({ draftAfterRetry, request: upsertFactor.mock.lastCall }).toEqual({
            draftAfterRetry: { notes: '', action: 'CYCLE_TRACKING.SAVE_FACTOR' },
            request: [
                replacementId,
                {
                    type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
                    startDate: calendarDate(TODAY),
                    endDate: null,
                    notes: newNote,
                    clearNotes: false,
                },
            ],
        });
    });
});

describe('Active Cycle factor saves during Retry', () => {
    it('keeps a pending factor edit owned by the same profile', async () => {
        const { harness, host, retry, original, factor, save } = await preparePendingFactorRetryAsync();
        retry.next(original);
        retry.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        expect(save.observed).toBe(true);
        expect(requiredElementOfType(card, 'textarea', HTMLTextAreaElement).value).toBe('Edited note');
        expect(button('CYCLE_TRACKING.UPDATE_FACTOR', card).disabled).toBe(true);

        save.next({ ...original, factors: [{ ...factor, notes: 'Edited note' }] });
        save.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(button('CYCLE_TRACKING.SAVE_FACTOR', card).disabled).toBe(false);
        });
        expect(upsertFactor).toHaveBeenCalledOnce();
    });

    it('cancels an old factor save when Retry reports no current profile', async () => {
        const { harness, host, retry, save } = await preparePendingFactorRetryAsync();
        retry.next(null);
        retry.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(save.observed).toBe(false);
            expect(requiredElement(host, '.cycle-tracking__setup-card').hasAttribute('hidden')).toBe(false);
        });

        await replaceDeletedCycleAsync(harness, cycleProfile({ id: entityId<'cycle-profile'>('created-after-factor-retry') }));
        requiredElement(host, 'details.cycle-tracking__details summary').click();
        harness.detectChanges();
        const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        expect(requiredElementOfType(card, 'textarea', HTMLTextAreaElement).value).toBe('');
        expect(button('CYCLE_TRACKING.SAVE_FACTOR', card).disabled).toBe(false);
        expect(upsertFactor).toHaveBeenCalledOnce();
    });
});

async function preparePendingFactorRetryAsync(): Promise<{
    harness: RouterTestingHarness;
    host: HTMLElement;
    retry: Subject<CycleResponse | null>;
    original: CycleResponse;
    factor: CycleFactor;
    save: Subject<CycleResponse>;
}> {
    const removedNote = 'Journal entry removed before refreshing a factor';
    const factor: CycleFactor = {
        id: FACTOR_ID,
        cycleProfileId: CYCLE_ID,
        type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
        startDate: calendarDate(TRACKING_START),
        endDate: null,
        notes: 'A saved factor',
    };
    const original = cycleProfile({ factors: [factor] });
    const retry = new Subject<CycleResponse | null>();
    const save = new Subject<CycleResponse>();
    getCurrent
        .mockReturnValueOnce(of({ ...original, dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }] }))
        .mockReturnValueOnce(of(null))
        .mockReturnValueOnce(retry);
    clearDay.mockReturnValueOnce(of(void 0));
    upsertFactor.mockReturnValueOnce(save);
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    await submitPendingFactorActionAsync(harness, 'update', save);
    return { harness, host, retry, original, factor, save };
}

describe('Cycle factor response ownership after profile deletion', () => {
    it.each([
        { action: 'update', recreate: false },
        { action: 'end', recreate: true },
    ] as const)('ignores a late factor $action from the deleted profile', async ({ action, recreate }) => {
        const factor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: 'Note from the old profile',
        };
        const pending = new Subject<CycleResponse>();
        const initial = cycleProfile({ factors: [factor] });
        getCurrent.mockReturnValueOnce(of(initial));
        upsertFactor.mockReturnValueOnce(pending);
        deleteCycle.mockReturnValueOnce(of(undefined));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);
        await submitPendingFactorActionAsync(harness, action, pending);

        await deleteCycleDuringFactorRequestAsync(harness);
        expect(pending.observed).toBe(false);
        const replacementLength = 32;
        const replacement = cycleProfile({ id: entityId<'cycle-profile'>('new-cycle'), averageCycleLength: replacementLength });
        if (recreate) {
            await replaceDeletedCycleAsync(harness, replacement);
        }
        const savedFactor = action === 'update' ? { ...factor, notes: 'Edited note' } : { ...factor, endDate: calendarDate(TODAY) };
        pending.next({ ...initial, factors: [savedFactor] });
        pending.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        if (recreate) {
            click('CYCLE_TRACKING.SETTINGS_TITLE', host);
            harness.detectChanges();
            const lengthInput = requiredElementOfType(
                document.body,
                '.cycle-drawer input[aria-label="CYCLE_TRACKING.AVG_LENGTH"]',
                HTMLInputElement,
            );
            expect(lengthInput.value).toBe(String(replacement.averageCycleLength));
        } else {
            expect(host.querySelector('fd-cycle-overview-card')).toBeNull();
            expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
            await replaceDeletedCycleAsync(harness, replacement);
            requiredElement(host, 'details.cycle-tracking__details summary').click();
            expect(requiredElementOfType(host, '[data-tour-id="cycle-tracking-factors"] textarea', HTMLTextAreaElement).value).toBe('');
        }
    });
});

describe('Cycle factor failures queued during profile deletion', () => {
    it.each([
        { action: 'update', errorKey: 'CYCLE_TRACKING.SAVE_FACTOR_FAILED' },
        { action: 'end', errorKey: 'CYCLE_TRACKING.END_FACTOR_FAILED' },
    ] as const)('keeps the replacement profile free from an obsolete factor $action error', async ({ action, errorKey }) => {
        const factor: CycleFactor = {
            id: FACTOR_ID,
            cycleProfileId: CYCLE_ID,
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate(TRACKING_START),
            endDate: null,
            notes: 'Old profile factor',
        };
        const pendingFactor = new Subject<CycleResponse>();
        const pendingDelete = new Subject<void>();
        getCurrent.mockReturnValueOnce(of(cycleProfile({ factors: [factor] })));
        upsertFactor.mockReturnValueOnce(pendingFactor);
        deleteCycle.mockReturnValueOnce(pendingDelete);
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        await submitPendingFactorActionAsync(harness, action, pendingFactor);
        confirmCycleDeletion(harness);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(pendingDelete.observed).toBe(true);
            expect(pendingFactor.observed).toBe(true);
        });

        pendingDelete.next(undefined);
        pendingFactor.error(API_ERROR);
        await waitForCycleDeletedAsync(harness);
        await replaceDeletedCycleAsync(harness, cycleProfile({ id: entityId<'cycle-profile'>('replacement-cycle') }));

        const host = cycleRouteHost(harness);
        requiredElement(host, 'details.cycle-tracking__details summary').click();
        harness.detectChanges();
        const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
        expect(card.textContent).not.toContain(errorKey);
        expect(button('CYCLE_TRACKING.SAVE_FACTOR', card).disabled).toBe(false);
    });
});

describe('Cycle episode results after profile deletion', () => {
    it.each([
        { action: 'save', queued: false, replace: false },
        { action: 'exclude', queued: true, replace: false },
        { action: 'delete', queued: false, replace: true },
    ] as const)('discards an old episode $action after removing the profile', async ({ action, queued, replace }) => {
        const initial = profileWithEpisode();
        const pendingEpisode = new Subject<CycleResponse>();
        const pendingDelete = new Subject<void>();
        getCurrent.mockReturnValueOnce(of(initial));
        deleteCycle.mockReturnValueOnce(pendingDelete);
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        await startPendingEpisodeActionAsync(harness, action, pendingEpisode);
        confirmCycleDeletion(harness);
        expect(pendingDelete.observed).toBe(true);

        pendingDelete.next(undefined);
        pendingDelete.complete();
        if (queued) {
            pendingEpisode.next(initial);
            pendingEpisode.complete();
        }
        const host = cycleRouteHost(harness);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
        });
        const requestCancelled = !pendingEpisode.observed;
        const replacementLength = 32;
        if (replace) {
            await replaceDeletedCycleAsync(
                harness,
                cycleProfile({ id: entityId<'cycle-profile'>('replacement-cycle'), averageCycleLength: replacementLength }),
            );
        }
        if (!queued) {
            pendingEpisode.next(initial);
            pendingEpisode.complete();
        }
        await harness.fixture.whenStable();
        harness.detectChanges();

        if (replace) {
            click('CYCLE_TRACKING.SETTINGS_TITLE', host);
            harness.detectChanges();
            const length = requiredElementOfType(
                document.body,
                '.cycle-drawer input[aria-label="CYCLE_TRACKING.AVG_LENGTH"]',
                HTMLInputElement,
            );
            expect(length.value).toBe(String(replacementLength));
        } else {
            expect(host.querySelector('fd-cycle-overview-card')).toBeNull();
        }
        expect(requestCancelled).toBe(true);
    });

    it('keeps episode history clear of an old error queued after deletion', async () => {
        const pendingEpisode = new Subject<CycleResponse>();
        const pendingDelete = new Subject<void>();
        getCurrent.mockReturnValueOnce(of(profileWithEpisode()));
        deleteCycle.mockReturnValueOnce(pendingDelete);
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        await startPendingEpisodeActionAsync(harness, 'exclude', pendingEpisode);
        confirmCycleDeletion(harness);
        expect(pendingDelete.observed).toBe(true);

        pendingDelete.next(undefined);
        pendingDelete.complete();
        pendingEpisode.error(API_ERROR);
        await waitForCycleDeletedAsync(harness);
        await replaceDeletedCycleAsync(harness, cycleProfile({ id: entityId<'cycle-profile'>('replacement-without-episodes') }));

        const history = openRoutedEpisodeHistory(harness);
        expect(history.textContent).toContain('CYCLE_TRACKING.EPISODE_HISTORY_EMPTY');
        expect(history.textContent).not.toContain('CYCLE_TRACKING.EPISODE_PREDICTION_FAILED');
    });
});

function profileWithEpisode(): CycleResponse {
    return cycleProfile({
        menstrualEpisodes: [
            {
                id: EPISODE_ID,
                cycleProfileId: CYCLE_ID,
                startDate: calendarDate(TRACKING_START),
                endDate: null,
                status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
                excludedFromPredictions: false,
            },
        ],
    });
}

function openRoutedEpisodeHistory(harness: RouterTestingHarness): HTMLElement {
    const host = cycleRouteHost(harness);
    requiredElement(host, 'details.cycle-tracking__details summary').click();
    harness.detectChanges();
    return requiredElement(host, '[data-tour-id="cycle-tracking-episode-history"]');
}

async function startPendingEpisodeActionAsync(
    harness: RouterTestingHarness,
    action: 'save' | 'exclude' | 'delete',
    pending: Subject<CycleResponse>,
): Promise<void> {
    const history = openRoutedEpisodeHistory(harness);
    if (action === 'delete') {
        deleteMenstrualEpisode.mockReturnValueOnce(pending);
        click('CYCLE_TRACKING.DELETE_EPISODE', history);
        harness.detectChanges();
        click('CYCLE_TRACKING.DELETE_EPISODE', requiredElement(document.body, CONFIRM_DIALOG));
    } else {
        updateMenstrualEpisode.mockReturnValueOnce(pending);
        if (action === 'save') {
            click('CYCLE_TRACKING.EDIT_EPISODE', history);
            harness.detectChanges();
            click('CYCLE_TRACKING.SAVE_EPISODE', history);
        } else {
            click('CYCLE_TRACKING.EXCLUDE_EPISODE', history);
        }
    }
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(pending.observed).toBe(true);
    });
}

async function submitPendingFactorActionAsync(
    harness: RouterTestingHarness,
    action: 'update' | 'end',
    pending: Subject<CycleResponse>,
): Promise<void> {
    const host = cycleRouteHost(harness);
    requiredElement(host, 'details.cycle-tracking__details summary').click();
    const card = requiredElement(host, '[data-tour-id="cycle-tracking-factors"]');
    if (action === 'update') {
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        harness.detectChanges();
        input(requiredElementOfType(card, 'textarea', HTMLTextAreaElement), 'Edited note');
        harness.detectChanges();
        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
    } else {
        click('CYCLE_TRACKING.END_FACTOR', card);
    }
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(pending.observed).toBe(true);
    });
    expect(upsertFactor).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ factorId: FACTOR_ID }));
}

async function deleteCycleDuringFactorRequestAsync(harness: RouterTestingHarness): Promise<void> {
    confirmCycleDeletion(harness);
    await waitForCycleDeletedAsync(harness);
}

function confirmCycleDeletion(harness: RouterTestingHarness): void {
    const host = cycleRouteHost(harness);
    click('CYCLE_TRACKING.SETTINGS_TITLE', host);
    harness.detectChanges();
    click('CYCLE_TRACKING.DELETE_CYCLE');
    harness.detectChanges();
    click('CYCLE_TRACKING.DELETE_CYCLE', requiredElement(document.body, CONFIRM_DIALOG));
}

async function waitForCycleDeletedAsync(harness: RouterTestingHarness): Promise<void> {
    const host = cycleRouteHost(harness);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
    });
    expect(deleteCycle).toHaveBeenCalledExactlyOnceWith(CYCLE_ID);
}

async function replaceDeletedCycleAsync(harness: RouterTestingHarness, replacement: CycleResponse): Promise<void> {
    const host = cycleRouteHost(harness);
    create.mockReturnValueOnce(of(replacement));
    const setup = requiredElement(host, '.cycle-tracking__setup-card');
    requiredElementOfType(setup, 'input[type="checkbox"]', HTMLInputElement).click();
    harness.detectChanges();
    click('CYCLE_TRACKING.SAVE_CYCLE', setup);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(host.querySelector('fd-cycle-overview-card')).not.toBeNull();
    });
    expect(create).toHaveBeenCalledOnce();
}

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
    it('keeps the displayed range until corrected and lets the user clear only the optional end', async () => {
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
        const corrected = { ...factor, startDate: calendarDate(later), endDate: calendarDate(correctedEnd) };
        upsertFactor
            .mockReturnValueOnce(of(cycleProfile({ factors: [corrected] })))
            .mockReturnValueOnce(of(cycleProfile({ factors: [{ ...corrected, endDate: null }] })));
        await renderAsync();
        requiredElement(page, 'details.cycle-tracking__details summary').click();
        const card = requiredElement(page, '[data-tour-id="cycle-tracking-factors"]');
        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        await stabilizeAsync();
        const start = requiredElement(card, 'form fd-ui-date-input:first-of-type');
        const end = requiredElement(card, 'form fd-ui-date-input:last-of-type');
        expectRequiredDatePicker(start);

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

        click('CYCLE_TRACKING.EDIT_FACTOR', card);
        await stabilizeAsync();
        const optionalEnd = requiredElement(card, 'form fd-ui-date-input:last-of-type');
        requiredElementOfType(optionalEnd, 'button[aria-label="CYCLE_TRACKING.CLEAR_FACTOR_END_DATE"]', HTMLButtonElement).click();
        await stabilizeAsync();
        expect(requiredElementOfType(optionalEnd, 'input', HTMLInputElement).value).toBe('');

        click('CYCLE_TRACKING.UPDATE_FACTOR', card);
        await stabilizeAsync();
        expect(upsertFactor).toHaveBeenCalledTimes(2);
        expect(upsertFactor).toHaveBeenLastCalledWith(
            CYCLE_ID,
            expect.objectContaining({ factorId: FACTOR_ID, startDate: later, endDate: null, notes: factor.notes }),
        );
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

describe('New cycle day bleeding defaults', () => {
    it('keeps the default medium flow when bleeding is turned off and back on before saving', async () => {
        const date = calendarDate(TODAY);
        const bleeding: Pick<BleedingEntry, 'type' | 'flow'> = { type: BLEEDING_TYPE_BLEEDING, flow: CYCLE_FLOW_MEDIUM };
        upsertDay.mockReturnValueOnce(
            of({
                date,
                cycleProfileId: CYCLE_ID,
                bleedingEntries: [{ id: entityId<'bleeding-entry'>('new-bleeding'), cycleProfileId: CYCLE_ID, date, ...bleeding }],
                symptoms: [],
                fertilitySignal: null,
            }),
        );
        await renderAsync();
        click('CYCLE_TRACKING.LOG_TODAY', page);
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');

        expect(button('CYCLE_TRACKING.BLEEDING_NONE', drawer).getAttribute('aria-pressed')).toBe('true');
        click('CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING', drawer);
        await stabilizeAsync();
        expect(button('CYCLE_TRACKING.FLOW_MEDIUM', drawer).getAttribute('aria-pressed')).toBe('true');

        click('CYCLE_TRACKING.BLEEDING_NONE', drawer);
        await stabilizeAsync();
        expect(drawer.textContent).not.toContain('CYCLE_TRACKING.FLOW_MEDIUM');
        click('CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING', drawer);
        await stabilizeAsync();
        expect(button('CYCLE_TRACKING.FLOW_MEDIUM', drawer).getAttribute('aria-pressed')).toBe('true');
        click('CYCLE_TRACKING.SAVE_DAY', drawer);
        await stabilizeAsync();

        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({ date, bleeding: { ...bleeding, painImpact: 0, clearNotes: true } }),
        );
    });
});

describe('Cycle day bleeding and pain correction', () => {
    it('keeps saved spotting after cancel and reopens with the corrected bleeding flow and pain after saving', async () => {
        const date = calendarDate(TODAY);
        const spotting: BleedingEntry = {
            id: entityId<'bleeding-entry'>('saved-spotting'),
            cycleProfileId: CYCLE_ID,
            date,
            type: BLEEDING_TYPE_SPOTTING,
            flow: CYCLE_FLOW_MEDIUM,
            painImpact: 3,
        };
        const painImpact = 9;
        const bleeding: BleedingEntry = {
            ...spotting,
            id: entityId<'bleeding-entry'>('corrected-bleeding'),
            type: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_HEAVY,
            painImpact,
        };
        const saved: CycleLogDay = { date, cycleProfileId: CYCLE_ID, bleedingEntries: [bleeding], symptoms: [], fertilitySignal: null };
        getCurrent
            .mockReturnValueOnce(of(cycleProfile({ bleedingEntries: [spotting] })))
            .mockReturnValueOnce(of(cycleProfile({ bleedingEntries: [bleeding] })));
        upsertDay.mockReturnValueOnce(of(saved));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);
        const drawer = (): HTMLElement => requiredElement(document.body, '.cycle-drawer');
        const setCorrection = (): void => {
            click('CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING', drawer());
            harness.detectChanges();
            click('CYCLE_TRACKING.FLOW_HEAVY', drawer());
            click('CYCLE_TRACKING.PAIN_LEVEL_9', drawer());
            harness.detectChanges();
        };

        click('CYCLE_TRACKING.EDIT_TODAY', host);
        harness.detectChanges();
        setCorrection();
        expect(button('CYCLE_TRACKING.FLOW_HEAVY', drawer()).getAttribute('aria-pressed')).toBe('true');
        expect(button('CYCLE_TRACKING.PAIN_LEVEL_9', drawer()).getAttribute('aria-pressed')).toBe('true');
        click('COMMON.CANCEL', drawer());
        harness.detectChanges();
        expect(upsertDay).not.toHaveBeenCalled();

        click('CYCLE_TRACKING.EDIT_TODAY', host);
        harness.detectChanges();
        expect(button('CYCLE_TRACKING.BLEEDING_TYPE_SPOTTING', drawer()).getAttribute('aria-pressed')).toBe('true');
        expect(button('CYCLE_TRACKING.FLOW_MEDIUM', drawer()).getAttribute('aria-pressed')).toBe('true');
        expect(button('CYCLE_TRACKING.PAIN_LEVEL_3', drawer()).getAttribute('aria-pressed')).toBe('true');
        setCorrection();
        click('CYCLE_TRACKING.UPDATE_DAY', drawer());
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector('#cycle-day-editor-title')).toBeNull();
        });

        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({
                date,
                clearBleeding: true,
                bleeding: { type: BLEEDING_TYPE_BLEEDING, flow: CYCLE_FLOW_HEAVY, painImpact, clearNotes: true },
            }),
        );
        expect(getCurrent).toHaveBeenCalledTimes(2);
        click('CYCLE_TRACKING.EDIT_TODAY', host);
        harness.detectChanges();
        expect(button('CYCLE_TRACKING.BLEEDING_TYPE_SPOTTING', drawer()).getAttribute('aria-pressed')).toBe('false');
        expect(button('CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING', drawer()).getAttribute('aria-pressed')).toBe('true');
        expect(button('CYCLE_TRACKING.FLOW_HEAVY', drawer()).getAttribute('aria-pressed')).toBe('true');
        expect(button('CYCLE_TRACKING.PAIN_LEVEL_9', drawer()).getAttribute('aria-pressed')).toBe('true');
    });
});

describe('Cycle advanced day editing', () => {
    it.each([
        { action: 'replaces the recorded temperature', temperature: 36.72, ovulationTestResult: OVULATION_TEST_RESULT_UNKNOWN },
        { action: 'clears the last tracked value', temperature: null, ovulationTestResult: OVULATION_TEST_RESULT_UNKNOWN },
        {
            action: 'keeps a tracked test when clearing the temperature',
            temperature: null,
            ovulationTestResult: OVULATION_TEST_RESULT_NEGATIVE,
        },
    ] as const)('$action and keeps other diary data', async ({ temperature, ovulationTestResult }) => {
        const { earlierSignal, earlierNote, todaySignal, todayNote, symptom, profile } = cycleAdvancedDayFixture(ovulationTestResult);
        const clearsSignal = temperature === null && ovulationTestResult === OVULATION_TEST_RESULT_UNKNOWN;
        const savedSignal = clearsSignal ? null : { ...todaySignal, basalBodyTemperatureCelsius: temperature };
        const savedDay: CycleLogDay = {
            cycleProfileId: CYCLE_ID,
            date: todaySignal.date,
            bleedingEntries: [],
            symptoms: [symptom],
            fertilitySignal: savedSignal,
            notes: todayNote,
        };
        const savedSignals = savedSignal === null ? [earlierSignal] : [earlierSignal, savedSignal];
        const result = new Subject<CycleLogDay>();
        upsertDay.mockReturnValueOnce(result);
        getCurrent.mockReturnValueOnce(of(profile)).mockReturnValueOnce(of({ ...profile, fertilitySignals: savedSignals }));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);
        const editor = openTodayAdvancedEditor(harness, host);
        const firstTemperature = cycleTemperatureInput(editor);
        expect(firstTemperature.value).toBe(String(todaySignal.basalBodyTemperatureCelsius));

        const intensity = requiredElement(editor, 'fieldset[aria-label="CYCLE_TRACKING.SYMPTOM_INTENSITY_LABEL"]');
        const currentSeverity = button('CYCLE_TRACKING.PAIN_LEVEL_6', intensity);
        expect(currentSeverity.getAttribute('aria-pressed')).toBe('true');
        currentSeverity.click();
        editField(firstTemperature, temperature === null ? '' : String(temperature));
        harness.detectChanges();
        click('CYCLE_TRACKING.UPDATE_DAY', editor);
        await vi.waitFor(() => {
            expect(upsertDay).toHaveBeenCalledOnce();
        });

        const [profileId, payload] = upsertDay.mock.calls[0];
        expect(profileId).toBe(CYCLE_ID);
        expect(payload.date).toBe(todaySignal.date);
        expect(payload.notes).toBe(todayNote);
        expect(payload.symptoms).toMatchObject([{ category: symptom.category, intensity: symptom.intensity }]);
        expect(payload.clearFertilitySignal).toBe(clearsSignal);
        if (clearsSignal) {
            expect(payload.fertilitySignal).toBeNull();
        } else {
            expect(payload.fertilitySignal).toMatchObject({ basalBodyTemperatureCelsius: temperature, ovulationTestResult });
        }

        result.next(savedDay);
        result.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector('#cycle-day-editor-title')).toBeNull();
        });
        const today = cycleHistoryDayWithNote(host, todayNote);
        expect(today.textContent.includes('CYCLE_TRACKING.BBT_SUMMARY')).toBe(temperature !== null);
        expect(today.textContent.includes('CYCLE_TRACKING.OVULATION_TEST_NEGATIVE_SUMMARY')).toBe(
            ovulationTestResult === OVULATION_TEST_RESULT_NEGATIVE,
        );
        const earlierDay = cycleHistoryDayWithNote(host, earlierNote);
        expect(earlierDay.textContent).toContain('CYCLE_TRACKING.BBT_SUMMARY');
        click('CYCLE_TRACKING.EDIT_DAY', earlierDay);
        harness.detectChanges();
        click('CYCLE_TRACKING.SHOW_ADVANCED');
        harness.detectChanges();
        expect(cycleTemperatureInput(requiredElement(document.body, '.cycle-drawer')).value).toBe(
            String(earlierSignal.basalBodyTemperatureCelsius),
        );
    });
});

function cycleAdvancedDayFixture(ovulationTestResult: OvulationTestResult): {
    earlierSignal: FertilitySignal;
    earlierNote: string;
    todaySignal: FertilitySignal;
    todayNote: string;
    symptom: CycleSymptomEntry;
    profile: CycleResponse;
} {
    const todayDate = calendarDate(TODAY);
    const earlierDate = calendarDate('2026-08-16');
    const earlierNote = 'Earlier morning note';
    const todayNote = 'Today’s morning note';
    const earlierSignal: FertilitySignal = {
        id: entityId<'fertility-signal'>('earlier-temperature'),
        cycleProfileId: CYCLE_ID,
        date: earlierDate,
        basalBodyTemperatureCelsius: 36.51,
        ovulationTestResult: OVULATION_TEST_RESULT_UNKNOWN,
    };
    const todaySignal: FertilitySignal = {
        ...earlierSignal,
        id: entityId<'fertility-signal'>('today-temperature'),
        date: todayDate,
        basalBodyTemperatureCelsius: 36.61,
        ovulationTestResult,
    };
    const symptom: CycleSymptomEntry = {
        id: entityId<'cycle-symptom-entry'>('today-headache'),
        cycleProfileId: CYCLE_ID,
        date: todayDate,
        category: 7,
        intensity: 5,
        tags: [],
    };
    const profile = cycleProfile({
        consents: [
            cycleConsent('tracking', CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING),
            cycleConsent('fertility', CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS),
        ],
        fertilitySignals: [earlierSignal, todaySignal],
        dayNotes: [
            { date: earlierDate, notes: earlierNote },
            { date: todayDate, notes: todayNote },
        ],
        symptoms: [symptom],
    });
    return { earlierSignal, earlierNote, todaySignal, todayNote, symptom, profile };
}

function openTodayAdvancedEditor(harness: RouterTestingHarness, host: HTMLElement): HTMLElement {
    click('CYCLE_TRACKING.EDIT_TODAY', host);
    harness.detectChanges();
    click('CYCLE_TRACKING.SHOW_ADVANCED');
    harness.detectChanges();
    return requiredElement(document.body, '.cycle-drawer');
}

function cycleTemperatureInput(editor: HTMLElement): HTMLInputElement {
    return requiredElementOfType(editor, '.cycle-drawer__advanced-fields input[type="number"]', HTMLInputElement);
}

function cycleHistoryDayWithNote(host: HTMLElement, note: string): HTMLElement {
    const row = Array.from(host.querySelectorAll<HTMLElement>('fd-cycle-day-item')).find(element => element.textContent.includes(note));
    if (row === undefined) {
        throw new Error(`Expected to find the cycle day with note ${note}`);
    }
    return row;
}

describe('Cycle day editor ownership during a profile refresh', () => {
    it('keeps old day notes with the profile that opened the editor', async () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const http = TestBed.inject(HttpTestingController);
        const transport = TestBed.runInInjectionContext(() => new CyclesService());
        upsertDay.mockImplementation((id, payload) => transport.upsertDay(id, payload));
        const removedNote = 'Day removed before retrying the current cycle';
        const editedDate = calendarDate('2026-08-15');
        const oldNote = 'Original profile journal note';
        const replacementNote = 'Replacement journal note';
        const original = cycleProfile({
            dayNotes: [
                { date: calendarDate('2026-08-16'), notes: removedNote },
                { date: editedDate, notes: oldNote },
            ],
        });
        const replacementId = entityId<'cycle-profile'>('00000000-0000-4000-8000-000000000013');
        const replacement = cycleProfile({ id: replacementId, dayNotes: [{ date: editedDate, notes: replacementNote }] });
        const reload = new Subject<CycleResponse>();
        getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
        clearDay.mockReturnValueOnce(of(void 0));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = await retryAfterJournalRemovalAsync(harness, removedNote);
        expect(reload.observed).toBe(true);
        harness.detectChanges();
        const oldDrawer = openCalendarDayDrawer(harness, editedDate);
        const oldNotes = requiredElementOfType(oldDrawer, 'textarea', HTMLTextAreaElement);
        expect(oldNotes.value).toBe(oldNote);
        input(oldNotes, 'Unsaved note for the previous profile');
        harness.detectChanges();
        const oldSave = button('CYCLE_TRACKING.UPDATE_DAY', oldDrawer);
        const request = { method: 'PUT', url: `${environment.apiUrls.cycles}/${replacementId}/days` };

        reload.next(replacement);
        reload.complete();
        expect(oldSave.isConnected).toBe(true);
        expect(oldSave.disabled).toBe(false);
        oldSave.click();
        http.expectNone(request, 'the old calendar form must not save personal notes in the replacement profile');
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(oldDrawer.isConnected).toBe(false);

        const newDrawer = openCalendarDayDrawer(harness, editedDate);
        const newNotes = requiredElementOfType(newDrawer, 'textarea', HTMLTextAreaElement);
        expect(newNotes.value).toBe(replacementNote);
        const savedNote = 'Updated replacement journal note';
        input(newNotes, savedNote);
        harness.detectChanges();
        click('CYCLE_TRACKING.UPDATE_DAY', newDrawer);
        const saved = http.expectOne(request);
        expect(saved.request.body).toMatchObject({ date: editedDate, notes: savedNote });
        getCurrent.mockReturnValue(of({ ...replacement, dayNotes: [{ date: editedDate, notes: savedNote }] }));
        saved.flush({
            date: editedDate,
            cycleProfileId: replacementId,
            bleedingEntries: [],
            symptoms: [],
            fertilitySignal: null,
            notes: savedNote,
        });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(cycleHistoryDayWithNote(host, savedNote).textContent).toContain(savedNote);
        http.verify();
    });
});

function openCalendarDayDrawer(harness: RouterTestingHarness, date: string): HTMLElement {
    const calendar = requiredElementOfType(cycleRouteHost(harness), 'details:has(fd-cycle-calendar-card)', HTMLDetailsElement);
    if (!calendar.open) {
        requiredElement(calendar, 'summary').click();
    }
    requiredElementOfType(calendar, `[data-date="${date}"]`, HTMLButtonElement).click();
    harness.detectChanges();
    return requiredElement(document.body, '.cycle-drawer');
}

describe('Cycle journal calendar-day ownership', () => {
    it('updates the chosen saved day without allowing its draft to overwrite a neighboring day', async () => {
        const editedDate = calendarDate('2026-08-15');
        const otherDate = calendarDate('2026-08-16');
        const editedNote = 'Journal entry selected in the calendar';
        const otherNote = 'Independent journal entry';
        const updatedNote = 'Corrected journal entry';
        const profile = cycleProfile({
            dayNotes: [
                { date: editedDate, notes: editedNote },
                { date: otherDate, notes: otherNote },
            ],
            bleedingEntries: [editedDate, otherDate].map(date => ({
                id: entityId<'bleeding-entry'>(`entry-${date}`),
                cycleProfileId: CYCLE_ID,
                date,
                type: BLEEDING_TYPE_BLEEDING,
                flow: CYCLE_FLOW_MEDIUM,
            })),
        });
        getCurrent.mockReturnValue(of(profile));
        upsertDay.mockImplementation((_profileId, payload) =>
            of({
                date: payload.date,
                cycleProfileId: CYCLE_ID,
                bleedingEntries: [],
                symptoms: [],
                fertilitySignal: null,
                notes: payload.notes,
            }),
        );
        await renderAsync();
        const calendarSection = requiredElement(page, 'details:has(fd-cycle-calendar-card)');
        requiredElement(calendarSection, 'summary').click();
        expect(calendarSection.hasAttribute('open')).toBe(true);
        const calendar = requiredElement(calendarSection, 'fd-cycle-calendar-card');
        requiredElementOfType(calendar, `[data-date="${editedDate}"]`, HTMLButtonElement).click();
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const notes = requiredElementOfType(drawer, 'textarea', HTMLTextAreaElement);
        expect(notes.value).toBe(editedNote);

        const trigger = requiredElementOfType(drawer, 'fd-ui-date-input button[aria-haspopup="dialog"]', HTMLButtonElement);
        trigger.click();
        await stabilizeAsync();
        const picker = document.getElementById(trigger.getAttribute('aria-controls') ?? '');
        picker?.querySelector<HTMLButtonElement>(`[data-date="${otherDate}"]`)?.click();
        await stabilizeAsync();
        click('CYCLE_TRACKING.BLEEDING_NONE', drawer);
        input(notes, updatedNote);
        click('CYCLE_TRACKING.UPDATE_DAY', drawer);
        await stabilizeAsync();

        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({ date: editedDate, clearBleeding: true, notes: updatedNote }),
        );
        expect(picker).toBeNull();
        expect(trigger.disabled).toBe(true);
        expect(cycleHistoryDayWithNote(page, otherNote).textContent).toContain(otherNote);
    });

    it('lets a new journal entry choose a different calendar date before saving', async () => {
        const chosenDate = calendarDate('2026-08-16');
        const note = 'Note logged for yesterday';
        upsertDay.mockReturnValueOnce(
            of({ date: chosenDate, cycleProfileId: CYCLE_ID, bleedingEntries: [], symptoms: [], fertilitySignal: null, notes: note }),
        );
        await renderAsync();
        click('CYCLE_TRACKING.LOG_TODAY', page);
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        await pickDateAsync(requiredElement(drawer, 'fd-ui-date-input'), chosenDate);
        input(requiredElementOfType(drawer, 'textarea', HTMLTextAreaElement), note);
        click('CYCLE_TRACKING.SAVE_DAY', drawer);
        await stabilizeAsync();

        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ date: chosenDate, notes: note }));
        expect(cycleHistoryDayWithNote(page, note).textContent).toContain(note);
    });
});

describe('Cycle history refresh after saving a journal day', () => {
    const refreshFailures = ['network error', 'no profile', 'another profile'] as const;

    it.each(refreshFailures)('keeps the saved entry and retries only the read after %s', async failure => {
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
            refreshing.next(failure === 'no profile' ? null : cycleProfile({ id: entityId<'cycle-profile'>('replacement-cycle') }));
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

it.each(['write', 'refresh', 'error'] as const)('Cycle journal save when Retry reloads the profile ignores an old %s', async outcome => {
    const date = calendarDate(TODAY);
    const removedDate = calendarDate('2026-08-16');
    const oldNote = 'Original profile note';
    const replacedNote = 'Replacement profile note';
    const submittedNote = 'Edit sent to the original profile';
    const removedNote = 'Earlier entry to remove';
    const original = cycleProfile({
        dayNotes: [
            { date, notes: oldNote },
            { date: removedDate, notes: removedNote },
        ],
    });
    const replacement = cycleProfile({
        id: entityId<'cycle-profile'>('replacement-cycle'),
        dayNotes: [{ date, notes: replacedNote }],
    });
    const reload = new Subject<CycleResponse>();
    const refresh = new Subject<CycleResponse | null>();
    const write = new Subject<CycleLogDay>();
    const savedDay = { date, cycleProfileId: CYCLE_ID, bleedingEntries: [], symptoms: [], fertilitySignal: null, notes: submittedNote };
    getCurrent
        .mockReturnValueOnce(of(original))
        .mockReturnValueOnce(of(null))
        .mockReturnValueOnce(reload)
        .mockReturnValue(outcome === 'refresh' ? refresh : of(original));
    clearDay.mockReturnValueOnce(of(void 0));
    upsertDay.mockReturnValueOnce(write);
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const isSaving = harness.routeDebugElement?.injector.get(CycleTrackingFacade).isSavingDay;
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(reload.observed).toBe(true);
    harness.detectChanges();
    click('CYCLE_TRACKING.EDIT_TODAY', host);
    harness.detectChanges();
    const notes = requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement);
    expect(notes.value).toBe(oldNote);
    input(notes, submittedNote);
    harness.detectChanges();
    click('CYCLE_TRACKING.UPDATE_DAY');
    await vi.waitFor(() => {
        expect(upsertDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ date, notes: submittedNote }));
        expect(write.observed).toBe(true);
    });

    if (outcome === 'refresh') {
        write.next(savedDay);
        write.complete();
        await vi.waitFor(() => {
            expect(refresh.observed).toBe(true);
        });
    }
    reload.next(replacement);
    reload.complete();
    harness.detectChanges();
    expect(host.textContent).toContain(replacedNote);
    const editorClosedOnReload = document.querySelector('#cycle-day-editor-title') === null;

    const readsAfterReload = getCurrent.mock.calls.length;
    if (outcome === 'error') {
        write.error(API_ERROR);
    } else if (outcome === 'write') {
        write.next(savedDay);
        write.complete();
    } else {
        refresh.next(null);
        refresh.complete();
    }
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(isSaving?.()).toBe(false);
    });

    expect(host.textContent).toContain(replacedNote);
    expect(host.textContent).not.toContain(submittedNote);
    expect(host.textContent).not.toContain('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
    expect(getCurrent).toHaveBeenCalledTimes(readsAfterReload);
    expect(editorClosedOnReload).toBe(true);
    expect(document.querySelector('#cycle-day-editor-title')).toBeNull();
    click('CYCLE_TRACKING.EDIT_TODAY', host);
    harness.detectChanges();
    expect(requiredElementOfType(document.body, '.cycle-drawer textarea', HTMLTextAreaElement).value).toBe(replacedNote);
});

async function retryAfterJournalRemovalAsync(harness: RouterTestingHarness, note: string): Promise<HTMLElement> {
    const host = cycleRouteHost(harness);
    confirmJournalRemoval(harness, cycleHistoryDayWithNote(host, note));
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(host.textContent).toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
    });
    click('CYCLE_TRACKING.RETRY_LOAD', host);
    return host;
}

it('keeps a journal removal confirmation on the profile that opened it', async () => {
    const removedDate = calendarDate('2026-08-16');
    const retainedDate = calendarDate(TODAY);
    const removedNote = 'Earlier entry to remove before retry';
    const originalNote = 'Original profile entry';
    const replacementNote = 'Replacement entry on the same date';
    const original = cycleProfile({
        dayNotes: [
            { date: removedDate, notes: removedNote },
            { date: retainedDate, notes: originalNote },
        ],
    });
    const replacement = cycleProfile({
        id: entityId<'cycle-profile'>('replacement-journal-confirmation'),
        dayNotes: [{ date: retainedDate, notes: replacementNote }],
    });
    const retry = new Subject<CycleResponse>();
    getCurrent
        .mockReturnValueOnce(of(original))
        .mockReturnValueOnce(of(null))
        .mockReturnValueOnce(retry)
        .mockReturnValue(of({ ...replacement, dayNotes: [] }));
    clearDay.mockReturnValue(of(void 0));
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(retry.observed).toBe(true);

    click('CYCLE_TRACKING.CLEAR_DAY', cycleHistoryDayWithNote(host, originalNote));
    harness.detectChanges();
    const originalConfirmation = requiredElement(document.body, CONFIRM_DIALOG);
    loadReplacementCycle(retry, replacement, harness);
    expect(cycleHistoryDayWithNote(host, replacementNote).textContent).toContain(replacementNote);

    click('CYCLE_TRACKING.CLEAR_DAY', originalConfirmation);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(clearDay.mock.calls).toEqual([[CYCLE_ID, removedDate]]);

    confirmJournalRemoval(harness, cycleHistoryDayWithNote(host, replacementNote));
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(host.textContent).not.toContain(replacementNote);
    });
    expect(clearDay.mock.calls).toEqual([
        [CYCLE_ID, removedDate],
        [replacement.id, retainedDate],
    ]);
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

describe('Cycle journal removal after loading a replacement profile', () => {
    it.each(['removal', 'refresh', 'error'] as const)('ignores an old %s response after retry loads another profile', async stage => {
        const latestDate = calendarDate('2026-08-16');
        const date = calendarDate('2026-08-15');
        const previous = cycleProfile({
            dayNotes: [
                { date: latestDate, notes: 'First journal entry' },
                { date, notes: 'Old journal entry' },
            ],
        });
        const nextNote = 'Journal entry from the new profile';
        const replacement = cycleProfile({
            id: entityId<'cycle-profile'>('replacement-cycle'),
            dayNotes: [{ date, notes: nextNote }],
        });
        const removing = new Subject<void>();
        const refreshing = new Subject<CycleResponse | null>();
        const pending = stage === 'refresh' ? refreshing : removing;
        clearDay.mockReturnValueOnce(of(void 0)).mockReturnValueOnce(stage === 'refresh' ? of(void 0) : removing);
        getCurrent.mockReturnValueOnce(of(previous)).mockReturnValueOnce(of(null));
        if (stage === 'refresh') {
            getCurrent.mockReturnValueOnce(refreshing);
        }
        getCurrent.mockReturnValue(of(replacement));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);

        confirmJournalRemoval(harness, requiredElement(host, 'fd-cycle-day-item:first-of-type'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(host.textContent).toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
        });
        const row = requiredElement(host, 'fd-cycle-day-item');
        expect(row.textContent).toContain('Old journal entry');
        confirmJournalRemoval(harness, row);
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });
        expect(clearDay).toHaveBeenLastCalledWith(CYCLE_ID, date);

        click('CYCLE_TRACKING.RETRY_LOAD', host);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, 'fd-cycle-day-item').textContent).toContain(nextNote);
        });

        const readsAfterRetry = getCurrent.mock.calls.length;
        if (stage === 'error') {
            removing.error(API_ERROR);
        } else {
            removing.next();
            removing.complete();
        }
        refreshing.next(null);
        refreshing.complete();
        harness.detectChanges();

        expect(host.textContent).toContain(nextNote);
        expect(host.textContent).not.toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
        expect(host.textContent).not.toContain('CYCLE_TRACKING.CLEAR_DAY_FAILED');
        expect(button('CYCLE_TRACKING.CLEAR_DAY', requiredElement(host, 'fd-cycle-day-item')).disabled).toBe(false);
        expect(getCurrent).toHaveBeenCalledTimes(readsAfterRetry);
    });
});

function confirmJournalRemoval(harness: RouterTestingHarness, row: HTMLElement): void {
    click('CYCLE_TRACKING.CLEAR_DAY', row);
    harness.detectChanges();
    click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, CONFIRM_DIALOG));
}

describe('Cycle journal deletion recovery', () => {
    it('blocks day editing during deletion and restores calendar actions and the same rows on failure', async () => {
        const targetDate = calendarDate('2026-08-16');
        const retainedNote = { date: calendarDate('2026-08-15'), notes: 'Retained journal entry' };
        const targetNote = { date: targetDate, notes: 'Journal entry to remove' };
        getCurrent
            .mockReturnValueOnce(of(cycleProfile({ dayNotes: [retainedNote, targetNote], menstrualEpisodes: undefined })))
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
        const competingRemoval = button('CYCLE_TRACKING.CLEAR_DAY', retained);
        click('CYCLE_TRACKING.CLEAR_DAY', requiredElement(document.body, '[role="dialog"]'));
        expect(pending.observed).toBe(true);
        expect(competingRemoval.disabled).toBe(false);
        competingRemoval.click();
        await stabilizeAsync();
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        expect(clearDay).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, targetDate);
        expect(button('CYCLE_TRACKING.CLEAR_DAY', target).disabled).toBe(true);
        expect(button('CYCLE_TRACKING.CLEAR_DAY', retained).disabled).toBe(true);
        expect(button('CYCLE_TRACKING.EDIT_DAY', retained).disabled).toBe(true);

        const overview = requiredElement(page, 'fd-cycle-overview-card');
        click('CYCLE_TRACKING.LOG_TODAY', overview);
        requiredElementOfType(overview, 'button[aria-current="date"]', HTMLButtonElement).click();
        await stabilizeAsync();
        expect(page.querySelector('fd-cycle-day-editor-drawer')).toBeNull();
        expect(upsertDay).not.toHaveBeenCalled();

        pending.error(API_ERROR);
        await stabilizeAsync();
        expect(getCurrent).toHaveBeenCalledOnce();
        expect(target.isConnected).toBe(true);
        expect(retained.isConnected).toBe(true);
        expect(page.textContent).toContain('CYCLE_TRACKING.CLEAR_DAY_FAILED');
        expect(button('CYCLE_TRACKING.CLEAR_DAY', target).disabled).toBe(false);
        expect(button('CYCLE_TRACKING.EDIT_DAY', retained).disabled).toBe(false);

        click('CYCLE_TRACKING.LOG_TODAY', overview);
        await stabilizeAsync();
        expect(page.querySelector('fd-cycle-day-editor-drawer')).not.toBeNull();
        click('COMMON.CANCEL', requiredElement(document.body, '.cycle-drawer'));
        await stabilizeAsync();
        expect(page.querySelector('fd-cycle-day-editor-drawer')).toBeNull();

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
    it('shows the newest episode first and keeps both date ranges when the history language changes', async () => {
        const start = calendarDate(TRACKING_START);
        const end = calendarDate('2026-08-05');
        const laterStart = calendarDate('2026-08-10');
        const laterEnd = calendarDate('2026-08-13');
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: calendarDate(`${start}T00:00:00Z`),
            endDate: calendarDate(`${end}T00:00:00Z`),
            status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
            excludedFromPredictions: false,
        };
        getCurrent.mockReturnValue(
            of(
                cycleProfile({
                    menstrualEpisodes: [
                        episode,
                        {
                            ...episode,
                            id: entityId<'menstrual-episode'>('later-episode'),
                            startDate: laterStart,
                            endDate: laterEnd,
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
        const displayedRanges = Array.from(page.querySelectorAll('.cycle-tracking__episode strong'));
        expect(displayedRanges.map(range => range.textContent.trim())).toEqual([
            cycleEpisodeRange('en-US', laterStart, laterEnd),
            cycleEpisodeRange('en-US', start, end),
        ]);

        await firstValueFrom(translate.use('ru'));
        await stabilizeAsync();

        expect(displayedRanges.map(range => range.textContent.trim())).toEqual([
            cycleEpisodeRange('ru-RU', laterStart, laterEnd),
            cycleEpisodeRange('ru-RU', start, end),
        ]);
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

describe('Cycle period confirmation after loading a replacement profile', () => {
    it.each(['success', 'failure'] as const)('ignores an old period confirmation %s after Retry loads another profile', async outcome => {
        const date = calendarDate('2026-08-16');
        const journalDate = calendarDate(TODAY);
        const episode = {
            id: EPISODE_ID,
            cycleProfileId: CYCLE_ID,
            startDate: date,
            endDate: null,
            status: MENSTRUAL_EPISODE_STATUS_INFERRED,
            excludedFromPredictions: false,
        };
        const original = cycleProfile({
            dayNotes: [{ date: journalDate, notes: 'Journal entry to remove' }],
            menstrualEpisodes: [episode],
            bleedingEntries: [
                {
                    id: entityId<'bleeding-entry'>('inferred-bleeding'),
                    cycleProfileId: CYCLE_ID,
                    date,
                    type: BLEEDING_TYPE_BLEEDING,
                    flow: CYCLE_FLOW_MEDIUM,
                },
            ],
        });
        const replacementNote = 'Journal entry from the replacement profile';
        const replacement = cycleProfile({
            id: entityId<'cycle-profile'>('replacement-cycle'),
            dayNotes: [{ date, notes: replacementNote }],
        });
        const pending = new Subject<CycleResponse>();
        getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(of(replacement));
        clearDay.mockReturnValueOnce(of(void 0));
        confirmPeriodStart.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = cycleRouteHost(harness);

        confirmJournalRemoval(harness, requiredElement(host, 'fd-cycle-day-item:first-of-type'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(host.textContent).toContain('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
        });
        click('CYCLE_TRACKING.CONFIRM_PERIOD_START', requiredElement(host, 'fd-cycle-day-item'));
        harness.detectChanges();
        expect(confirmPeriodStart).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, date);

        click('CYCLE_TRACKING.RETRY_LOAD', host);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, 'fd-cycle-day-item').textContent).toContain(replacementNote);
        });

        if (outcome === 'success') {
            pending.next({ ...original, menstrualEpisodes: [{ ...episode, status: MENSTRUAL_EPISODE_STATUS_CONFIRMED }] });
            pending.complete();
        } else {
            pending.error(API_ERROR);
        }
        harness.detectChanges();

        const row = requiredElement(host, 'fd-cycle-day-item');
        expect(row.textContent).toContain(replacementNote);
        expect(row.textContent).not.toContain('CYCLE_TRACKING.PERIOD_START_CONFIRMED');
        expect(host.textContent).not.toContain('CYCLE_TRACKING.CONFIRM_PERIOD_START_FAILED');
        expect(button('CYCLE_TRACKING.EDIT_DAY', row).disabled).toBe(false);
    });
});

describe('Cycle settings duration validation', () => {
    it('announces an empty duration and only saves after correcting it', async () => {
        const averageCycleLength = 30;
        getCurrent.mockReturnValueOnce(of(cycleProfile()));
        updateSettings.mockReturnValueOnce(of(cycleProfile({ averageCycleLength })));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        click('CYCLE_TRACKING.SETTINGS_TITLE', cycleRouteHost(harness));
        harness.detectChanges();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        const length = settingsLength(drawer);

        input(length, '');
        harness.detectChanges();
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await harness.fixture.whenStable();
        harness.detectChanges();

        const error = requiredElement(drawer, 'fd-ui-input [role="alert"]');
        expect(error.textContent).toContain('CYCLE_TRACKING.AVG_LENGTH_ERROR');
        expect(length.getAttribute('aria-invalid')).toBe('true');
        expect(length.getAttribute('aria-describedby')).toContain(error.id);
        expect(updateSettings).not.toHaveBeenCalled();
        expect(updateConsent).not.toHaveBeenCalled();

        input(length, String(averageCycleLength));
        harness.detectChanges();
        expect(length.getAttribute('aria-invalid')).toBeNull();
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector('#cycle-settings-title')).toBeNull();
        });
        expect(updateSettings).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ averageCycleLength }));
        expect(updateConsent).not.toHaveBeenCalled();
    });
});

describe('Cycle settings draft during same-profile Retry', () => {
    it('keeps unsaved settings while Retry refreshes the same profile', async () => {
        const removedDate = calendarDate('2026-08-16');
        const removedNote = 'Earlier entry to remove before refreshing settings';
        const original = cycleProfile({ dayNotes: [{ date: removedDate, notes: removedNote }] });
        const savedLength = 32;
        const draftLength = 31;
        const refreshed = { ...original, dayNotes: [], averageCycleLength: savedLength };
        const reload = new Subject<CycleResponse>();
        getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
        clearDay.mockReturnValueOnce(of(void 0));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = await retryAfterJournalRemovalAsync(harness, removedNote);
        expect(reload.observed).toBe(true);
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        input(settingsLength(drawer), String(draftLength));
        harness.detectChanges();
        expect(settingsLength(drawer).value).toBe(String(draftLength));

        reload.next(refreshed);
        reload.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(drawer.isConnected).toBe(true);
        expect(settingsLength(drawer).value).toBe(String(draftLength));
        expect(updateSettings).not.toHaveBeenCalled();
        click('COMMON.CANCEL', drawer);
        harness.detectChanges();
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        expect(settingsLength(requiredElement(document.body, '.cycle-drawer')).value).toBe(String(savedLength));
    });
});

describe('Cycle settings after Retry changes profiles', () => {
    it.each(['settings', 'consent', 'failure'] as const)('does not apply a late %s result to the replacement profile', async outcome => {
        const state = await submitSettingsWhileRetryPendingAsync();
        const { harness, host, drawer, reload, settings, attempted, replacement, replacementNote, nutrition } = state;
        if (outcome === 'consent') {
            updateConsent.mockReturnValueOnce(nutrition);
            settings.next(attempted);
            settings.complete();
            await vi.waitFor(() => {
                expect(nutrition.observed).toBe(true);
                expect(updateConsent).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS, {
                    granted: true,
                });
            });
        }
        loadReplacementCycle(reload, replacement, harness);
        expect(host.textContent).toContain(replacementNote);
        const oldDrawerClosed = !drawer.isConnected;
        const permissionsAfterReload = updateConsent.mock.calls.length;
        const readsAfterReload = getNutritionSummary.mock.calls.length;

        if (outcome === 'failure') {
            settings.error(API_ERROR);
        } else if (outcome === 'settings') {
            settings.next(attempted);
            settings.complete();
        } else {
            nutrition.next({
                ...attempted,
                consents: [...(attempted.consents ?? []), cycleConsent('nutrition', CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS)],
            });
            nutrition.complete();
        }
        const saving = harness.routeDebugElement?.injector.get(CycleTrackingFacade).isSavingSettings;
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(saving?.()).toBe(false);
        });

        expect(host.textContent).toContain(replacementNote);
        expect(document.body.textContent).not.toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
        expect(updateConsent).toHaveBeenCalledTimes(permissionsAfterReload);
        expect(getNutritionSummary).toHaveBeenCalledTimes(readsAfterReload);
        expect(oldDrawerClosed).toBe(true);
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const currentDrawer = requiredElement(document.body, '.cycle-drawer');
        expect(settingsLength(currentDrawer).value).toBe(String(replacement.averageCycleLength));
        expect(permissionCheckbox(currentDrawer, 'CYCLE_TRACKING.CONSENT_NUTRITION').checked).toBe(false);
        expect(permissionCheckbox(currentDrawer, 'CYCLE_TRACKING.CONSENT_FERTILITY').checked).toBe(false);
    });
});

describe('Cycle settings after Retry changes profiles during a pending request', () => {
    it('opens and saves replacement settings before the previous request responds', async () => {
        const { harness, host, drawer, reload, settings, attempted, replacement, replacementNote } =
            await submitSettingsWhileRetryPendingAsync();
        const nextSave = new Subject<CycleResponse>();
        updateSettings.mockReturnValueOnce(nextSave);
        const replacementLength = 33;

        loadReplacementCycle(reload, replacement, harness);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(drawer.isConnected).toBe(false);
            expect(settings.observed).toBe(false);
        });
        await harness.fixture.whenStable();
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const currentDrawer = requiredElement(document.body, '.cycle-drawer');
        const length = settingsLength(currentDrawer);
        expect(length.value).toBe(String(replacement.averageCycleLength));
        expect(length.disabled).toBe(false);
        input(length, String(replacementLength));
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(button('CYCLE_TRACKING.SAVE_SETTINGS', currentDrawer).disabled).toBe(false);
        click('CYCLE_TRACKING.SAVE_SETTINGS', currentDrawer);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(updateSettings).toHaveBeenLastCalledWith(
                replacement.id,
                expect.objectContaining({ averageCycleLength: replacementLength }),
            );
            expect(nextSave.observed).toBe(true);
        });

        expect(settings.observed).toBe(false);
        settings.next(attempted);
        settings.complete();
        harness.detectChanges();
        expect(button('CYCLE_TRACKING.SAVE_SETTINGS', currentDrawer).disabled).toBe(true);
        expect(host.textContent).toContain(replacementNote);
        expect(updateConsent).not.toHaveBeenCalled();

        nextSave.next({ ...replacement, averageCycleLength: replacementLength });
        nextSave.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(currentDrawer.isConnected).toBe(false);
        });
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        expect(settingsLength(requiredElement(document.body, '.cycle-drawer')).value).toBe(String(replacementLength));
    });
});

async function submitSettingsWhileRetryPendingAsync(): Promise<{
    harness: RouterTestingHarness;
    host: HTMLElement;
    drawer: HTMLElement;
    reload: Subject<CycleResponse>;
    settings: Subject<CycleResponse>;
    attempted: CycleResponse;
    replacement: CycleResponse;
    replacementNote: string;
    nutrition: Subject<CycleResponse>;
}> {
    const date = calendarDate(TODAY);
    const removedDate = calendarDate('2026-08-16');
    const removedNote = 'Earlier entry to remove before changing settings';
    const replacementNote = 'Replacement profile journal';
    const original = cycleProfile({
        dayNotes: [{ date: removedDate, notes: removedNote }],
        consents: [cycleConsent('fertility', CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS)],
    });
    const replacement = cycleProfile({
        id: entityId<'cycle-profile'>('replacement-settings-cycle'),
        averageCycleLength: 32,
        dayNotes: [{ date, notes: replacementNote }],
    });
    const attempted = { ...original, averageCycleLength: 30, dayNotes: [] };
    const reload = new Subject<CycleResponse>();
    const settings = new Subject<CycleResponse>();
    const nutrition = new Subject<CycleResponse>();
    getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
    clearDay.mockReturnValueOnce(of(void 0));
    updateSettings.mockReturnValueOnce(settings);
    updateConsent.mockReturnValue(of(attempted));
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(reload.observed).toBe(true);
    click('CYCLE_TRACKING.SETTINGS_TITLE', host);
    harness.detectChanges();
    const drawer = requiredElement(document.body, '.cycle-drawer');
    input(settingsLength(drawer), String(attempted.averageCycleLength));
    permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_NUTRITION').click();
    permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_FERTILITY').click();
    harness.detectChanges();
    click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
    await vi.waitFor(() => {
        expect(settings.observed).toBe(true);
        expect(updateSettings).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({ averageCycleLength: attempted.averageCycleLength }),
        );
    });
    return { harness, host, drawer, reload, settings, attempted, replacement, replacementNote, nutrition };
}

function loadReplacementCycle(reload: Subject<CycleResponse>, replacement: CycleResponse, harness: RouterTestingHarness): void {
    reload.next(replacement);
    reload.complete();
    harness.detectChanges();
}

function settingsLength(drawer: HTMLElement): HTMLInputElement {
    return requiredElementOfType(drawer, 'input[aria-label="CYCLE_TRACKING.AVG_LENGTH"]', HTMLInputElement);
}

describe('Rendered Cycle Settings controls after the profile disappears', () => {
    it.each([
        { action: 'Save', label: 'CYCLE_TRACKING.SAVE_SETTINGS' },
        { action: 'Cancel', label: 'COMMON.CANCEL' },
    ])('ignores stale Cycle Settings $action before redrawing onboarding', async ({ label }) => {
        const removedNote = 'Journal entry removed before the settings refresh';
        const original = cycleProfile({ dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }] });
        const reload = new Subject<CycleResponse | null>();
        getCurrent.mockReturnValueOnce(of(original)).mockReturnValueOnce(of(null)).mockReturnValueOnce(reload);
        clearDay.mockReturnValueOnce(of(void 0));
        const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
        const host = await retryAfterJournalRemovalAsync(harness, removedNote);
        expect(reload.observed).toBe(true);

        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        input(settingsLength(drawer), String(original.averageCycleLength + 1));
        harness.detectChanges();
        const control = button(label, drawer);

        reload.next(null);
        reload.complete();
        expect(control.isConnected).toBe(true);
        expect(control.disabled).toBe(false);
        control.click();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(updateSettings).not.toHaveBeenCalled();
        expect(updateConsent).not.toHaveBeenCalled();
        expect(drawer.isConnected).toBe(false);
        expect(host.querySelector('fd-cycle-overview-card')).toBeNull();
        expect(requiredElement(host, '.cycle-tracking__setup-card').hidden).toBe(false);
    });
});

describe('Cycle settings after a stale same-profile read', () => {
    it('rereads the confirmed duration and retries only the permission that failed', async () => {
        const { harness, drawer, duration, freshRequested } = await prepareCycleSettingsRereadAsync(false);
        const persistedDuration = harness.routeDebugElement?.injector.get(CycleTrackingFacade).cycle()?.averageCycleLength;
        expect(settingsLength(drawer).value).toBe(String(duration));
        expect(button('CYCLE_TRACKING.SAVE_SETTINGS', drawer).disabled).toBe(false);
        click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector('#cycle-settings-title')).toBeNull();
        });

        expect({
            persistedDuration,
            settingsWrites: updateSettings.mock.calls.length,
            repeatedPermissions: updateConsent.mock.calls.slice(2),
            freshRequested,
            reads: getCurrent.mock.calls.length,
        }).toEqual({
            persistedDuration: duration,
            settingsWrites: 1,
            repeatedPermissions: [[CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false }]],
            freshRequested: true,
            reads: 4,
        });
    });

    it('shows the remote revocation from the fresh read when reopening settings', async () => {
        const { harness, host, drawer, duration, freshRequested } = await prepareCycleSettingsRereadAsync(true);
        click('COMMON.CANCEL', drawer);
        harness.detectChanges();
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const reopened = requiredElement(document.body, '.cycle-drawer');

        expect({
            duration: settingsLength(reopened).value,
            nutritionAllowed: permissionCheckbox(reopened, 'CYCLE_TRACKING.CONSENT_NUTRITION').checked,
            freshRequested,
            reads: getCurrent.mock.calls.length,
        }).toEqual({ duration: String(duration), nutritionAllowed: false, freshRequested: true, reads: 4 });
        expect(updateSettings).toHaveBeenCalledOnce();
        expect(updateConsent).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false });
    });

    it('keeps the confirmed duration when the fresh read fails and allows a new Retry', async () => {
        const { harness, host, drawer, duration, freshRequested } = await prepareCycleSettingsRereadAsync(false, API_ERROR);
        expect(freshRequested).toBe(true);
        expect(host.textContent).toContain('CYCLE_TRACKING.LOAD_FAILED');
        click('COMMON.CANCEL', drawer);
        harness.detectChanges();
        click('CYCLE_TRACKING.SETTINGS_TITLE', host);
        harness.detectChanges();
        const reopened = requiredElement(document.body, '.cycle-drawer');
        expect(settingsLength(reopened).value).toBe(String(duration));

        click('COMMON.CANCEL', reopened);
        harness.detectChanges();
        const readsBeforeRetry = getCurrent.mock.calls.length;
        click('CYCLE_TRACKING.RETRY_LOAD', host);
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(host.textContent).not.toContain('CYCLE_TRACKING.LOAD_FAILED');
        expect(getCurrent).toHaveBeenCalledTimes(readsBeforeRetry + 1);
        expect(updateSettings).toHaveBeenCalledOnce();
    });
});

type CycleSettingsRereadFixture = {
    harness: RouterTestingHarness;
    host: HTMLElement;
    drawer: HTMLElement;
    duration: number;
    freshRequested: boolean;
};

async function prepareCycleSettingsRereadAsync(
    nutritionRevokedElsewhere: boolean,
    freshReadError?: Error,
): Promise<CycleSettingsRereadFixture> {
    const removedNote = 'Earlier journal entry to remove before settings retry';
    const tracking = cycleConsent('tracking', CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING);
    const fertility = cycleConsent('fertility', CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS);
    const nutrition = cycleConsent('nutrition', CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS);
    const original = cycleProfile({
        consents: nutritionRevokedElsewhere ? [tracking, fertility, nutrition] : [tracking, fertility],
        dayNotes: [{ date: calendarDate('2026-08-16'), notes: removedNote }],
    });
    const olderSnapshot = { ...original, dayNotes: [] };
    const duration = 30;
    const updated = { ...olderSnapshot, averageCycleLength: duration };
    const withNutrition = { ...updated, consents: [tracking, fertility, nutrition] };
    const freshSnapshot = nutritionRevokedElsewhere
        ? {
              ...withNutrition,
              consents: [tracking, fertility, { ...nutrition, isActive: false, revokedAtUtc: utcInstant('2026-08-17T11:00:00Z') }],
          }
        : withNutrition;
    const completed = { ...withNutrition, consents: [tracking, { ...fertility, isActive: false }, nutrition] };
    const olderRead = new Subject<CycleResponse>();
    const freshRead = new Subject<CycleResponse>();
    const settings = new Subject<CycleResponse>();
    const permission = new Subject<CycleResponse>();
    getCurrent
        .mockReturnValueOnce(of(original))
        .mockReturnValueOnce(of(null))
        .mockReturnValueOnce(olderRead)
        .mockReturnValueOnce(freshRead)
        .mockReturnValue(of(freshSnapshot));
    clearDay.mockReturnValueOnce(of(void 0));
    updateSettings.mockReturnValueOnce(settings).mockReturnValue(of(withNutrition));
    if (!nutritionRevokedElsewhere) {
        updateConsent.mockReturnValueOnce(of(withNutrition));
    }
    updateConsent.mockReturnValueOnce(permission).mockReturnValueOnce(of(completed));
    const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
    const host = await retryAfterJournalRemovalAsync(harness, removedNote);
    expect(olderRead.observed).toBe(true);
    click('CYCLE_TRACKING.SETTINGS_TITLE', host);
    harness.detectChanges();
    const drawer = requiredElement(document.body, '.cycle-drawer');
    input(settingsLength(drawer), String(duration));
    if (!nutritionRevokedElsewhere) {
        permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_NUTRITION').click();
    }
    permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_FERTILITY').click();
    harness.detectChanges();
    click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(settings.observed).toBe(true);
    });

    settings.next(updated);
    settings.complete();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(permission.observed).toBe(true);
    });
    expect(updateSettings).toHaveBeenCalledExactlyOnceWith(CYCLE_ID, expect.objectContaining({ averageCycleLength: duration }));
    expect(updateConsent).toHaveBeenLastCalledWith(CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false });

    olderRead.next(olderSnapshot);
    olderRead.complete();
    permission.error(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }));
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(drawer.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
    });
    const freshRequested = freshRead.observed;
    if (freshRequested) {
        const result = freshReadError === undefined ? of(freshSnapshot) : throwError(() => freshReadError);
        result.subscribe(freshRead);
        await harness.fixture.whenStable();
        harness.detectChanges();
    }
    return { harness, host, drawer, duration, freshRequested };
}

describe('Cycle nutrition during settings revocation', () => {
    it.each(['already rendered', 'still in flight'] as const)(
        'hides a summary that is %s when nutrition consent is revoked while fertility is still saving',
        async phase => {
            const tracking = cycleConsent('tracking', CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING);
            const fertility = cycleConsent('fertility', CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS);
            const nutrition = cycleConsent('nutrition', CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS);
            const initial = cycleProfile({ consents: [tracking, fertility, nutrition] });
            const nutritionRevoked = { ...initial, consents: [tracking, fertility, { ...nutrition, isActive: false }] };
            const oldSummary = new Subject<CycleNutritionSummary>();
            const pendingFertility = new Subject<CycleResponse>();
            const previous: CycleNutritionSummary = {
                dateFrom: calendarDate(TRACKING_START),
                dateTo: calendarDate(TODAY),
                loggedCycleDays: 31,
                daysWithMeals: 4,
                bleedingDays: 2,
                averageCaloriesOnBleedingDays: 2100,
                averageCaloriesOnNonBleedingCycleDays: 1800,
                averageFiberOnBleedingDays: 18,
                averageFiberOnNonBleedingCycleDays: 28,
                averagePainImpactOnDaysWithMeals: 6,
                hasEnoughNutritionData: true,
            };
            getCurrent.mockReturnValueOnce(of(initial));
            getNutritionSummary.mockReturnValueOnce(phase === 'already rendered' ? of(previous) : oldSummary);
            updateConsent.mockReturnValueOnce(of(nutritionRevoked)).mockReturnValueOnce(pendingFertility);
            const harness = await RouterTestingHarness.create(CYCLE_ROUTE);
            const host = cycleRouteHost(harness);
            const card = requiredElement(host, 'fd-cycle-nutrition-summary-card');
            const loggedDays = (): string | undefined => card.querySelector('.cycle-tracking__nutrition-metric strong')?.textContent.trim();
            if (phase === 'already rendered') {
                expect(loggedDays()).toBe(String(previous.loggedCycleDays));
            } else {
                expect(oldSummary.observed).toBe(true);
                expect(card.textContent).toContain('CYCLE_TRACKING.LOADING');
            }

            click('CYCLE_TRACKING.SETTINGS_TITLE', host);
            harness.detectChanges();
            const drawer = requiredElement(document.body, '.cycle-drawer');
            permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_NUTRITION').click();
            permissionCheckbox(drawer, 'CYCLE_TRACKING.CONSENT_FERTILITY').click();
            harness.detectChanges();
            click('CYCLE_TRACKING.SAVE_SETTINGS', drawer);
            await vi.waitFor(() => {
                harness.detectChanges();
                expect(pendingFertility.observed).toBe(true);
            });
            expect(updateConsent.mock.calls).toEqual([
                [CYCLE_ID, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS, { granted: false }],
                [CYCLE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false }],
            ]);

            const obsoleteReadWasStillObserved = oldSummary.observed;
            oldSummary.next(previous);
            oldSummary.complete();
            harness.detectChanges();
            expect(loggedDays()).not.toBe(String(previous.loggedCycleDays));
            expect(obsoleteReadWasStillObserved).toBe(false);

            pendingFertility.error(API_ERROR);
            await vi.waitFor(() => {
                harness.detectChanges();
                expect(drawer.textContent).toContain('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
            });
            expect(loggedDays()).not.toBe(String(previous.loggedCycleDays));
        },
    );
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
        expect(updateSettings).toHaveBeenCalledExactlyOnceWith(
            CYCLE_ID,
            expect.objectContaining({ mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING, goal, reproductiveState }),
        );
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
    it('blocks WebMCP navigation with edited settings and closes a clean drawer before navigation', async () => {
        const screens = new WebmcpScreenRegistry<DevelopmentScreen>();
        TestBed.configureTestingModule({ providers: [{ provide: DEVELOPMENT_SCREENS, useValue: screens }] });
        await renderAsync();
        expect(screens.current()?.kind).toBe('cycle-tracking');

        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        const drawer = requiredElement(document.body, '.cycle-drawer');
        expect(screens.current()?.kind).toBe('cycle-settings');
        input(requiredElementOfType(drawer, 'input[type="number"]', HTMLInputElement), '32');
        await stabilizeAsync();
        expect(() => {
            screens.prepareNavigation(true);
        }).toThrow('Unsaved draft exists.');
        expect(drawer.isConnected).toBe(true);

        click('COMMON.CANCEL', drawer);
        await waitForDrawerCloseAsync('#cycle-settings-title');
        click('CYCLE_TRACKING.SETTINGS_TITLE', page);
        await stabilizeAsync();
        screens.prepareNavigation(true);
        await waitForDrawerCloseAsync('#cycle-settings-title');
        expect(screens.current()?.kind).toBe('cycle-tracking');
        expect(updateSettings).not.toHaveBeenCalled();
        expect(updateConsent).not.toHaveBeenCalled();
    });

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

function expectRequiredDatePicker(host: HTMLElement): void {
    const field = requiredElementOfType(host, 'input', HTMLInputElement);
    expect(field.readOnly).toBe(true);
    expect(field.getAttribute('aria-required')).toBe('true');
    expect(host.querySelector('button:not([aria-haspopup="dialog"])')).toBeNull();
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
