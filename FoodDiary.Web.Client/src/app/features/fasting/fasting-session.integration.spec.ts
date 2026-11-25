import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeRu from '@angular/common/locales/ru';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, NEVER, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { UserService } from '../../shared/api/user.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../shared/development/development-tools';
import { LocalizationService } from '../../shared/i18n/localization.service';
import type { FastingCheckIn, FastingMessage, FastingOverview, FastingSession } from '../../shared/models/fasting.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { FastingService } from './api/fasting.service';
import fastingRoutes from './fasting.routes';
import { FASTING_HARD_STOP_THRESHOLD_HOURS } from './lib/fasting-page.constants';

const FIXTURE_TIME = '2026-05-19T12:00:00Z';
const SESSION_START = '2026-05-18T12:00:00Z';
const INTERMITTENT_HOURS = 16;
const EXTENDED_HOURS = 28;
const REDUCED_HOURS = 4;
const CUSTOM_TARGET_HOURS = 26;
const CYCLIC_FAST_DAYS = 2;
const CYCLIC_EAT_DAYS = 1;
const CYCLIC_EAT_DAY_FAST_HOURS = 13;
const CYCLIC_EAT_DAY_WINDOW_HOURS = 11;
const SAVED_NOTE = 'Light walk before lunch';
const EXTENDED_WARNING_HOURS = 72;
const EXTEND_ONE_DAY_HOURS = 24;
const FINAL_HISTORY_PAGE = 2;
const RESTORED_INTERMITTENT_HOURS = 14;
const PROMPT_STORAGE_KEY = 'fd_fasting_prompt_state';
const REPLACEMENT_SESSION_ID = entityId<'fasting-session'>('session-replacement');

@Component({ template: '<h1>Another page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class AnotherPageComponent {}

type FastingApiMocks = {
    [
        Method in
            | 'getOverview'
            | 'getOverviewStrict'
            | 'getHistory'
            | 'start'
            | 'end'
            | 'updateCheckIn'
            | 'extend'
            | 'reduceTarget'
            | 'skipCyclicDay'
            | 'postponeCyclicDay'
    ]: Mock<FastingService[Method]>;
};

type FastingFixture = {
    harness: RouterTestingHarness;
    api: FastingApiMocks;
    developmentScreens: Map<string, DevelopmentScreen>;
    reportEvent: Mock<FrontendObservabilityService['recordFastingLifecycleEvent']>;
    showSuccess: Mock<FdUiToastService['success']>;
    showError: Mock<FdUiToastService['error']>;
};

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
    vi.useRealTimers();
});

async function renderFastingAsync(
    session: FastingSession | null,
    overview = makeOverview(session),
    systemTime = FIXTURE_TIME,
): Promise<FastingFixture> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(systemTime));
    const api: FastingApiMocks = {
        getOverview: vi.fn<FastingService['getOverview']>().mockReturnValue(of(overview)),
        getOverviewStrict: vi.fn<FastingService['getOverviewStrict']>().mockReturnValue(of(overview)),
        getHistory: vi.fn<FastingService['getHistory']>(),
        start: vi.fn<FastingService['start']>(),
        end: vi.fn<FastingService['end']>(),
        updateCheckIn: vi.fn<FastingService['updateCheckIn']>(),
        extend: vi.fn<FastingService['extend']>(),
        reduceTarget: vi.fn<FastingService['reduceTarget']>(),
        skipCyclicDay: vi.fn<FastingService['skipCyclicDay']>(),
        postponeCyclicDay: vi.fn<FastingService['postponeCyclicDay']>(),
    };
    const reportEvent = vi.fn<FrontendObservabilityService['recordFastingLifecycleEvent']>();
    const showSuccess = vi.fn<FdUiToastService['success']>();
    const showError = vi.fn<FdUiToastService['error']>();
    const developmentScreens = new Map<string, DevelopmentScreen>();
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'fasting', children: fastingRoutes },
                { path: 'another-page', component: AnotherPageComponent },
            ]),
            provideTranslateTesting(),
            { provide: FastingService, useValue: api },
            { provide: UserService, useValue: { user: signal(null) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: FrontendObservabilityService, useValue: { recordFastingLifecycleEvent: reportEvent } },
            { provide: FdUiToastService, useValue: { success: showSuccess, error: showError } },
            {
                provide: DEVELOPMENT_SCREENS,
                useValue: {
                    attach: (screen: DevelopmentScreen): (() => void) => {
                        developmentScreens.set(screen.kind, screen);
                        return (): void => {
                            developmentScreens.delete(screen.kind);
                        };
                    },
                },
            },
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setFallbackLang('en');
    translate.use('en');
    const harness = await RouterTestingHarness.create('/fasting');
    return { harness, api, developmentScreens, reportEvent, showSuccess, showError };
}

function makeSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return {
        id: entityId<'fasting-session'>('session-active'),
        startedAtUtc: utcInstant(SESSION_START),
        endedAtUtc: null,
        initialPlannedDurationHours: INTERMITTENT_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: INTERMITTENT_HOURS,
        protocol: 'Fast16Eat8',
        planType: 'Intermittent',
        occurrenceKind: 'FastingWindow',
        cyclicFastDays: null,
        cyclicEatDays: null,
        cyclicEatDayFastHours: null,
        cyclicEatDayEatingWindowHours: null,
        cyclicPhaseDayNumber: null,
        cyclicPhaseDayTotal: null,
        isCompleted: false,
        status: 'Active',
        notes: null,
        checkInAtUtc: null,
        hungerLevel: null,
        energyLevel: null,
        moodLevel: null,
        symptoms: [],
        checkInNotes: null,
        checkIns: [],
        ...overrides,
    };
}

function makeCyclicEatingDay(): FastingSession {
    return makeSession({
        planType: 'Cyclic',
        protocol: 'Cyclic',
        occurrenceKind: 'EatDay',
        initialPlannedDurationHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        plannedDurationHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        cyclicFastDays: CYCLIC_FAST_DAYS,
        cyclicEatDays: CYCLIC_EAT_DAYS,
        cyclicEatDayFastHours: CYCLIC_EAT_DAY_FAST_HOURS,
        cyclicEatDayEatingWindowHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        cyclicPhaseDayNumber: CYCLIC_EAT_DAYS,
        cyclicPhaseDayTotal: CYCLIC_EAT_DAYS,
    });
}

function makeOverview(session: FastingSession | null, history = session === null ? [] : [session]): FastingOverview {
    return {
        currentSession: session,
        stats: {
            totalCompleted: history.filter(item => item.isCompleted).length,
            currentStreak: 0,
            averageDurationHours: 0,
            completionRateLast30Days: 0,
            checkInRateLast30Days: 0,
            lastCheckInAtUtc: null,
            topSymptom: null,
        },
        insights: { alerts: [], insights: [] },
        history: { data: history, page: 1, limit: 10, totalPages: 1, totalItems: history.length },
    };
}

function getButton(root: ParentNode, label: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected a fasting action with label ${label}`);
    }
    return button;
}

function getDialog(selector: string): HTMLElement {
    const host = document.querySelector<HTMLElement>(selector);
    if (host === null) {
        throw new Error(`Expected the ${selector} dialog`);
    }
    return host;
}

function pageRoot(harness: RouterTestingHarness): HTMLElement {
    const page = harness.routeNativeElement;
    if (page === null) {
        throw new Error('Expected the fasting route to be rendered');
    }
    return page;
}

function redraw(harness: RouterTestingHarness): void {
    TestBed.tick();
    harness.detectChanges();
}

function enterNumber(dialog: HTMLElement, label: string, value: number, harness: RouterTestingHarness): void {
    const field = Array.from(dialog.querySelectorAll('label')).find(candidate => candidate.textContent.trim() === label)?.control;
    if (!(field instanceof HTMLInputElement)) {
        throw new TypeError(`Expected a number input with label ${label}`);
    }
    field.value = String(value);
    field.dispatchEvent(new Event('input', { bubbles: true }));
    redraw(harness);
}

function makePrompt(id: string, titleKey: string): FastingMessage {
    return { id, titleKey, bodyKey: 'FASTING.PROMPT_BODY', bodyParams: null, tone: 'neutral' };
}

function makeOverviewWithPrompts(session: FastingSession, alerts: FastingMessage[]): FastingOverview {
    return { ...makeOverview(session), insights: { alerts, insights: [] } };
}

async function revisitFastingAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.navigateByUrl('/another-page', AnotherPageComponent);
    expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
    await harness.navigateByUrl('/fasting');
    redraw(harness);
}

function getFastingPrompt(harness: RouterTestingHarness): HTMLElement {
    const alert = pageRoot(harness).querySelector<HTMLElement>('.fasting-redesign__alert[role="status"]');
    if (alert === null) {
        throw new Error('Expected a visible fasting prompt');
    }
    return alert;
}

it('updates the weekly rhythm when the idle fasting route is visible again on Monday', async () => {
    const completed = makeSession({
        startedAtUtc: utcInstant(new Date('2026-05-24T06:00:00').toISOString()),
        endedAtUtc: utcInstant(new Date('2026-05-24T22:00:00').toISOString()),
        isCompleted: true,
        status: 'Completed',
    });
    const { harness, api } = await renderFastingAsync(null, makeOverview(null, [completed]), '2026-05-24T23:59:00');
    const page = pageRoot(harness);
    const rhythmLabels = (): string[] => Array.from(page.querySelectorAll('.fasting-redesign__day strong'), day => day.textContent.trim());
    expect(rhythmLabels()).toEqual(['-', '-', '-', '-', '-', '-', '16:8']);
    expect(page.querySelectorAll('.fasting-redesign__history-row')).toHaveLength(1);

    vi.setSystemTime(new Date('2026-05-25T00:01:00'));
    document.dispatchEvent(new Event('visibilitychange'));
    redraw(harness);

    expect(rhythmLabels()).toEqual(['-', '-', '-', '-', '-', '-', '-']);
    expect(page.querySelectorAll('.fasting-redesign__history-row')).toHaveLength(1);
    expect(api.getOverview).toHaveBeenCalledOnce();
    expect(api.getOverviewStrict).not.toHaveBeenCalled();
});

describe('Rendered fasting reminders stored on the device', () => {
    it('recovers the first reminder when browser storage contains corrupt JSON', async () => {
        const session = makeSession();
        const alert = makePrompt('first', 'FASTING.FIRST_PROMPT');
        localStorage.setItem(PROMPT_STORAGE_KEY, '{broken state');

        const { harness } = await renderFastingAsync(session, makeOverviewWithPrompts(session, [alert]));

        expect(getFastingPrompt(harness).textContent).toContain(alert.titleKey);
        expect(localStorage.getItem(PROMPT_STORAGE_KEY)).toBeNull();
    });

    it('shows reminders with unreadable deadlines and preserves a saved dismissal', async () => {
        const session = makeSession();
        const dismissed = makePrompt('dismissed', 'FASTING.DISMISSED_PROMPT');
        const recovered = makePrompt('recovered', 'FASTING.RECOVERED_PROMPT');
        const unreadableDeadline = { snoozedUntilUtc: 'unreadable saved time' };
        localStorage.setItem(
            PROMPT_STORAGE_KEY,
            JSON.stringify({
                [`${session.id}:${dismissed.id}`]: { ...unreadableDeadline, dismissed: true },
                [`${session.id}:${recovered.id}`]: unreadableDeadline,
            }),
        );

        const { harness } = await renderFastingAsync(session, makeOverviewWithPrompts(session, [dismissed, recovered]));

        expect(getFastingPrompt(harness).textContent).toContain(recovered.titleKey);
        expect(getFastingPrompt(harness).textContent).not.toContain(dismissed.titleKey);
    });
});

describe('Rendered fasting reminder continuity', () => {
    it('hides a dismissed reminder across a route revisit and shows it for the next fast', async () => {
        const session = makeSession();
        const first = makePrompt('first', 'FASTING.FIRST_PROMPT');
        const next = makePrompt('next', 'FASTING.NEXT_PROMPT');
        const { harness, api } = await renderFastingAsync(session, makeOverviewWithPrompts(session, [first, next]));
        expect(getFastingPrompt(harness).textContent).toContain(first.titleKey);

        getButton(getFastingPrompt(harness), 'FASTING.PROMPTS.DISMISS').click();
        redraw(harness);
        expect(getFastingPrompt(harness).textContent).toContain(next.titleKey);
        expect(localStorage.getItem(PROMPT_STORAGE_KEY)).toBe(JSON.stringify({ [`${session.id}:${first.id}`]: { dismissed: true } }));

        await revisitFastingAsync(harness);
        expect(getFastingPrompt(harness).textContent).toContain(next.titleKey);

        const replacement = makeSession({ id: REPLACEMENT_SESSION_ID });
        api.getOverview.mockReturnValue(of(makeOverviewWithPrompts(replacement, [first, next])));
        await revisitFastingAsync(harness);
        expect(getFastingPrompt(harness).textContent).toContain(first.titleKey);
    });

    it.each([
        { description: 'a non-record entry', value: null },
        { description: 'an invalid dismissal', value: { dismissed: 'true' } },
        { description: 'an invalid deadline', value: { snoozedUntilUtc: 42 } },
    ])('ignores $description while retaining a valid dismissal', async ({ value }) => {
        const session = makeSession();
        const dismissed = makePrompt('dismissed', 'FASTING.DISMISSED_PROMPT');
        const visible = makePrompt('visible', 'FASTING.VISIBLE_PROMPT');
        localStorage.setItem(
            PROMPT_STORAGE_KEY,
            JSON.stringify({ [`${session.id}:${dismissed.id}`]: { dismissed: true }, [`${session.id}:${visible.id}`]: value }),
        );
        const { harness } = await renderFastingAsync(session, makeOverviewWithPrompts(session, [dismissed, visible]));
        expect(getFastingPrompt(harness).textContent).toContain(visible.titleKey);

        getButton(getFastingPrompt(harness), 'FASTING.PROMPTS.DISMISS').click();
        redraw(harness);
        expect(pageRoot(harness).querySelector('.fasting-redesign__alert')).toBeNull();
        expect(localStorage.getItem(PROMPT_STORAGE_KEY)).toBe(
            JSON.stringify({
                [`${session.id}:${dismissed.id}`]: { dismissed: true },
                [`${session.id}:${visible.id}`]: { dismissed: true },
            }),
        );
    });

    it('restores a saved reminder at its deadline even when the route has been closed', async () => {
        const session = makeSession();
        const deferred = makePrompt('deferred', 'FASTING.DEFERRED_PROMPT');
        const available = makePrompt('available', 'FASTING.AVAILABLE_PROMPT');
        const deadline = '2026-05-19T13:00:00Z';
        localStorage.setItem(
            PROMPT_STORAGE_KEY,
            JSON.stringify({
                [`${session.id}:${deferred.id}`]: { snoozedUntilUtc: deadline },
                [`${session.id}:${available.id}`]: { snoozedUntilUtc: '' },
            }),
        );
        const { harness } = await renderFastingAsync(session, makeOverviewWithPrompts(session, [deferred, available]));
        expect(getFastingPrompt(harness).textContent).toContain(available.titleKey);

        vi.setSystemTime(new Date(deadline));
        await revisitFastingAsync(harness);
        expect(getFastingPrompt(harness).textContent).toContain(deferred.titleKey);
    });
});

describe('Rendered extended fasting interruption', () => {
    it.each([
        { entry: 'page action', openManagement: false },
        { entry: 'management dialog', openManagement: true },
    ])('cancels and confirms interruption from the $entry', async ({ openManagement }) => {
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            initialPlannedDurationHours: EXTENDED_HOURS,
            plannedDurationHours: EXTENDED_HOURS,
        });
        const previousPrompt = { dismissed: true };
        const otherPrompt = { snoozedUntilUtc: '2026-05-19T18:00:00Z' };
        const previousPromptKey = `${session.id}:previous`;
        const otherPromptKey = `${REPLACEMENT_SESSION_ID}:previous`;
        localStorage.setItem(PROMPT_STORAGE_KEY, JSON.stringify({ [previousPromptKey]: previousPrompt, [otherPromptKey]: otherPrompt }));
        const { harness, api } = await renderFastingAsync(session);
        const response = new Subject<FastingSession>();
        api.end.mockReturnValueOnce(response);
        let actionRoot = pageRoot(harness);
        if (openManagement) {
            getButton(actionRoot, 'FASTING.REDESIGN.MANAGE').click();
            redraw(harness);
            actionRoot = getDialog('fd-fasting-session-management-dialog');
        }

        const interrupt = getButton(actionRoot, 'FASTING.INTERRUPT_FAST');
        interrupt.click();
        redraw(harness);
        const confirmDialog = getDialog('fd-fasting-end-confirm-dialog');
        expect(confirmDialog.textContent).toContain('FASTING.INTERRUPT_CONFIRM_TITLE');
        getButton(confirmDialog, 'FASTING.CANCEL_ACTION').click();
        redraw(harness);
        expect(api.end).not.toHaveBeenCalled();
        expect(localStorage.getItem(PROMPT_STORAGE_KEY)).toBe(
            JSON.stringify({ [previousPromptKey]: previousPrompt, [otherPromptKey]: otherPrompt }),
        );

        interrupt.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.INTERRUPT_FAST').click();
        redraw(harness);
        expect(api.end).toHaveBeenCalledOnce();
        const ended = { ...session, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Interrupted' } satisfies FastingSession;
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(null, [ended])));
        response.next(ended);
        response.complete();
        redraw(harness);

        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
        expect(pageRoot(harness).textContent).toContain('FASTING.BADGE_INTERRUPTED');
        expect(localStorage.getItem(PROMPT_STORAGE_KEY)).toBe(JSON.stringify({ [otherPromptKey]: otherPrompt }));
    });
});

describe('Rendered fasting check-in lifecycle', () => {
    it('retains a check-in draft when saving fails and closes the dialog after a successful retry', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api, showSuccess, reportEvent } = await renderFastingAsync(session);
        const firstSave = new Subject<FastingSession>();
        const nextSave = new Subject<FastingSession>();
        api.updateCheckIn.mockReturnValueOnce(firstSave).mockReturnValueOnce(nextSave);

        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHECK_IN').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-check-in-dialog');
        const notes = dialog.querySelector('textarea');
        if (notes === null) {
            throw new Error('Expected the check-in notes field');
        }
        notes.value = ` ${SAVED_NOTE} `;
        notes.dispatchEvent(new Event('input', { bubbles: true }));
        redraw(harness);
        const save = getButton(dialog, 'FASTING.CHECK_IN.SAVE');
        save.click();
        redraw(harness);
        expect(save.disabled).toBe(true);
        expectCheckInFieldsDisabled(dialog, true);
        save.click();
        expect(api.updateCheckIn).toHaveBeenCalledOnce();
        expect(api.updateCheckIn.mock.calls[0][0].checkInNotes).toBe(SAVED_NOTE);

        firstSave.error(new Error('Check-in unavailable'));
        redraw(harness);
        expect(notes.value).toBe(` ${SAVED_NOTE} `);
        expect(dialog.querySelector('[role="alert"]')?.textContent).toContain('FASTING.REQUEST_ERROR');
        expect(showSuccess).not.toHaveBeenCalled();
        expect(save.disabled).toBe(false);
        expectCheckInFieldsDisabled(dialog, false);
        save.click();
        const payload = api.updateCheckIn.mock.calls[1][0];
        expect(payload).toEqual(api.updateCheckIn.mock.calls[0][0]);
        const saved: FastingSession = {
            ...session,
            checkInAtUtc: utcInstant(FIXTURE_TIME),
            checkInNotes: SAVED_NOTE,
            hungerLevel: payload.hungerLevel,
            energyLevel: payload.energyLevel,
            moodLevel: payload.moodLevel,
        };
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(saved)));
        nextSave.next(saved);
        nextSave.complete();
        redraw(harness);
        expect(document.querySelector('fd-fasting-check-in-dialog')).toBeNull();
        expect(showSuccess).toHaveBeenCalledExactlyOnceWith('FASTING.CHECK_IN.SAVED_TOAST');
        expect(reportEvent).toHaveBeenCalledWith('check-in.saved', expect.objectContaining({ sessionId: saved.id, hadNotes: true }));

        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHECK_IN').click();
        redraw(harness);
        expect(getDialog('fd-fasting-check-in-dialog').querySelector('textarea')?.value).toBe(SAVED_NOTE);
    });
});

describe('Rendered fasting check-in during an ending request', () => {
    it('keeps check-in save disabled while an ending request is pending', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api } = await renderFastingAsync(session);
        const ending = new Subject<FastingSession>();
        api.end.mockReturnValueOnce(ending);
        getButton(pageRoot(harness), 'FASTING.END_FAST').click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST').click();
        redraw(harness);
        expect(ending.observed).toBe(true);

        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHECK_IN').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-check-in-dialog');
        const save = getButton(dialog, 'FASTING.CHECK_IN.SAVE');
        expect(save.disabled).toBe(true);
        expectCheckInFieldsDisabled(dialog, true);
        save.click();
        expect(api.updateCheckIn).not.toHaveBeenCalled();

        ending.error(new Error('End request unavailable'));
        redraw(harness);
        expect(save.disabled).toBe(false);
        expectCheckInFieldsDisabled(dialog, false);
        expect(api.end).toHaveBeenCalledOnce();
    });
});

describe('Fasting check-in ownership after ending', () => {
    it('keeps a finished fast in history when an older check-in response arrives late', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api, reportEvent } = await renderFastingAsync(session);
        const saving = new Subject<FastingSession>();
        const ending = new Subject<FastingSession>();
        const checkIn = { ...session, checkInAtUtc: utcInstant('2026-05-19T11:00:00Z'), checkInNotes: SAVED_NOTE };
        const ended = { ...checkIn, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed', isCompleted: true } satisfies FastingSession;
        api.updateCheckIn.mockReturnValueOnce(saving);
        api.end.mockReturnValueOnce(ending);
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(null, [ended])));

        saveAndDismissFastingCheckIn(harness);
        expect(saving.observed).toBe(true);
        expect(api.updateCheckIn).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ checkInNotes: SAVED_NOTE }));
        confirmFastingEnd(harness);
        expect(ending.observed).toBe(true);
        ending.next(ended);
        ending.complete();
        redraw(harness);
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');

        saving.next(checkIn);
        saving.complete();
        redraw(harness);

        const page = pageRoot(harness);
        expect(page.textContent).toContain('FASTING.REDESIGN.READY');
        expect(page.textContent).not.toContain('FASTING.END_FAST');
        expect(page.querySelector('button[aria-label="FASTING.REDESIGN.OPEN_SESSION_DETAILS"]')?.textContent).toContain(
            'FASTING.BADGE_ENDED_EARLY',
        );
        expect(api.getOverviewStrict).toHaveBeenCalledOnce();
        expect(reportEvent).toHaveBeenCalledWith('session.completed', expect.objectContaining({ sessionId: session.id }));
    });

    it('leaves the next session check-in clear when the previous save fails', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api } = await renderFastingAsync(session);
        const saving = new Subject<FastingSession>();
        const ended = { ...session, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed', isCompleted: true } satisfies FastingSession;
        const replacement = makeSession({ id: REPLACEMENT_SESSION_ID, startedAtUtc: utcInstant(FIXTURE_TIME) });
        api.updateCheckIn.mockReturnValueOnce(saving);
        api.end.mockReturnValueOnce(of(ended));
        api.start.mockReturnValueOnce(of(replacement));
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(null, [ended]))).mockReturnValueOnce(of(makeOverview(replacement)));

        saveAndDismissFastingCheckIn(harness);
        expect(saving.observed).toBe(true);
        confirmFastingEnd(harness);
        getButton(pageRoot(harness), 'FASTING.START_FAST').click();
        redraw(harness);
        const nextCheckIn = openFastingCheckIn(harness);

        saving.error(new Error('Previous check-in unavailable'));
        redraw(harness);

        expect(nextCheckIn.querySelector('fd-ui-form-error [role="alert"]')).toBeNull();
        expect(getButton(nextCheckIn, 'FASTING.CHECK_IN.SAVE').disabled).toBe(false);
        expect(checkInNotesField(nextCheckIn).value).toBe('');
        expect(api.updateCheckIn).toHaveBeenCalledOnce();
    });
});

function saveAndDismissFastingCheckIn(harness: RouterTestingHarness): void {
    const dialog = openFastingCheckIn(harness);
    const notes = checkInNotesField(dialog);
    notes.value = SAVED_NOTE;
    notes.dispatchEvent(new Event('input', { bubbles: true }));
    redraw(harness);
    getButton(dialog, 'FASTING.CHECK_IN.SAVE').click();
    redraw(harness);
    const close = dialog.querySelector<HTMLButtonElement>('button[aria-label="COMMON.CLOSE"]');
    if (close === null) {
        throw new Error('Expected the check-in close button');
    }
    close.click();
    redraw(harness);
    expect(dialog.isConnected).toBe(false);
}

function confirmFastingEnd(harness: RouterTestingHarness): void {
    getButton(pageRoot(harness), 'FASTING.END_FAST').click();
    redraw(harness);
    getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST').click();
    redraw(harness);
}

describe('Mounted fasting check-in translations', () => {
    it('updates the open check-in when the active language receives its application dictionary', async () => {
        registerLocaleData(localeRu);
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api } = await renderFastingAsync(session);
        const translate = TestBed.inject(TranslateService);
        const dialog = openFastingCheckIn(harness);
        const hunger = dialog.querySelector<HTMLButtonElement>('button[role="radio"][aria-label^="FASTING.CHECK_IN.HUNGER_LEVEL_4."]');
        if (hunger === null) {
            throw new Error('Expected the native hunger option');
        }
        hunger.click();
        const headache = getButton(dialog, 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE');
        headache.click();
        const notes = checkInNotesField(dialog);
        notes.value = SAVED_NOTE;
        notes.dispatchEvent(new Event('input', { bubbles: true }));

        translate.setTranslation('ru', { COMMON: { CANCEL: 'Отмена' } });
        vi.spyOn(TestBed.inject(LocalizationService), 'getCurrentLanguage').mockReturnValue('ru');
        await firstValueFrom(translate.use('ru'));
        redraw(harness);
        expect(hunger.getAttribute('aria-checked')).toBe('true');
        expect(headache.getAttribute('aria-pressed')).toBe('true');
        expect(hunger.getAttribute('aria-label')).toContain('FASTING.CHECK_IN.HUNGER_LEVEL_4');

        translate.setTranslation(
            'ru',
            {
                FASTING: {
                    CHECK_IN: {
                        HUNGER_LEVEL_4: 'Сильный голод',
                        HUNGER_LEVEL_4_DESCRIPTION: 'Сложно отвлечься',
                        SYMPTOMS: { HEADACHE: 'Головная боль' },
                        SYMPTOM_HINTS: { HEADACHE: 'Боль или давление в голове' },
                    },
                },
            },
            true,
        );
        redraw(harness);
        expect(document.querySelector('fd-fasting-check-in-dialog')).toBe(dialog);
        expect(hunger.getAttribute('aria-label')).toBe('Сильный голод. Сложно отвлечься');
        expect(headache.textContent.trim()).toBe('Головная боль');
        expect(hunger.getAttribute('aria-checked')).toBe('true');
        expect(headache.getAttribute('aria-pressed')).toBe('true');
        expect(notes.value).toBe(SAVED_NOTE);

        const saved = { ...session, hungerLevel: 4, symptoms: ['headache'], checkInNotes: SAVED_NOTE } satisfies FastingSession;
        api.updateCheckIn.mockReturnValueOnce(of(saved));
        getButton(dialog, 'FASTING.CHECK_IN.SAVE').click();
        expect(api.updateCheckIn).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ hungerLevel: 4, symptoms: ['headache'], checkInNotes: SAVED_NOTE }),
        );
    });
});

describe('Recording another check-in from the fasting route', () => {
    it('records the next check-in without carrying over the note or symptoms', async () => {
        const session = makeSession({
            startedAtUtc: utcInstant('2026-05-19T08:00:00Z'),
            checkInAtUtc: utcInstant('2026-05-19T10:00:00Z'),
            checkInNotes: SAVED_NOTE,
            symptoms: ['headache'],
        });
        const { harness, api, reportEvent } = await renderFastingAsync(session);
        const saved = { ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: null, symptoms: [] };
        api.updateCheckIn.mockReturnValueOnce(of(saved));
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(saved)));

        const dialog = openFastingCheckIn(harness);
        const notes = checkInNotesField(dialog);
        const headache = getButton(dialog, 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE');
        const noSymptoms = getButton(dialog, 'FASTING.CHECK_IN.SYMPTOMS.GOOD');
        expect(notes.value).toBe(SAVED_NOTE);
        expect(headache.getAttribute('aria-pressed')).toBe('true');

        noSymptoms.click();
        redraw(harness);
        expect(noSymptoms.getAttribute('aria-pressed')).toBe('true');
        expect(headache.getAttribute('aria-pressed')).toBe('false');
        noSymptoms.click();
        notes.value = '   ';
        notes.dispatchEvent(new Event('input', { bubbles: true }));
        redraw(harness);
        expect(noSymptoms.getAttribute('aria-pressed')).toBe('false');
        getButton(dialog, 'FASTING.CHECK_IN.SAVE').click();
        redraw(harness);

        expect(api.updateCheckIn).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ symptoms: [], checkInNotes: null }));
        expect(reportEvent).toHaveBeenCalledWith('check-in.saved', expect.objectContaining({ hadNotes: false, symptomsCount: 0 }));
        expect(document.querySelector('fd-fasting-check-in-dialog')).toBeNull();
        const reopened = openFastingCheckIn(harness);
        expect(checkInNotesField(reopened).value).toBe('');
        expect(getButton(reopened, 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE').getAttribute('aria-pressed')).toBe('false');
    });
});

describe('Fasting actions with an older overview request pending', () => {
    it('shows the most recently saved check-in when the first overview responds after the second', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api } = await renderFastingAsync(session);
        const olderOverview = new Subject<FastingOverview>();
        const older = { ...session, checkInAtUtc: utcInstant('2026-05-19T10:00:00Z'), checkInNotes: 'First check-in note' };
        const latest = { ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: 'Second check-in note' };
        api.updateCheckIn.mockReturnValueOnce(of(older)).mockReturnValueOnce(of(latest));
        api.getOverviewStrict.mockReturnValueOnce(olderOverview).mockReturnValueOnce(of(makeOverview(latest)));

        submitFastingCheckInNote(harness, older.checkInNotes);
        expect(olderOverview.observed).toBe(true);
        submitFastingCheckInNote(harness, latest.checkInNotes);
        expect(api.updateCheckIn.mock.calls.map(([payload]) => payload.checkInNotes)).toEqual([older.checkInNotes, latest.checkInNotes]);
        expect(api.getOverviewStrict).toHaveBeenCalledTimes(2);

        olderOverview.next(makeOverview(older));
        olderOverview.complete();
        redraw(harness);

        const reopened = openFastingCheckIn(harness);
        expect(checkInNotesField(reopened).value).toBe(latest.checkInNotes);
    });

    it('keeps a confirmed target extension when a check-in overview responds afterward', async () => {
        const originalHours = 36;
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Fast36',
            plannedDurationHours: originalHours,
            initialPlannedDurationHours: originalHours,
        });
        const { harness, api } = await renderFastingAsync(session);
        const olderOverview = new Subject<FastingOverview>();
        const checkedIn = { ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE };
        const extended = {
            ...checkedIn,
            plannedDurationHours: originalHours + EXTEND_ONE_DAY_HOURS,
            addedDurationHours: EXTEND_ONE_DAY_HOURS,
        };
        api.updateCheckIn.mockReturnValueOnce(of(checkedIn));
        api.getOverviewStrict.mockReturnValueOnce(olderOverview);
        api.extend.mockReturnValueOnce(of(extended));

        submitFastingCheckInNote(harness, SAVED_NOTE);
        expect(olderOverview.observed).toBe(true);
        const page = pageRoot(harness);
        extendFastingByOneDay(harness);
        expect(api.extend).toHaveBeenCalledExactlyOnceWith({ additionalHours: EXTEND_ONE_DAY_HOURS });
        const extendedTarget = `${extended.plannedDurationHours} FASTING.HOURS`;
        expect(page.querySelector('.fasting-redesign__protocol')?.textContent.trim()).toBe(extendedTarget);

        olderOverview.next(makeOverview(checkedIn));
        olderOverview.complete();
        redraw(harness);

        expect(page.querySelector('.fasting-redesign__protocol')?.textContent.trim()).toBe(extendedTarget);
        expect(api.getOverviewStrict).toHaveBeenCalledOnce();
    });
});

describe('Fasting check-in and extension response ordering', () => {
    it('keeps a confirmed target extension when a check-in write responds afterward', async () => {
        const originalHours = 36;
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Fast36',
            plannedDurationHours: originalHours,
            initialPlannedDurationHours: originalHours,
        });
        const { harness, api } = await renderFastingAsync(session);
        const saving = new Subject<FastingSession>();
        const refreshing = new Subject<FastingOverview>();
        const checkedIn = { ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE };
        const extended = {
            ...session,
            plannedDurationHours: originalHours + EXTEND_ONE_DAY_HOURS,
            addedDurationHours: EXTEND_ONE_DAY_HOURS,
        };
        api.updateCheckIn.mockReturnValueOnce(saving);
        api.extend.mockReturnValueOnce(of(extended));
        api.getOverviewStrict.mockReturnValueOnce(refreshing);

        saveAndDismissFastingCheckIn(harness);
        expect(saving.observed).toBe(true);
        extendFastingByOneDay(harness);
        expect(api.extend).toHaveBeenCalledExactlyOnceWith({ additionalHours: EXTEND_ONE_DAY_HOURS });
        const protocol = pageRoot(harness).querySelector('.fasting-redesign__protocol');
        const extendedTarget = `${extended.plannedDurationHours} FASTING.HOURS`;
        expect(protocol?.textContent.trim()).toBe(extendedTarget);

        saving.next(checkedIn);
        saving.complete();
        redraw(harness);

        expect(refreshing.observed).toBe(true);
        expect(protocol?.textContent.trim()).toBe(extendedTarget);
        expect(checkInNotesField(openFastingCheckIn(harness)).value).toBe(SAVED_NOTE);
    });
});

function extendFastingByOneDay(harness: RouterTestingHarness): void {
    getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
    redraw(harness);
    const management = getDialog('fd-fasting-session-management-dialog');
    const extendPanel = management.querySelector<HTMLButtonElement>(
        '.fasting-controls__extend-panel:not(.fasting-controls__extend-panel--subtle) > button',
    );
    if (extendPanel === null) {
        throw new Error('Expected the fasting extension panel');
    }
    extendPanel.click();
    redraw(harness);
    getButton(management, 'FASTING.ADD_DAY').click();
    redraw(harness);
}

function submitFastingCheckInNote(harness: RouterTestingHarness, note: string): void {
    const dialog = openFastingCheckIn(harness);
    const notes = checkInNotesField(dialog);
    notes.value = note;
    notes.dispatchEvent(new Event('input', { bubbles: true }));
    redraw(harness);
    getButton(dialog, 'FASTING.CHECK_IN.SAVE').click();
    redraw(harness);
    expect(document.querySelector('fd-fasting-check-in-dialog')).toBeNull();
}

describe('Fasting check-in when leaving the route', () => {
    it('closes the dialog and cancels its pending save when navigating away', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api, showSuccess, reportEvent } = await renderFastingAsync(session);
        const saving = new Subject<FastingSession>();
        api.updateCheckIn.mockReturnValueOnce(saving);
        const dialog = openFastingCheckIn(harness);
        const notes = checkInNotesField(dialog);
        notes.value = SAVED_NOTE;
        notes.dispatchEvent(new Event('input', { bubbles: true }));
        getButton(dialog, 'FASTING.CHECK_IN.SAVE').click();
        redraw(harness);
        expect(saving.observed).toBe(true);
        expectCheckInFieldsDisabled(dialog, true);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(saving.observed).toBe(false);
        expect(document.querySelector('fd-fasting-check-in-dialog')).toBeNull();

        saving.next({ ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE });
        saving.complete();
        expect(showSuccess).not.toHaveBeenCalled();
        expect(reportEvent).not.toHaveBeenCalled();
        expect(api.getOverviewStrict).not.toHaveBeenCalled();

        await harness.navigateByUrl('/fasting');
        redraw(harness);
        expect(checkInNotesField(openFastingCheckIn(harness)).value).toBe('');
        expect(api.updateCheckIn).toHaveBeenCalledOnce();
    });
});

function openFastingCheckIn(harness: RouterTestingHarness): HTMLElement {
    getButton(pageRoot(harness), 'FASTING.REDESIGN.CHECK_IN').click();
    redraw(harness);
    return getDialog('fd-fasting-check-in-dialog');
}

function checkInNotesField(dialog: HTMLElement): HTMLTextAreaElement {
    const field = dialog.querySelector('textarea');
    if (field === null) {
        throw new Error('Expected the check-in notes field');
    }
    return field;
}

function expectCheckInFieldsDisabled(dialog: HTMLElement, disabled: boolean): void {
    for (const selector of ['textarea', 'fd-ui-emoji-picker button[role="radio"]', 'fd-ui-chip-select button']) {
        const controls = Array.from(dialog.querySelectorAll(selector));
        expect(controls, selector).not.toHaveLength(0);
        expect(
            controls.every(control => control.matches(':disabled') === disabled),
            selector,
        ).toBe(true);
    }
}

describe('Rendered fasting actions during a session update', () => {
    it('prevents a second end request and offers a retry after the pending request fails', async () => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api, showError } = await renderFastingAsync(session);
        const ending = new Subject<FastingSession>();
        api.end.mockReturnValue(NEVER).mockReturnValueOnce(ending);
        const end = getButton(pageRoot(harness), 'FASTING.END_FAST');
        end.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST').click();
        redraw(harness);

        expect(ending.observed).toBe(true);
        end.click();
        redraw(harness);
        const duplicateConfirm = document.querySelector<HTMLElement>('fd-fasting-end-confirm-dialog');
        if (duplicateConfirm !== null) {
            getButton(duplicateConfirm, 'FASTING.END_FAST').click();
            redraw(harness);
        }
        expect(api.end).toHaveBeenCalledTimes(1);
        expect(end.disabled).toBe(true);

        ending.error(new Error('The end request failed'));
        redraw(harness);

        expect(showError).toHaveBeenCalledExactlyOnceWith('FASTING.REQUEST_ERROR');
        expect(end.disabled).toBe(false);
        expect(pageRoot(harness).textContent).toContain('FASTING.FASTING_WINDOW');
        expect(pageRoot(harness).textContent).not.toContain('FASTING.REDESIGN.READY');

        const ended = { ...session, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed' } satisfies FastingSession;
        api.end.mockReturnValueOnce(of(ended));
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(null, [ended])));
        end.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST').click();
        redraw(harness);

        expect(api.end).toHaveBeenCalledTimes(2);
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
    });
});

describe('Visible fasting End controls during an overview refresh', () => {
    it.each([
        { surface: 'management', transition: 'replacement' },
        { surface: 'page', transition: 'replacement' },
        { surface: 'page', transition: 'completion' },
    ])('keeps the $surface End action attached to its rendered session after overview $transition', async ({ surface, transition }) => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const session = makeSession({
            startedAtUtc: utcInstant('2026-05-19T08:00:00Z'),
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: EXTENDED_HOURS,
            initialPlannedDurationHours: EXTENDED_HOURS,
        });
        const { harness, api } = await renderFastingAsync(session);
        const transport = TestBed.runInInjectionContext(() => new FastingService());
        const http = TestBed.inject(HttpTestingController);
        const request = { method: 'PUT', url: `${environment.apiUrls.fasting}/end` };
        const overview = new Subject<FastingOverview>();
        api.end.mockImplementation(() => transport.end());
        api.updateCheckIn.mockReturnValueOnce(of({ ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE }));
        api.getOverviewStrict.mockReturnValueOnce(overview);

        submitFastingCheckInNote(harness, SAVED_NOTE);
        expect(overview.observed).toBe(true);
        if (surface === 'management') {
            getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
            redraw(harness);
        }
        const management = surface === 'management' ? getDialog('fd-fasting-session-management-dialog') : null;
        const oldInterrupt = getButton(management ?? pageRoot(harness), 'FASTING.INTERRUPT_FAST');
        const ended = { ...session, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed', isCompleted: true } satisfies FastingSession;
        const replacement = makeSession({
            id: REPLACEMENT_SESSION_ID,
            startedAtUtc: utcInstant(FIXTURE_TIME),
            planType: 'Cyclic',
            protocol: 'Cyclic',
            occurrenceKind: 'FastDay',
            cyclicFastDays: CYCLIC_FAST_DAYS,
            cyclicEatDays: CYCLIC_EAT_DAYS,
            cyclicEatDayFastHours: CYCLIC_EAT_DAY_FAST_HOURS,
            cyclicEatDayEatingWindowHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        });

        overview.next(transition === 'replacement' ? makeOverview(replacement, [replacement, ended]) : makeOverview(null, [ended]));
        overview.complete();
        expect(oldInterrupt.isConnected).toBe(true);
        expect(oldInterrupt.disabled).toBe(false);
        oldInterrupt.click();
        redraw(harness);
        const staleConfirmation = document.querySelector('fd-fasting-end-confirm-dialog');
        if (staleConfirmation !== null) {
            getButton(staleConfirmation, 'FASTING.STOP_CYCLE').click();
            redraw(harness);
        }
        http.expectNone(request, 'the rendered Interrupt button must not stop a replacement cycle');
        expect(staleConfirmation).toBeNull();
        if (transition === 'completion') {
            expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
        } else {
            getButton(management ?? pageRoot(harness), 'FASTING.STOP_CYCLE').click();
            redraw(harness);
            const confirmation = getDialog('fd-fasting-end-confirm-dialog');
            expect(confirmation.textContent).toContain('FASTING.STOP_CYCLE_CONFIRM_MESSAGE');
            getButton(confirmation, 'FASTING.STOP_CYCLE').click();
            redraw(harness);
            const endedReplacement = {
                ...replacement,
                endedAtUtc: utcInstant(FIXTURE_TIME),
                status: 'Completed',
                isCompleted: true,
            } satisfies FastingSession;
            api.getOverviewStrict.mockReturnValue(of(makeOverview(null, [endedReplacement, ended])));
            http.expectOne(request).flush(endedReplacement);
            redraw(harness);
            if (management !== null) {
                expect(management.isConnected).toBe(false);
            }
        }
        http.verify();
    });
});

describe('Fasting end confirmation during an overview refresh', () => {
    it.each(['replaced', 'ended'] as const)('does not end a fast from a confirmation after its session was %s', async change => {
        const session = makeSession({ startedAtUtc: utcInstant('2026-05-19T08:00:00Z') });
        const { harness, api } = await renderFastingAsync(session);
        const overview = new Subject<FastingOverview>();
        const saved = { ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE };
        api.updateCheckIn.mockReturnValueOnce(of(saved));
        api.getOverviewStrict.mockReturnValueOnce(overview);
        api.end.mockReturnValue(NEVER);

        submitFastingCheckInNote(harness, SAVED_NOTE);
        expect(overview.observed).toBe(true);
        getButton(pageRoot(harness), 'FASTING.END_FAST').click();
        redraw(harness);
        const originalConfirmation = getDialog('fd-fasting-end-confirm-dialog');
        const confirm = getButton(originalConfirmation, 'FASTING.END_FAST');

        const ended = { ...saved, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed', isCompleted: true } satisfies FastingSession;
        const replacement = makeSession({ id: REPLACEMENT_SESSION_ID, startedAtUtc: utcInstant(FIXTURE_TIME) });
        overview.next(change === 'replaced' ? makeOverview(replacement, [replacement, ended]) : makeOverview(null, [ended]));
        overview.complete();
        redraw(harness);

        confirm.click();
        redraw(harness);
        expect(originalConfirmation.isConnected).toBe(false);
        expect(api.end).not.toHaveBeenCalled();

        if (change === 'replaced') {
            getButton(pageRoot(harness), 'FASTING.END_FAST').click();
            redraw(harness);
            getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST').click();
            redraw(harness);
            expect(api.end).toHaveBeenCalledOnce();
        } else {
            expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
        }
    });
});

describe('Rendered fasting actions during an extended target change', () => {
    it.each([
        {
            action: 'FASTING.REDUCE_4_HOURS',
            section: '.fasting-controls__extend-panel--subtle > button',
            request: 'reduceTarget',
            payload: { reducedHours: REDUCED_HOURS },
            updatedHours: 32,
            difference: -REDUCED_HOURS,
        },
        {
            action: 'FASTING.ADD_DAY',
            section: '.fasting-controls__extend-panel:not(.fasting-controls__extend-panel--subtle) > button',
            request: 'extend',
            payload: { additionalHours: EXTEND_ONE_DAY_HOURS },
            updatedHours: 60,
            difference: EXTEND_ONE_DAY_HOURS,
        },
    ] as const)('blocks page actions during $action after the management dialog closes', async scenario => {
        const plannedDurationHours = 36;
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Fast36',
            initialPlannedDurationHours: plannedDurationHours,
            plannedDurationHours,
        });
        const { harness, api } = await renderFastingAsync(session);
        const pending = new Subject<FastingSession>();
        api[scenario.request].mockReturnValueOnce(pending);
        const page = pageRoot(harness);
        const manage = getButton(page, 'FASTING.REDESIGN.MANAGE');
        const interrupt = getButton(page, 'FASTING.INTERRUPT_FAST');
        manage.click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-session-management-dialog');
        const durationPanel = dialog.querySelector<HTMLButtonElement>(scenario.section);
        expect(durationPanel).not.toBeNull();
        durationPanel?.click();
        redraw(harness);
        getButton(dialog, scenario.action).click();
        redraw(harness);
        expect(pending.observed).toBe(true);
        expect(api[scenario.request]).toHaveBeenCalledExactlyOnceWith(scenario.payload);
        dialog.querySelector<HTMLButtonElement>('button[aria-label="COMMON.CLOSE"]')?.click();
        redraw(harness);
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();

        expect(manage.disabled).toBe(true);
        expect(interrupt.disabled).toBe(true);
        manage.click();
        interrupt.click();
        redraw(harness);
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
        expect(document.querySelector('fd-fasting-end-confirm-dialog')).toBeNull();
        expect(api.end).not.toHaveBeenCalled();

        pending.next({ ...session, plannedDurationHours: scenario.updatedHours, addedDurationHours: scenario.difference });
        pending.complete();
        redraw(harness);

        expect(manage.disabled).toBe(false);
        expect(interrupt.disabled).toBe(false);
        expect(page.querySelector('.fasting-redesign__protocol')?.textContent.trim()).toBe(`${scenario.updatedHours} FASTING.HOURS`);
    });
});

describe('Rendered cyclic day timeline', () => {
    it.each([
        { occurrenceKind: 'FastDay', day: 1, total: 2, phase: 'FASTING.FASTING_WINDOW', next: 'FASTING.FAST_DAY' },
        { occurrenceKind: 'FastDay', day: 2, total: 2, phase: 'FASTING.FASTING_WINDOW', next: 'FASTING.EAT_DAY' },
        { occurrenceKind: 'EatDay', day: 1, total: 2, phase: 'FASTING.EATING_WINDOW', next: 'FASTING.EAT_DAY' },
        { occurrenceKind: 'EatDay', day: 2, total: 2, phase: 'FASTING.EATING_WINDOW', next: 'FASTING.FAST_DAY' },
    ] as const)('shows the phase after $occurrenceKind day $day of $total', async ({ occurrenceKind, day, total, phase, next }) => {
        const durationHours = 24;
        const session = makeSession({
            startedAtUtc: utcInstant('2026-05-19T06:00:00Z'),
            protocol: 'Cyclic',
            planType: 'Cyclic',
            occurrenceKind,
            initialPlannedDurationHours: durationHours,
            plannedDurationHours: durationHours,
            cyclicFastDays: total,
            cyclicEatDays: total,
            cyclicPhaseDayNumber: day,
            cyclicPhaseDayTotal: total,
        });
        const { harness } = await renderFastingAsync(session);
        const page = pageRoot(harness);
        const timeline = page.querySelector('.fasting-redesign__timeline[role="progressbar"]');

        expect(timeline?.getAttribute('aria-label')).toBe(phase);
        expect(timeline?.getAttribute('aria-valuenow')).toBe('25');
        expect(page.querySelector('.fasting-redesign__next-phase strong')?.textContent.trim()).toBe(next);
        expect(page.querySelector<HTMLElement>('.fasting-redesign__target-notice')?.hidden).toBe(true);
        if (occurrenceKind === 'EatDay') {
            expect(page.querySelector('.fasting-redesign__stage')).toBeNull();
        }
    });
});

describe('Rendered fasting setup locale', () => {
    it('keeps a custom target and updates its summary when the open setup changes language', async () => {
        const { harness, api } = await renderFastingAsync(null);
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { FASTING: { REDESIGN: { EXTENDED_SELECTION_SUMMARY: 'Fast for {{hours}} hours' } } });
        translate.setTranslation('ru', { FASTING: { REDESIGN: { EXTENDED_SELECTION_SUMMARY: 'Голодание на {{hours}} часов' } } });
        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-protocol-dialog');
        getButton(dialog, 'FASTING.MODE_EXTENDED').click();
        redraw(harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.CUSTOM_HOURS', CUSTOM_TARGET_HOURS, harness);

        expect(dialog.querySelector('.fasting-controls__selection-summary')?.textContent).toContain(
            `Fast for ${CUSTOM_TARGET_HOURS} hours`,
        );
        vi.spyOn(TestBed.inject(LocalizationService), 'getCurrentLanguage').mockReturnValue('ru');
        await firstValueFrom(translate.use('ru'));
        redraw(harness);

        expect(dialog.querySelector('.fasting-controls__selection-summary')?.textContent).toContain(
            `Голодание на ${CUSTOM_TARGET_HOURS} часов`,
        );
        expect(dialog.querySelector<HTMLInputElement>('input[type="number"]')?.value).toBe(String(CUSTOM_TARGET_HOURS));
        expect(api.start).not.toHaveBeenCalled();
    });
});

const EXTEND_VISIBLE_TARGET = {
    panel: '.fasting-controls__extend-panel:not(.fasting-controls__extend-panel--subtle) > button',
    label: 'FASTING.ADD_DAY',
    endpoint: 'current/duration',
    body: { additionalHours: EXTEND_ONE_DAY_HOURS },
    durationChange: EXTEND_ONE_DAY_HOURS,
};
const REDUCE_VISIBLE_TARGET = {
    panel: '.fasting-controls__extend-panel--subtle > button',
    label: 'FASTING.REDUCE_4_HOURS',
    endpoint: 'current/duration/reduce',
    body: { reducedHours: REDUCED_HOURS },
    durationChange: -REDUCED_HOURS,
};

describe('Visible fasting target controls during an overview refresh', () => {
    it.each([
        { action: 'extend', target: EXTEND_VISIBLE_TARGET, transition: 'replacement' },
        { action: 'extend', target: EXTEND_VISIBLE_TARGET, transition: 'completion' },
        { action: 'reduce', target: REDUCE_VISIBLE_TARGET, transition: 'replacement' },
        { action: 'reduce', target: REDUCE_VISIBLE_TARGET, transition: 'completion' },
    ])('waits for current controls before sending $action after an overview $transition', async ({ target, transition }) => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const session = makeSession({
            startedAtUtc: utcInstant('2026-05-19T08:00:00Z'),
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: EXTENDED_HOURS,
            initialPlannedDurationHours: EXTENDED_HOURS,
        });
        const { harness, api } = await renderFastingAsync(session);
        const transport = TestBed.runInInjectionContext(() => new FastingService());
        const http = TestBed.inject(HttpTestingController);
        api.extend.mockImplementation(payload => transport.extend(payload));
        api.reduceTarget.mockImplementation(payload => transport.reduceTarget(payload));
        const request = { method: 'PUT', url: `${environment.apiUrls.fasting}/${target.endpoint}` };
        const overview = new Subject<FastingOverview>();
        api.updateCheckIn.mockReturnValueOnce(of({ ...session, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE }));
        api.getOverviewStrict.mockReturnValueOnce(overview);
        submitFastingCheckInNote(harness, SAVED_NOTE);
        expect(overview.observed).toBe(true);

        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-session-management-dialog');
        const header = dialog.querySelector<HTMLButtonElement>(target.panel);
        if (header === null) {
            throw new Error('Expected the visible fasting target action panel');
        }
        header.click();
        redraw(harness);
        const oldControl = getButton(dialog, target.label);
        expect(oldControl.disabled).toBe(false);

        const ended = { ...session, endedAtUtc: utcInstant(FIXTURE_TIME), status: 'Completed', isCompleted: true } satisfies FastingSession;
        const replacement = { ...session, id: REPLACEMENT_SESSION_ID, startedAtUtc: utcInstant(FIXTURE_TIME) };
        overview.next(transition === 'replacement' ? makeOverview(replacement, [replacement, ended]) : makeOverview(null, [ended]));
        overview.complete();
        expect(oldControl.isConnected).toBe(true);
        expect(oldControl.disabled).toBe(false);
        oldControl.click();
        http.expectNone(request, 'an old rendered fasting control must not change the current session');

        redraw(harness);
        if (transition === 'replacement') {
            getButton(dialog, target.label).click();
            const updated = http.expectOne(request);
            expect(updated.request.body).toEqual(target.body);
            updated.flush({
                ...replacement,
                addedDurationHours: target.durationChange,
                plannedDurationHours: replacement.plannedDurationHours + target.durationChange,
            });
            redraw(harness);
        } else {
            expect(dialog.isConnected).toBe(false);
            expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
        }
        http.verify();
    });
});

describe('Rendered fasting target changes', () => {
    it('closes management and displays history when reducing the target completes the session', async () => {
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: EXTENDED_HOURS,
            initialPlannedDurationHours: EXTENDED_HOURS,
        });
        const { harness, api } = await renderFastingAsync(session);
        const response = new Subject<FastingSession>();
        api.reduceTarget.mockReturnValueOnce(response);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-session-management-dialog');
        const panelHeader = Array.from(dialog.querySelectorAll('button')).find(button =>
            button.textContent.includes('FASTING.REDUCE_GROUP'),
        );
        if (panelHeader === undefined) {
            throw new Error('Expected the target reduction controls');
        }
        panelHeader.click();
        redraw(harness);
        const reduce = getButton(dialog, 'FASTING.REDUCE_4_HOURS');
        expect(reduce.disabled).toBe(false);
        reduce.click();
        redraw(harness);
        expect(api.reduceTarget).toHaveBeenCalledExactlyOnceWith({ reducedHours: REDUCED_HOURS });
        expect(reduce.disabled).toBe(true);

        response.next({
            ...session,
            plannedDurationHours: EXTENDED_HOURS - REDUCED_HOURS,
            addedDurationHours: -REDUCED_HOURS,
            endedAtUtc: utcInstant(FIXTURE_TIME),
            isCompleted: true,
            status: 'Completed',
        });
        response.complete();
        redraw(harness);
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.READY');
        const historyRow = pageRoot(harness).querySelector('button[aria-label="FASTING.REDESIGN.OPEN_SESSION_DETAILS"]');
        expect(historyRow?.textContent).toContain('FASTING.BADGE_COMPLETED');
    });

    it('sends the custom extended target selected through the actual setup controls', async () => {
        const { harness, api } = await renderFastingAsync(null);
        const starting = new Subject<FastingSession>();
        api.start.mockReturnValueOnce(starting);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-protocol-dialog');
        getButton(dialog, 'FASTING.MODE_EXTENDED').click();
        redraw(harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.CUSTOM_HOURS', CUSTOM_TARGET_HOURS, harness);
        getButton(dialog, 'FASTING.START_FAST').click();
        redraw(harness);

        expect(api.start).toHaveBeenCalledExactlyOnceWith({ planType: 'Extended', protocol: 'Custom', durationHours: CUSTOM_TARGET_HOURS });
        const active = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            initialPlannedDurationHours: CUSTOM_TARGET_HOURS,
            plannedDurationHours: CUSTOM_TARGET_HOURS,
            startedAtUtc: utcInstant(FIXTURE_TIME),
        });
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(active)));
        starting.next(active);
        starting.complete();
        redraw(harness);
        expect(document.querySelector('fd-fasting-protocol-dialog')).toBeNull();
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.MANAGE');
    });
});

describe('Custom extended fasting adjustments from the session dialog', () => {
    it.each([
        {
            action: 'extension',
            group: 'FASTING.EXTEND_GROUP',
            field: 'FASTING.ADD_HOURS',
            apply: 'FASTING.ADD_TIME',
            request: 'extend',
            payload: 'additionalHours',
        },
        {
            action: 'reduction',
            group: 'FASTING.REDUCE_GROUP',
            field: 'FASTING.REDUCE_HOURS',
            apply: 'FASTING.REDUCE_TIME',
            request: 'reduceTarget',
            payload: 'reducedHours',
        },
    ] as const)('blocks a blank custom $action and submits the newly entered value', async scenario => {
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: EXTENDED_HOURS,
            initialPlannedDurationHours: EXTENDED_HOURS,
        });
        const { harness, api } = await renderFastingAsync(session);
        const request = api[scenario.request];
        request.mockReturnValueOnce(new Subject<FastingSession>());
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-session-management-dialog');
        const group = Array.from(dialog.querySelectorAll('button')).find(button => button.textContent.includes(scenario.group));
        if (group === undefined) {
            throw new Error(`Expected the ${scenario.action} controls`);
        }
        group.click();
        redraw(harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM').click();
        redraw(harness);
        const input = dialog.querySelector<HTMLInputElement>('input[type="number"]');
        if (input === null) {
            throw new Error('Expected the custom duration field');
        }

        const previousHours = 3;
        enterNumber(dialog, scenario.field, previousHours, harness);
        const apply = getButton(dialog, scenario.apply);
        expect(apply.disabled).toBe(false);
        input.value = '';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        redraw(harness);
        expect(input.value).toBe('');
        expect(apply.disabled).toBe(true);
        expect(apply.title).toBe('FORM_ERRORS.REQUIRED');
        apply.click();
        expect(request).not.toHaveBeenCalled();

        const correctedHours = 2;
        enterNumber(dialog, scenario.field, correctedHours, harness);
        expect(apply.disabled).toBe(false);
        apply.click();
        redraw(harness);
        expect(request).toHaveBeenCalledExactlyOnceWith({ [scenario.payload]: correctedHours });
        expect(apply.disabled).toBe(true);
    });
});

describe('Routed fasting cycle setup', () => {
    it('sends the custom cycle days and the separate eating-day window', async () => {
        const { harness, api } = await renderFastingAsync(null);
        const starting = new Subject<FastingSession>();
        api.start.mockReturnValueOnce(starting);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-protocol-dialog');
        getButton(dialog, 'FASTING.MODE_CYCLIC').click();
        redraw(harness);
        getButton(dialog, 'FASTING.CUSTOM_CYCLE').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.CYCLIC_FAST_DAYS_SHORT', CYCLIC_FAST_DAYS, harness);
        enterNumber(dialog, 'FASTING.CYCLIC_EAT_DAYS_SHORT', CYCLIC_EAT_DAYS, harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM_INTERMITTENT').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.FAST_HOURS_SHORT', CYCLIC_EAT_DAY_FAST_HOURS, harness);
        getButton(dialog, 'FASTING.START_FAST').click();
        redraw(harness);

        expect(api.start).toHaveBeenCalledOnce();
        const [intent] = api.start.mock.calls[0];
        if (intent.planType !== 'Cyclic') {
            throw new Error('Expected to start a cyclic fasting plan');
        }
        expect(intent.fastDays).toBe(CYCLIC_FAST_DAYS);
        expect(intent.eatDays).toBe(CYCLIC_EAT_DAYS);
        expect(intent.eatDayWindow.fastHours).toBe(CYCLIC_EAT_DAY_FAST_HOURS);
        expect(intent.eatDayWindow.eatingWindowHours).toBe(CYCLIC_EAT_DAY_WINDOW_HOURS);
    });

    it('selects the mode defaults and starts the chosen fixed cycle with a different eating-day protocol', async () => {
        const { harness, api } = await renderFastingAsync(null);
        api.start.mockReturnValueOnce(new Subject<FastingSession>());
        const dialog = openIdleProtocolSettings(harness);
        expect(getButton(dialog, 'FASTING.PROTOCOL_16_8').getAttribute('aria-checked')).toBe('true');

        getButton(dialog, 'FASTING.MODE_EXTENDED').click();
        redraw(harness);
        expect(getButton(dialog, 'FASTING.PROTOCOL_24_0').getAttribute('aria-checked')).toBe('true');

        getButton(dialog, 'FASTING.MODE_INTERMITTENT').click();
        redraw(harness);
        expect(getButton(dialog, 'FASTING.PROTOCOL_16_8').getAttribute('aria-checked')).toBe('true');

        getButton(dialog, 'FASTING.MODE_CYCLIC').click();
        redraw(harness);
        const preset = getButton(dialog, '2:1');
        const eatingDay = getButton(dialog, 'FASTING.PROTOCOL_18_6');
        preset.click();
        redraw(harness);
        eatingDay.click();
        redraw(harness);
        expect(preset.getAttribute('aria-checked')).toBe('true');
        expect(eatingDay.getAttribute('aria-checked')).toBe('true');
        expect(api.start).not.toHaveBeenCalled();

        getButton(dialog, 'FASTING.START_FAST').click();
        expect(api.start).toHaveBeenCalledOnce();
        const [intent] = api.start.mock.calls[0];
        if (intent.planType !== 'Cyclic') {
            throw new Error('Expected to start a cyclic fasting plan');
        }
        expect({
            fastDays: intent.fastDays,
            eatDays: intent.eatDays,
            fastHours: intent.eatDayWindow.fastHours,
            eatingWindowHours: intent.eatDayWindow.eatingWindowHours,
        }).toEqual({
            fastDays: 2,
            eatDays: 1,
            fastHours: 18,
            eatingWindowHours: 6,
        });
    });
});

describe('Routed idle fasting selection', () => {
    it('shows the chosen cyclic preset before the session is started', async () => {
        const { harness, api } = await renderFastingAsync(null);
        const dialog = openIdleProtocolSettings(harness);
        getButton(dialog, 'FASTING.MODE_CYCLIC').click();
        redraw(harness);
        const preset = getButton(dialog, '2:1');
        preset.click();
        redraw(harness);
        expect(preset.getAttribute('aria-checked')).toBe('true');

        expect(closeIdleProtocolSettings(harness, dialog)).toBe('2:1');
        expect(api.start).not.toHaveBeenCalled();
    });

    it('shows a short custom extended fast in hours before the session is started', async () => {
        const duration = 8;
        const { harness, api } = await renderFastingAsync(null);
        const dialog = openIdleProtocolSettings(harness);
        getButton(dialog, 'FASTING.MODE_EXTENDED').click();
        redraw(harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.CUSTOM_HOURS', duration, harness);

        expect(closeIdleProtocolSettings(harness, dialog)).toBe(`${duration} FASTING.HOURS`);
        expect(api.start).not.toHaveBeenCalled();
    });

    it('shows one hour after clearing the custom extended target and submits that value', async () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        const { harness, api } = await renderFastingAsync(null);
        const transport = TestBed.runInInjectionContext(() => new FastingService());
        const http = TestBed.inject(HttpTestingController);
        api.start.mockImplementationOnce(intent => transport.start(intent));

        const dialog = openIdleProtocolSettings(harness);
        getButton(dialog, 'FASTING.MODE_EXTENDED').click();
        redraw(harness);
        getButton(dialog, 'FASTING.PROTOCOL_CUSTOM').click();
        redraw(harness);
        enterNumber(dialog, 'FASTING.CUSTOM_HOURS', CUSTOM_TARGET_HOURS, harness);
        const durationInput = dialog.querySelector('input[type="number"]');
        if (!(durationInput instanceof HTMLInputElement)) {
            throw new Error('Expected the custom extended target field');
        }
        durationInput.value = '';
        durationInput.dispatchEvent(new Event('input', { bubbles: true }));
        redraw(harness);

        expect(durationInput.value).toBe('1');
        expect(closeIdleProtocolSettings(harness, dialog)).toBe('1 FASTING.HOURS');
        expect(api.start).not.toHaveBeenCalled();

        getButton(openIdleProtocolSettings(harness), 'FASTING.START_FAST').click();
        redraw(harness);
        const start = http.expectOne({ method: 'POST', url: `${environment.apiUrls.fasting}/start` });
        expect(start.request.body).toEqual({ planType: 'Extended', protocol: 'Custom', plannedDurationHours: 1 });
        const active = makeSession({ planType: 'Extended', protocol: 'Custom', initialPlannedDurationHours: 1, plannedDurationHours: 1 });
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(active)));
        start.flush(active);
        redraw(harness);

        expect(document.querySelector('fd-fasting-protocol-dialog')).toBeNull();
        expect(pageRoot(harness).textContent).toContain('FASTING.REDESIGN.MANAGE');
        http.verify();
    });
});

function openIdleProtocolSettings(harness: RouterTestingHarness): HTMLElement {
    getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
    redraw(harness);
    return getDialog('fd-fasting-protocol-dialog');
}

function closeIdleProtocolSettings(harness: RouterTestingHarness, dialog: HTMLElement): string {
    const close = dialog.querySelector<HTMLButtonElement>('button[aria-label="COMMON.CLOSE"]');
    if (close === null) {
        throw new Error('Expected the close button');
    }
    close.click();
    redraw(harness);
    expect(document.querySelector('fd-fasting-protocol-dialog')).toBeNull();
    const preview = pageRoot(harness).querySelector('.fasting-redesign__time');
    if (preview === null) {
        throw new Error('Expected the idle fasting plan summary');
    }
    return preview.textContent.trim();
}

describe('Routed fasting history', () => {
    it('keeps the first page on a failed request and opens the legacy cyclic check-in after retry', async () => {
        const first = makeSession({
            id: entityId<'fasting-session'>('recent-fast'),
            planType: 'Extended',
            protocol: 'Fast24',
            initialPlannedDurationHours: EXTEND_ONE_DAY_HOURS,
            plannedDurationHours: EXTEND_ONE_DAY_HOURS,
            endedAtUtc: utcInstant('2026-05-19T11:58:00Z'),
            status: 'Interrupted',
        });
        const legacy = makeSession({
            id: entityId<'fasting-session'>('historic-cycle'),
            startedAtUtc: utcInstant('2026-05-18T08:30:00Z'),
            endedAtUtc: utcInstant('2026-05-19T11:45:00Z'),
            planType: 'Cyclic',
            protocol: 'Cyclic',
            occurrenceKind: 'FastDay',
            initialPlannedDurationHours: EXTEND_ONE_DAY_HOURS,
            plannedDurationHours: EXTEND_ONE_DAY_HOURS,
            status: 'Completed',
            isCompleted: true,
            checkInAtUtc: utcInstant('2026-05-19T11:35:00Z'),
            checkInNotes: 'Older entry without separate ratings',
        });
        const overview = makeOverview(null, [first]);
        overview.history = { ...overview.history, totalPages: FINAL_HISTORY_PAGE, totalItems: 2 };
        const { harness, api } = await renderFastingAsync(null, overview);
        const pending = new Subject<FastingOverview['history']>();
        api.getHistory
            .mockReturnValueOnce(pending)
            .mockReturnValueOnce(of({ ...overview.history, data: [legacy], page: FINAL_HISTORY_PAGE }));
        const viewHistory = pageRoot(harness).querySelector<HTMLButtonElement>('.fasting-redesign__history-action button');
        expect(viewHistory?.textContent).toContain('FASTING.REDESIGN.VIEW_HISTORY');
        viewHistory?.click();
        redraw(harness);
        const history = getDialog('fd-fasting-history-dialog');
        expect(history.querySelectorAll('.fasting-history-dialog__row')).toHaveLength(1);

        getButton(history, 'FASTING.LOAD_MORE_HISTORY').click();
        redraw(harness);
        expect(getButton(history, 'FASTING.LOAD_MORE_HISTORY').disabled).toBe(true);
        expect(api.getHistory).toHaveBeenCalledExactlyOnceWith({
            from: '2026-04-01T00:00:00.000Z',
            to: '2026-06-30T23:59:59.999Z',
            page: FINAL_HISTORY_PAGE,
            limit: overview.history.limit,
        });
        pending.error(new Error('History unavailable'));
        await waitForAsyncTasksAsync();
        redraw(harness);
        expect(history.querySelector('[role="alert"]')?.textContent).toContain('FASTING.REQUEST_ERROR');
        expect(history.querySelectorAll('.fasting-history-dialog__row')).toHaveLength(1);

        getButton(history, 'FASTING.LOAD_MORE_HISTORY').click();
        redraw(harness);

        expect(history.querySelector('[role="alert"]')).toBeNull();
        expect(history.textContent).not.toContain('FASTING.LOAD_MORE_HISTORY');
        const rows = history.querySelectorAll<HTMLButtonElement>('.fasting-history-dialog__row');
        expect(rows).toHaveLength(2);
        const cyclic = Array.from(rows).find(row => row.textContent.includes('FASTING.CYCLIC_TYPE'));
        if (cyclic === undefined) {
            throw new Error('Expected the saved cyclic fast to appear after history retry');
        }
        expect(cyclic.textContent).toContain('1:1 (16:8)');
        cyclic.click();
        redraw(harness);

        const details = getDialog('fd-fasting-session-details-dialog');
        expect(details.querySelector('fd-fasting-check-in-chart')).toBeNull();
        const checkIns = details.querySelectorAll('fd-fasting-history-check-in-entry');
        expect(checkIns).toHaveLength(1);
        expect(checkIns[0].textContent).toContain('Older entry without separate ratings');
    });
});

describe('Routed cyclic management confirmations', () => {
    it('leaves the cyclic eating day unchanged on cancel or failure and advances it on confirmed retry', async () => {
        const eatingDay = makeSession({
            planType: 'Cyclic',
            protocol: 'Cyclic',
            occurrenceKind: 'EatDay',
            cyclicFastDays: CYCLIC_FAST_DAYS,
            cyclicEatDays: CYCLIC_EAT_DAYS,
            cyclicEatDayFastHours: CYCLIC_EAT_DAY_FAST_HOURS,
            cyclicEatDayEatingWindowHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        });
        const { harness, api, showError } = await renderFastingAsync(eatingDay);
        const switching = new Subject<FastingSession>();
        api.skipCyclicDay.mockReturnValueOnce(switching);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const management = getDialog('fd-fasting-session-management-dialog');
        const startNow = getButton(management, 'FASTING.START_FAST_NOW');
        startNow.click();
        redraw(harness);
        const cancelled = getDialog('fd-fasting-end-confirm-dialog');
        expect(cancelled.textContent).toContain('FASTING.START_FAST_NOW_CONFIRM_MESSAGE');
        getButton(cancelled, 'FASTING.CANCEL_ACTION').click();
        redraw(harness);
        expect(api.skipCyclicDay).not.toHaveBeenCalled();

        startNow.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.START_FAST_NOW').click();
        redraw(harness);
        expect(api.skipCyclicDay).toHaveBeenCalledOnce();
        expect(startNow.disabled).toBe(true);
        expect(getButton(pageRoot(harness), 'FASTING.STOP_CYCLE').disabled).toBe(true);
        const skipDay = getButton(management, 'FASTING.SKIP_DAY');
        expect(skipDay.disabled).toBe(true);
        skipDay.click();
        expect(api.postponeCyclicDay).not.toHaveBeenCalled();

        switching.error(new Error('Cycle update unavailable'));
        redraw(harness);
        expect(showError).toHaveBeenCalledExactlyOnceWith('FASTING.REQUEST_ERROR');
        expect(startNow.disabled).toBe(false);
        const fastingDay = {
            ...eatingDay,
            id: entityId<'fasting-session'>('session-next-day'),
            occurrenceKind: 'FastDay' as const,
        };
        api.skipCyclicDay.mockReturnValueOnce(of(fastingDay));
        api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(fastingDay)));
        startNow.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.START_FAST_NOW').click();
        redraw(harness);

        expect(api.skipCyclicDay).toHaveBeenCalledTimes(2);
        expect(getButton(management, 'FASTING.SKIP_FASTING_PERIOD').disabled).toBe(false);
    });
});

describe('Routed extended fasting confirmations', () => {
    it('requires an explicit confirmation before extending a session beyond the warning threshold', async () => {
        const active = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: EXTENDED_WARNING_HOURS,
            initialPlannedDurationHours: EXTENDED_WARNING_HOURS,
        });
        const { harness, api } = await renderFastingAsync(active);
        const response = new Subject<FastingSession>();
        api.extend.mockReturnValueOnce(response);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const management = getDialog('fd-fasting-session-management-dialog');
        const extendPanel = Array.from(management.querySelectorAll('button')).find(button =>
            button.textContent.includes('FASTING.EXTEND_GROUP'),
        );
        if (extendPanel === undefined) {
            throw new Error('Expected the extension panel');
        }
        extendPanel.click();
        redraw(harness);
        const addDay = getButton(management, 'FASTING.ADD_DAY');
        addDay.click();
        redraw(harness);
        const warning = getDialog('fd-fasting-safety-dialog');
        expect(warning.textContent).toContain('FASTING.EXTEND_WARNING_MESSAGE');
        expect(api.extend).not.toHaveBeenCalled();
        getButton(warning, 'FASTING.CANCEL_ACTION').click();
        redraw(harness);
        expect(api.extend).not.toHaveBeenCalled();

        addDay.click();
        redraw(harness);
        getButton(getDialog('fd-fasting-safety-dialog'), 'FASTING.CONFIRM_ADD_ACTION').click();
        redraw(harness);
        expect(api.extend).toHaveBeenCalledExactlyOnceWith({ additionalHours: EXTEND_ONE_DAY_HOURS });
        expect(addDay.disabled).toBe(true);

        response.next({
            ...active,
            plannedDurationHours: EXTENDED_WARNING_HOURS + EXTEND_ONE_DAY_HOURS,
            addedDurationHours: EXTEND_ONE_DAY_HOURS,
        });
        response.complete();
        redraw(harness);
        expect(addDay.disabled).toBe(false);
    });
});

describe('Routed fasting development controls', () => {
    it('opens the native setup, applies a development draft and clears it without starting a session', async () => {
        const { harness, api, developmentScreens } = await renderFastingAsync(null);
        const page = developmentScreens.get('fasting');
        if (page?.openDraft === undefined) {
            throw new Error('Expected the fasting page to expose its setup');
        }
        expect(page.read()).toMatchObject({ loading: false, active: false, session: null });
        expect(page.isDirty?.()).toBe(false);
        page.openDraft();
        redraw(harness);
        const dialog = getDialog('fd-fasting-protocol-dialog');
        const setup = developmentScreens.get('fasting-setup');
        if (setup?.fillDraft === undefined || setup.clearDraft === undefined) {
            throw new Error('Expected the fasting setup to expose draft editing');
        }
        const baseline = setup.read();

        expect(() => setup.fillDraft?.({ mode: 'intermittent', fastHours: RESTORED_INTERMITTENT_HOURS, fastDays: 1 })).toThrow(
            'Cycle days apply only to cyclic setup.',
        );
        expect(setup.read()).toEqual(baseline);
        setup.fillDraft({ mode: 'extended', fastHours: CUSTOM_TARGET_HOURS });
        redraw(harness);
        expectSelectedFastingSetup(dialog, 'FASTING.MODE_EXTENDED', 'FASTING.PROTOCOL_CUSTOM', CUSTOM_TARGET_HOURS);

        setup.fillDraft({ mode: 'intermittent', fastHours: RESTORED_INTERMITTENT_HOURS });
        redraw(harness);
        expectSelectedFastingSetup(
            dialog,
            'FASTING.MODE_INTERMITTENT',
            'FASTING.PROTOCOL_CUSTOM_INTERMITTENT',
            RESTORED_INTERMITTENT_HOURS,
        );
        expect(setup.isDirty?.()).toBe(true);

        setup.clearDraft();
        redraw(harness);
        expect(setup.read()).toEqual(baseline);
        expect(setup.isDirty?.()).toBe(false);
        expect(api.start).not.toHaveBeenCalled();
        dialog.querySelector<HTMLButtonElement>('button[aria-label="COMMON.CLOSE"]')?.click();
        redraw(harness);
        expect(developmentScreens.has('fasting-setup')).toBe(false);
    });

    it('opens editable Custom controls for a developer cyclic draft outside the displayed presets', async () => {
        const { harness, api, developmentScreens } = await renderFastingAsync(null);
        api.start.mockReturnValueOnce(new Subject<FastingSession>());
        const dialog = openIdleProtocolSettings(harness);
        const setup = developmentScreens.get('fasting-setup');
        if (setup?.fillDraft === undefined) {
            throw new Error('Expected the fasting setup to expose draft editing');
        }

        const fastHours = CYCLIC_EAT_DAY_FAST_HOURS;
        setup.fillDraft({ mode: 'cyclic', fastDays: CYCLIC_FAST_DAYS, eatDays: CYCLIC_EAT_DAYS, fastHours });
        redraw(harness);
        expect(getButton(dialog, '2:1').getAttribute('aria-checked')).toBe('true');

        const customDays = { fastDays: 4, eatDays: 5 };
        const editedFastDays = 6;
        setup.fillDraft({ mode: 'cyclic', ...customDays, fastHours });
        redraw(harness);
        expect(getButton(dialog, 'FASTING.CUSTOM_CYCLE').getAttribute('aria-checked')).toBe('true');
        enterNumber(dialog, 'FASTING.CYCLIC_FAST_DAYS_SHORT', editedFastDays, harness);
        expect(api.start).not.toHaveBeenCalled();

        getButton(dialog, 'FASTING.START_FAST').click();
        expect(api.start).toHaveBeenCalledOnce();
        const [intent] = api.start.mock.calls[0];
        if (intent.planType !== 'Cyclic') {
            throw new Error('Expected to start a cyclic fasting plan');
        }
        expect(intent.fastDays).toBe(editedFastDays);
        expect(intent.eatDays).toBe(customDays.eatDays);
        expect(intent.eatDayWindow.fastHours).toBe(fastHours);
        expect(intent.eatDayWindow.eatingWindowHours).toBe(CYCLIC_EAT_DAY_WINDOW_HOURS);
    });
});

function expectSelectedFastingSetup(dialog: HTMLElement, mode: string, protocol: string, hours: number): void {
    expect(getButton(dialog, mode).getAttribute('aria-checked')).toBe('true');
    expect(getButton(dialog, protocol).getAttribute('aria-checked')).toBe('true');
    expect(dialog.querySelector<HTMLInputElement>('input')?.value).toBe(String(hours));
}

const FASTING_HISTORY_DIALOG = 'fd-fasting-history-dialog';
const FASTING_DETAILS_DIALOG = 'fd-fasting-session-details-dialog';
const FASTING_HISTORY_ROW = '.fasting-history-dialog__row';

function openFastingHistory(harness: RouterTestingHarness): HTMLElement {
    const action = pageRoot(harness).querySelector<HTMLButtonElement>('.fasting-redesign__history-action button');
    if (action === null) {
        throw new Error('Expected the fasting history action');
    }
    action.click();
    redraw(harness);
    return getDialog(FASTING_HISTORY_DIALOG);
}

function openRecentFastingDetails(harness: RouterTestingHarness): HTMLElement {
    const row = pageRoot(harness).querySelector<HTMLButtonElement>('.fasting-redesign__history-row');
    if (row === null) {
        throw new Error('Expected a recent fasting session');
    }
    row.click();
    redraw(harness);
    return getDialog(FASTING_DETAILS_DIALOG);
}

function updatedFastingCheckIns(): FastingCheckIn[] {
    return [
        {
            id: 'after-the-walk',
            notes: 'After the walk',
            checkedInAtUtc: '2026-05-19T10:00:00Z',
            hungerLevel: 3,
            energyLevel: 4,
            moodLevel: 4,
            symptoms: [],
        },
        {
            id: 'before-lunch',
            notes: 'Before lunch',
            checkedInAtUtc: '2026-05-19T08:00:00Z',
            hungerLevel: 2,
            energyLevel: 3,
            moodLevel: 3,
            symptoms: [],
        },
    ];
}

function archivedFastingSessions(prefix: string, size: number): FastingSession[] {
    return Array.from({ length: size }, (_, index) =>
        makeSession({
            id: entityId<'fasting-session'>(`${prefix}-${index}`),
            endedAtUtc: utcInstant(FIXTURE_TIME),
            status: 'Completed',
            isCompleted: true,
        }),
    );
}

describe('Fasting history pagination during a concurrent history change', () => {
    it('shows each session once and opens the refreshed check-ins when the next page repeats a session', async () => {
        const pageSize = makeOverview(null).history.limit;
        const middlePage = 2;
        const lastPage = 3;
        const overlap = makeSession({
            id: entityId<'fasting-session'>('overlap'),
            planType: 'Extended',
            protocol: 'Fast24',
            initialPlannedDurationHours: EXTEND_ONE_DAY_HOURS,
            plannedDurationHours: EXTEND_ONE_DAY_HOURS,
            endedAtUtc: utcInstant(FIXTURE_TIME),
            status: 'Completed',
            isCompleted: true,
        });
        const firstPage = archivedFastingSessions('recent', pageSize - 1);
        const overview = makeOverview(null, [...firstPage, overlap]);
        overview.history = { ...overview.history, totalPages: lastPage, totalItems: pageSize * middlePage + 1 };
        const { harness, api } = await renderFastingAsync(null, overview);
        const older = makeSession({
            id: entityId<'fasting-session'>('older'),
            startedAtUtc: utcInstant('2026-05-16T12:00:00Z'),
            endedAtUtc: utcInstant('2026-05-17T12:00:00Z'),
            status: 'Completed',
            isCompleted: true,
        });
        const checkIns = updatedFastingCheckIns();
        const shiftedHistory = { ...overview.history, totalItems: overview.history.totalItems + 1 };
        api.getHistory
            .mockReturnValueOnce(
                of({
                    ...shiftedHistory,
                    data: [{ ...overlap, checkIns }, ...archivedFastingSessions('middle', pageSize - 1)],
                    page: middlePage,
                }),
            )
            .mockReturnValueOnce(
                of({
                    ...shiftedHistory,
                    data: [older, { ...older, id: entityId<'fasting-session'>('oldest') }],
                    page: lastPage,
                }),
            );
        const history = openFastingHistory(harness);
        expect(history.querySelectorAll(FASTING_HISTORY_ROW)).toHaveLength(pageSize);
        getButton(history, 'FASTING.LOAD_MORE_HISTORY').click();
        redraw(harness);
        expect(api.getHistory).toHaveBeenCalledOnce();

        const rows = Array.from(history.querySelectorAll<HTMLButtonElement>(FASTING_HISTORY_ROW));
        expect(rows).toHaveLength(pageSize * middlePage - 1);
        const refreshedRow = rows.find(row => row.textContent.includes('FASTING.EXTENDED_TYPE'));
        if (refreshedRow === undefined) {
            throw new Error('Expected to find the updated extended fast in its original row');
        }
        expect(rows[pageSize - 1]).toBe(refreshedRow);
        getButton(history, 'FASTING.LOAD_MORE_HISTORY').click();
        redraw(harness);
        expect(api.getHistory.mock.calls.map(([query]) => query.page)).toEqual([middlePage, lastPage]);
        expect(history.querySelectorAll(FASTING_HISTORY_ROW)).toHaveLength(pageSize * middlePage + 1);
        expect(history.textContent).not.toContain('FASTING.LOAD_MORE_HISTORY');
        refreshedRow.click();
        redraw(harness);
        expect(document.querySelector(FASTING_HISTORY_DIALOG)).toBeNull();

        const details = getDialog(FASTING_DETAILS_DIALOG);
        expect(details.querySelector('fd-fasting-check-in-chart')).not.toBeNull();
        expect(details.querySelectorAll('fd-fasting-history-check-in-entry')).toHaveLength(checkIns.length);
        expect(details.textContent).toContain(checkIns[0].notes);
        expect(details.textContent).toContain(checkIns[1].notes);
        getButton(details, 'COMMON.CLOSE').click();
        redraw(harness);
        expect(document.querySelector(FASTING_DETAILS_DIALOG)).toBeNull();
    });
});

describe('Fasting history dialogs when leaving the route', () => {
    it.each([
        { selector: FASTING_HISTORY_DIALOG, open: openFastingHistory },
        { selector: FASTING_DETAILS_DIALOG, open: openRecentFastingDetails },
    ])('dismisses $selector when its route is replaced', async ({ selector, open }) => {
        const active = makeSession();
        const pageSize = makeOverview(null).history.limit;
        const earlier = archivedFastingSessions('earlier', pageSize - 1);
        const overview = makeOverview(active, [active, ...earlier]);
        overview.history = { ...overview.history, totalPages: FINAL_HISTORY_PAGE, totalItems: pageSize + 1 };
        const { harness, api } = await renderFastingAsync(active, overview);
        const popup = open(harness);
        const pending = new Subject<FastingOverview['history']>();
        if (selector === FASTING_HISTORY_DIALOG) {
            api.getHistory.mockReturnValueOnce(pending);
            getButton(popup, 'FASTING.LOAD_MORE_HISTORY').click();
            redraw(harness);
            expect(pending.observed).toBe(true);
        } else {
            expect(popup.querySelector('.fasting-session-details-dialog__empty')?.textContent).toContain('FASTING.REDESIGN.NO_CHECK_INS');
        }

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(pending.observed).toBe(false);
        expect(document.querySelector(selector)).toBeNull();
    });
});

describe('Fasting operation dialogs when leaving the route', () => {
    it('dismisses protocol setup and cancels its pending request when leaving the route', async () => {
        const { harness, api, showSuccess } = await renderFastingAsync(null);
        const starting = new Subject<FastingSession>();
        api.start.mockReturnValueOnce(starting);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-protocol-dialog');
        getButton(dialog, 'FASTING.START_FAST').click();
        redraw(harness);
        expect(starting.observed).toBe(true);
        expect(getButton(dialog, 'FASTING.START_FAST').disabled).toBe(true);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(starting.observed).toBe(false);
        expect(document.querySelector('fd-fasting-protocol-dialog')).toBeNull();

        starting.next(makeSession());
        starting.complete();
        expect(showSuccess).not.toHaveBeenCalled();
        await harness.navigateByUrl('/fasting');
        getButton(pageRoot(harness), 'FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        redraw(harness);
        expect(getButton(getDialog('fd-fasting-protocol-dialog'), 'FASTING.START_FAST').disabled).toBe(false);
    });

    it('dismisses active management and cancels its pending duration request when leaving the route', async () => {
        const session = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            initialPlannedDurationHours: EXTENDED_HOURS,
            plannedDurationHours: EXTENDED_HOURS,
        });
        const { harness, api } = await renderFastingAsync(session);
        const reducing = new Subject<FastingSession>();
        api.reduceTarget.mockReturnValueOnce(reducing);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const dialog = getDialog('fd-fasting-session-management-dialog');
        const reducePanel = dialog.querySelector<HTMLButtonElement>('.fasting-controls__extend-panel--subtle > button');
        if (reducePanel === null) {
            throw new Error('Expected the target reduction controls');
        }
        reducePanel.click();
        redraw(harness);
        getButton(dialog, 'FASTING.REDUCE_4_HOURS').click();
        redraw(harness);
        expect(api.reduceTarget).toHaveBeenCalledExactlyOnceWith({ reducedHours: REDUCED_HOURS });
        expect(reducing.observed).toBe(true);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(reducing.observed).toBe(false);
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
    });

    it('dismisses an unanswered end confirmation after leaving without ending the fast', async () => {
        const { harness, api } = await renderFastingAsync(makeSession());
        getButton(pageRoot(harness), 'FASTING.END_FAST').click();
        redraw(harness);
        const confirm = getButton(getDialog('fd-fasting-end-confirm-dialog'), 'FASTING.END_FAST');
        expect(confirm.disabled).toBe(false);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(document.querySelector('fd-fasting-end-confirm-dialog')).toBeNull();
        expect(api.end).not.toHaveBeenCalled();
    });
});

describe('Fasting management confirmations when leaving the route', () => {
    it.each([
        { action: 'FASTING.START_FAST_NOW', message: 'FASTING.START_FAST_NOW_CONFIRM_MESSAGE', request: 'skipCyclicDay' },
        { action: 'FASTING.STOP_CYCLE', message: 'FASTING.STOP_CYCLE_CONFIRM_MESSAGE', request: 'end' },
    ] as const)('dismisses a nested $action confirmation without applying it', async ({ action, message, request }) => {
        const eatingDay = makeSession({
            planType: 'Cyclic',
            protocol: 'Cyclic',
            occurrenceKind: 'EatDay',
            cyclicFastDays: CYCLIC_FAST_DAYS,
            cyclicEatDays: CYCLIC_EAT_DAYS,
            cyclicEatDayFastHours: CYCLIC_EAT_DAY_FAST_HOURS,
            cyclicEatDayEatingWindowHours: CYCLIC_EAT_DAY_WINDOW_HOURS,
        });
        const { harness, api } = await renderFastingAsync(eatingDay);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        getButton(getDialog('fd-fasting-session-management-dialog'), action).click();
        redraw(harness);
        const confirmation = getDialog('fd-fasting-end-confirm-dialog');
        expect(confirmation.textContent).toContain(message);
        expect(getButton(confirmation, action).disabled).toBe(false);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
        expect(document.querySelector('fd-fasting-end-confirm-dialog')).toBeNull();
        expect(api[request]).not.toHaveBeenCalled();
    });

    it.each([
        {
            duration: EXTENDED_WARNING_HOURS,
            message: 'FASTING.EXTEND_WARNING_MESSAGE',
            action: 'FASTING.CONFIRM_ADD_ACTION',
        },
        {
            duration: FASTING_HARD_STOP_THRESHOLD_HOURS,
            message: 'FASTING.LIFE_RISK_MESSAGE',
            action: 'FASTING.CLOSE_ACTION',
        },
    ])('dismisses the $message dialog without extending the session', async ({ duration, message, action }) => {
        const active = makeSession({
            planType: 'Extended',
            protocol: 'Custom',
            plannedDurationHours: duration,
            initialPlannedDurationHours: duration,
        });
        const { harness, api } = await renderFastingAsync(active);
        getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
        redraw(harness);
        const management = getDialog('fd-fasting-session-management-dialog');
        const extendPanel = Array.from(management.querySelectorAll('button')).find(button =>
            button.textContent.includes('FASTING.EXTEND_GROUP'),
        );
        if (extendPanel === undefined) {
            throw new Error('Expected the extension panel');
        }
        extendPanel.click();
        redraw(harness);
        getButton(management, 'FASTING.ADD_DAY').click();
        redraw(harness);
        const warning = getDialog('fd-fasting-safety-dialog');
        expect(warning.textContent).toContain(message);
        expect(getButton(warning, action).disabled).toBe(false);

        await harness.navigateByUrl('/another-page', AnotherPageComponent);
        expect(pageRoot(harness).querySelector('h1')?.textContent).toBe('Another page');
        expect(document.querySelector('fd-fasting-session-management-dialog')).toBeNull();
        expect(document.querySelector('fd-fasting-safety-dialog')).toBeNull();
        expect(api.extend).not.toHaveBeenCalled();
    });
});

describe('Rendered cyclic day actions during an overview refresh', () => {
    it.each([
        {
            method: 'skipCyclicDay',
            endpoint: 'skip-day',
            oldAction: 'FASTING.START_FAST_NOW',
            freshAction: 'FASTING.SKIP_FASTING_PERIOD',
            freshMessage: 'FASTING.SKIP_FASTING_PERIOD_CONFIRM_MESSAGE',
        },
        {
            method: 'postponeCyclicDay',
            endpoint: 'postpone-day',
            oldAction: 'FASTING.SKIP_DAY',
            freshAction: 'FASTING.SKIP_DAY',
            freshMessage: 'FASTING.SKIP_FAST_DAY_CONFIRM_MESSAGE',
        },
    ] as const)(
        'keeps $method attached to the rendered day before confirmation',
        async ({ method, endpoint, oldAction, freshAction, freshMessage }) => {
            TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
            const day = makeCyclicEatingDay();
            const replacement = {
                ...day,
                id: REPLACEMENT_SESSION_ID,
                occurrenceKind: 'FastDay',
                startedAtUtc: utcInstant(FIXTURE_TIME),
                initialPlannedDurationHours: EXTEND_ONE_DAY_HOURS,
                plannedDurationHours: EXTEND_ONE_DAY_HOURS,
                cyclicPhaseDayNumber: CYCLIC_FAST_DAYS,
                cyclicPhaseDayTotal: CYCLIC_FAST_DAYS,
            } satisfies FastingSession;
            const { harness, api } = await renderFastingAsync(day);
            const transport = TestBed.runInInjectionContext(() => new FastingService());
            const http = TestBed.inject(HttpTestingController);
            const request = { method: 'PUT', url: `${environment.apiUrls.fasting}/current/${endpoint}` };
            const overview = new Subject<FastingOverview>();
            api[method].mockImplementation(() => transport[method]());
            api.updateCheckIn.mockReturnValueOnce(of({ ...day, checkInAtUtc: utcInstant(FIXTURE_TIME), checkInNotes: SAVED_NOTE }));
            api.getOverviewStrict.mockReturnValueOnce(overview);

            submitFastingCheckInNote(harness, SAVED_NOTE);
            expect(overview.observed).toBe(true);
            getButton(pageRoot(harness), 'FASTING.REDESIGN.MANAGE').click();
            redraw(harness);
            const management = getDialog('fd-fasting-session-management-dialog');
            const oldButton = getButton(management, oldAction);

            overview.next(makeOverview(replacement));
            overview.complete();
            expect(oldButton.isConnected).toBe(true);
            expect(oldButton.disabled).toBe(false);
            oldButton.click();
            redraw(harness);

            const staleConfirmation = document.querySelector('fd-fasting-end-confirm-dialog');
            if (staleConfirmation !== null) {
                getButton(staleConfirmation, freshAction).click();
                redraw(harness);
            }
            http.expectNone(request, 'a button rendered for the eating day cannot change the replacement fasting day');
            expect(staleConfirmation).toBeNull();

            getButton(management, freshAction).click();
            redraw(harness);
            const confirmation = getDialog('fd-fasting-end-confirm-dialog');
            expect(confirmation.textContent).toContain(freshMessage);
            getButton(confirmation, freshAction).click();
            redraw(harness);
            const next = {
                ...day,
                id: entityId<'fasting-session'>('next-eating-day'),
                startedAtUtc: utcInstant(FIXTURE_TIME),
            } satisfies FastingSession;
            api.getOverviewStrict.mockReturnValueOnce(of(makeOverview(next)));
            http.expectOne(request).flush(next);
            redraw(harness);
            expect(getButton(management, 'FASTING.START_FAST_NOW').disabled).toBe(false);
            http.verify();
        },
    );
});
