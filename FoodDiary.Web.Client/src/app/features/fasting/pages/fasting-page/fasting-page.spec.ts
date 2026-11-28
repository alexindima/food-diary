import { registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { type FdTourDefinition, FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, type Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import type { FastingInsights, FastingProtocol, FastingSession, FastingStats } from '../../../../shared/models/fasting.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { FastingCheckInDialogResult } from '../../components/fasting-check-in-dialog/fasting-check-in-dialog';
import { FastingEndConfirmDialogComponent } from '../../components/fasting-end-confirm-dialog/fasting-end-confirm-dialog';
import {
    FastingHistoryDialogComponent,
    type FastingHistoryDialogData,
} from '../../components/fasting-history-dialog/fasting-history-dialog';
import { FastingProtocolDialogComponent } from '../../components/fasting-protocol-dialog/fasting-protocol-dialog';
import {
    FastingSessionDetailsDialogComponent,
    type FastingSessionDetailsDialogData,
} from '../../components/fasting-session-details-dialog/fasting-session-details-dialog';
import { FastingSessionManagementDialogComponent } from '../../components/fasting-session-management-dialog/fasting-session-management-dialog';
import { FastingFacade } from '../../lib/fasting.facade';
import type { IntermittentFastingProtocol } from '../../models/fasting-start-intent';
import { FastingPageComponent } from './fasting-page';

const SESSION_CHECK_INS_BEYOND_PAGE_SIZE = 6;
const DEFAULT_SESSION_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const DEFAULT_REDUCE_HOURS = 4;
const CHECK_IN_HUNGER_LEVEL = 2;
const CHECK_IN_ENERGY_LEVEL = 4;
const CHECK_IN_MOOD_LEVEL = 3;
const HISTORY_START_HOUR = 10;
const INITIAL_VISIBLE_CHECK_INS = 5;
const CYCLIC_FAST_DAYS = 2;
const CYCLIC_EAT_DAYS = 1;
const CYCLIC_EAT_FAST_HOURS = 14;
const CYCLIC_EAT_WINDOW_HOURS = 10;

registerLocaleData(localeRu);

let component: FastingPageComponent;
let fixture: ComponentFixture<FastingPageComponent>;
let facade: FastingFacadeMock;
let toastService: { success: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };
let tourService: { start: ReturnType<typeof vi.fn> };
let dialogData: FastingHistoryDialogData | FastingSessionDetailsDialogData;
let dialogRef: { close: ReturnType<typeof vi.fn> };

describe('FastingPageComponent rendered workflow', () => {
    beforeEach(createComponentAsync);

    it('shows loading before revealing the fasting start action and protocol settings', () => {
        facade.isLoading.set(true);
        fixture.detectChanges();

        expect(pageHost().querySelector('.fasting__skeleton-grid')).not.toBeNull();
        expect(pageHost().querySelector('fd-fasting-redesign-preview')).toBeNull();

        facade.isLoading.set(false);
        fixture.detectChanges();
        pageButton('FASTING.START_FAST').click();
        pageButton('FASTING.REDESIGN.CHANGE_PROTOCOL').click();

        expect(pageHost().querySelector('.fasting__skeleton-grid')).toBeNull();
        expect(facade.startFasting).toHaveBeenCalledOnce();
        expect(dialogService.open).toHaveBeenCalledWith(
            FastingProtocolDialogComponent,
            expect.objectContaining({ providers: [{ provide: FastingFacade, useValue: facade }] }),
        );
    });

    it('starts the page tour from its accessible help button', () => {
        pageHost().querySelector<HTMLButtonElement>('button[aria-label="FASTING.TOUR.START_TOUR"]')?.click();

        expect(tourService.start).toHaveBeenCalledOnce();
        const [definition, options] = tourService.start.mock.calls[0] as [FdTourDefinition, { force: boolean }];
        expect(definition.id).toBe('fasting-page');
        expect(definition.steps[0]?.id).toBe('timer');
        expect(options.force).toBe(true);
    });

    it.each([
        ['Extended', 'FASTING.INTERRUPT_FAST', 'FASTING.INTERRUPT_CONFIRM_TITLE'],
        ['Cyclic', 'FASTING.STOP_CYCLE', 'FASTING.STOP_CYCLE_CONFIRM_TITLE'],
    ] as const)('asks before ending a %s fast and ignores cancellation', (planType, actionLabel, title) => {
        facade.currentSession.set({ ...createSession(), planType });
        facade.isActive.set(true);
        const result = new Subject<'confirm' | 'cancel'>();
        dialogService.open.mockReturnValue({ afterClosed: () => result });
        fixture.detectChanges();

        pageButton(actionLabel).click();
        expect(dialogService.open.mock.lastCall).toMatchObject([
            FastingEndConfirmDialogComponent,
            { preset: 'confirm', data: { title, confirmLabel: actionLabel } },
        ]);

        result.next('cancel');
        result.complete();
        expect(facade.endFasting).not.toHaveBeenCalled();
    });

    it('preserves a saved check-in draft and ignores a dialog result after navigation', () => {
        facade.currentSession.set(createSession());
        facade.isActive.set(true);
        const saved = new Subject<FastingCheckInDialogResult>();
        const pending = new Subject<FastingCheckInDialogResult>();
        const closePending = vi.fn();
        dialogService.open
            .mockReturnValueOnce({ afterClosed: () => saved })
            .mockReturnValueOnce({ afterClosed: () => pending, close: closePending });
        fixture.detectChanges();

        pageButton('FASTING.REDESIGN.CHECK_IN').click();
        saved.next('saved');
        saved.complete();
        expect(facade.resetCheckInDraft).not.toHaveBeenCalled();

        pageButton('FASTING.REDESIGN.CHECK_IN').click();
        expect(dialogService.open).toHaveBeenCalledTimes(2);
        fixture.destroy();
        expect(closePending).toHaveBeenCalledOnce();
        pending.next('cancel');
        pending.complete();
        expect(facade.resetCheckInDraft).not.toHaveBeenCalled();
    });
});

describe('FastingPageComponent overview recovery', () => {
    beforeEach(createComponentAsync);

    it('offers Retry while hiding the start action after an overview failure', () => {
        facade.overviewLoadError.set(true);
        fixture.detectChanges();

        expect(pageHost().querySelector('[role="alert"]')?.textContent ?? '').toContain('FASTING.OVERVIEW_LOAD_ERROR');
        expect(pageHost().querySelector('fd-fasting-redesign-preview')).toBeNull();

        pageButton('ERRORS.RETRY').click();
        expect(facade.initialize).toHaveBeenCalledTimes(2);

        facade.overviewLoadError.set(false);
        fixture.detectChanges();

        expect(pageHost().querySelector('[role="alert"]')).toBeNull();
        expect(pageButton('FASTING.START_FAST').disabled).toBe(false);
    });
});

describe('FastingPageComponent rendered dialogs', () => {
    beforeEach(createComponentAsync);

    it('keeps protocol settings open after a failed start and closes after a successful retry', () => {
        pageButton('FASTING.REDESIGN.CHANGE_PROTOCOL').click();
        expect(dialogService.open).toHaveBeenCalledWith(FastingProtocolDialogComponent, expect.any(Object));
        const dialog = TestBed.createComponent(FastingProtocolDialogComponent);
        dialog.detectChanges();
        const dialogHost = dialog.nativeElement as HTMLElement;
        const start = pageButton('FASTING.START_FAST', dialogHost);
        start.click();
        expect(facade.startFasting).toHaveBeenCalledOnce();

        facade.isStarting.set(true);
        dialog.detectChanges();
        expect(start.disabled).toBe(true);
        facade.isStarting.set(false);
        facade.requestError.set('FASTING.REQUEST_ERROR');
        dialog.detectChanges();
        expect(dialogHost.textContent).toContain('FASTING.REQUEST_ERROR');
        expect(dialogRef.close).not.toHaveBeenCalled();

        start.click();
        expect(facade.startFasting).toHaveBeenCalledTimes(2);
        facade.requestError.set(null);
        facade.currentSession.set(createSession());
        facade.isActive.set(true);
        dialog.detectChanges();
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('opens completed-session notes and check-ins from the recent history row', () => {
        const historical = {
            ...createHistorySession('noted-fast', 2),
            status: 'Interrupted' as const,
            endedAtUtc: utcInstant('2026-04-12T20:00:00Z'),
            notes: 'Paused during a long walk',
        };
        facade.history.set([historical]);
        fixture.detectChanges();
        pageHost().querySelector<HTMLButtonElement>('button[aria-label="FASTING.REDESIGN.OPEN_SESSION_DETAILS"]')?.click();

        expect(dialogService.open).toHaveBeenCalledWith(FastingSessionDetailsDialogComponent, expect.any(Object));
        dialogData = (dialogService.open.mock.calls[0][1] as { data: FastingSessionDetailsDialogData }).data;
        const dialog = TestBed.createComponent(FastingSessionDetailsDialogComponent);
        dialog.detectChanges();
        const dialogHost = dialog.nativeElement as HTMLElement;
        expect(dialogHost.textContent).toContain('Paused during a long walk');
        expect(dialogHost.textContent).toContain('FASTING.BADGE_INTERRUPTED');
        expect(dialogHost.textContent).toContain('→');
        expect(dialogHost.querySelector('fd-fasting-check-in-chart')).not.toBeNull();
        expect(dialogHost.querySelectorAll('fd-fasting-history-check-in-entry')).toHaveLength(2);

        pageButton('COMMON.CLOSE', dialogHost).click();
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });
});

describe('FastingPageComponent rendered legacy check-in', () => {
    beforeEach(createComponentAsync);

    it('shows recorded energy and notes without inventing missing ratings', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { FASTING: { CHECK_IN: { SUMMARY: '{{hunger}} / {{energy}} / {{mood}}' } } });
        await firstValueFrom(translate.use('en'));
        facade.history.set([
            {
                ...createHistorySession('legacy-partial-check-in', 0),
                status: 'Completed',
                endedAtUtc: utcInstant('2026-04-12T19:00:00Z'),
                checkInAtUtc: utcInstant('2026-04-12T18:00:00Z'),
                energyLevel: CHECK_IN_ENERGY_LEVEL,
                checkInNotes: 'Recorded during the fast',
            },
        ]);
        fixture.detectChanges();

        pageHost().querySelector<HTMLButtonElement>('button[aria-label="FASTING.REDESIGN.OPEN_SESSION_DETAILS"]')?.click();
        dialogData = (dialogService.open.mock.calls[0][1] as { data: FastingSessionDetailsDialogData }).data;
        const dialog = TestBed.createComponent(FastingSessionDetailsDialogComponent);
        dialog.detectChanges();

        const host = dialog.nativeElement as HTMLElement;
        const entries = host.querySelectorAll('fd-fasting-history-check-in-entry');
        expect(entries).toHaveLength(1);
        expect(entries[0].textContent).toContain('— / ⚡ / —');
        expect(entries[0].textContent).toContain('Recorded during the fast');
        expect(host.querySelector('fd-fasting-check-in-chart')).toBeNull();
    });
});

describe('FastingPageComponent rendered history and alerts', () => {
    beforeEach(createComponentAsync);

    it('passes live history and delegates row selection and pagination to the page', () => {
        const endedAtUtc = utcInstant('2026-04-13T06:00:00Z');
        const historical = { ...createHistorySession('historical', 2), endedAtUtc, status: 'Completed' as const };
        facade.history.set([historical]);
        facade.historyTotalPages.set(2);
        fixture.detectChanges();

        pageButton('FASTING.REDESIGN.VIEW_HISTORY').click();
        expect(dialogService.open).toHaveBeenCalledWith(FastingHistoryDialogComponent, expect.objectContaining({ preset: 'detail' }));
        const history = renderHistoryDialog();
        expect(history.textContent).toContain('FASTING.BADGE_COMPLETED');
        expect(history.textContent).toContain('24 FASTING.HOURS');

        pageButton('FASTING.LOAD_MORE_HISTORY', history).click();
        expect(facade.loadMoreHistory).toHaveBeenCalledOnce();
        facade.historyPage.set(2);
        facade.historyError.set('FASTING.REQUEST_ERROR');
        TestBed.tick();
        expect(history.textContent).not.toContain('FASTING.LOAD_MORE_HISTORY');
        expect(history.querySelector('[role="alert"]')?.textContent).toContain('FASTING.REQUEST_ERROR');

        history.querySelector<HTMLButtonElement>('button[aria-label="FASTING.REDESIGN.OPEN_SESSION_DETAILS"]')?.click();
        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogService.open.mock.lastCall).toMatchObject([FastingSessionDetailsDialogComponent, { data: { session: historical } }]);
    });

    it('updates an open history dialog when results and language change, and closes from its footer', async () => {
        const translate = TestBed.inject(TranslateService);
        const historical = {
            ...createHistorySession('short-fast', 0),
            endedAtUtc: utcInstant('2026-04-12T06:30:00Z'),
            status: 'Completed' as const,
        };
        pageButton('FASTING.REDESIGN.VIEW_HISTORY').click();
        const history = renderHistoryDialog();

        expect(history.textContent).toContain('FASTING.NO_HISTORY');
        expect(history.querySelector('.fasting-history-dialog__row')).toBeNull();

        facade.history.set([historical]);
        TestBed.tick();
        expect(history.textContent).toContain('April');
        expect(history.textContent).toContain('30 FASTING.MINUTES');
        expect(history.querySelector('.fasting-history-dialog__status--interrupted')?.textContent).toContain('FASTING.BADGE_ENDED_EARLY');

        await firstValueFrom(translate.use('ru'));
        TestBed.tick();
        expect(history.textContent).toContain('апреля');
        expect(history.textContent).not.toContain('April');

        pageButton('COMMON.CLOSE', history).click();
        expect(dialogRef.close).toHaveBeenCalledOnce();
    });

    it('localizes message parameters, filters hidden prompts, and dismisses the visible prompt', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { FASTING: { SUMMARY: '{{stage}} after {{duration}}', STAGE: 'Water' } });
        translate.setTranslation('ru', { FASTING: { SUMMARY: '{{stage}} спустя {{duration}}', STAGE: 'Вода' } });
        await firstValueFrom(translate.use('en'));
        facade.currentSession.set(createSession());
        facade.isPromptVisible.mockImplementation((_session: FastingSession, alert: { id: string }) => alert.id !== 'hidden-alert');
        const alert = {
            id: 'visible-alert',
            titleKey: 'FASTING.CUSTOM_TITLE',
            bodyKey: 'FASTING.SUMMARY',
            tone: 'warning' as const,
            bodyParams: { stage: 'FASTING.STAGE', duration: '12 h' },
        };
        facade.insightsData.set({
            alerts: [alert, { ...alert, id: 'hidden-alert', titleKey: 'FASTING.HIDDEN' }],
            insights: [{ ...alert, id: 'steady', titleKey: 'FASTING.STEADY', tone: 'positive', bodyParams: null }],
        });
        fixture.detectChanges();
        expect(pageHost().textContent).toContain('Water after 12 h');
        expect(pageHost().textContent).toContain('FASTING.STEADY');
        expect(pageHost().textContent).not.toContain('FASTING.HIDDEN');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(pageHost().textContent).toContain('Вода спустя 12 h');
        pageButton('FASTING.PROMPTS.DISMISS').click();
        expect(facade.dismissPrompt).toHaveBeenCalledExactlyOnceWith('visible-alert');
    });
});

describe('FastingPageComponent setup and actions', () => {
    beforeEach(createComponentAsync);

    it('initializes facade on init', () => {
        expect(facade.initialize).toHaveBeenCalledTimes(1);
    });

    it('opens the check-in dialog and resets the draft when it closes without saving', () => {
        facade.currentSession.set(createSession());
        fixture.detectChanges();
        pageButton('FASTING.REDESIGN.CHECK_IN').click();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.resetCheckInDraft).toHaveBeenCalledTimes(1);
    });

    it('opens session management confirmation and ends the active fast after confirmation', () => {
        facade.currentSession.set(createSession());
        dialogService.open.mockReturnValue({ afterClosed: () => of('confirm') });
        fixture.detectChanges();

        pageButton('FASTING.END_FAST').click();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.endFasting).toHaveBeenCalledTimes(1);
    });

    it('delegates save and load more actions to facade', () => {
        component['saveCheckIn']();
        component['loadMoreHistory']();
        component['dismissPrompt']('prompt-1');
        component['snoozePrompt']('prompt-2');

        expect(facade.saveCheckIn).toHaveBeenCalledTimes(1);
        expect(facade.loadMoreHistory).toHaveBeenCalledTimes(1);
        expect(facade.dismissPrompt).toHaveBeenCalledWith('prompt-1');
        expect(facade.snoozePrompt).toHaveBeenCalledWith('prompt-2');
    });

    it('announces a successful check-in when the saved version changes', () => {
        facade.checkInSavedVersion.set(1);
        fixture.detectChanges();

        expect(toastService.success).toHaveBeenCalledWith('FASTING.CHECK_IN.SAVED_TOAST');
    });

    it('does not subscribe the saved check-in effect to toast state changes', () => {
        const toastState = signal(0);
        toastService.success.mockImplementation(() => {
            toastState();
            toastState.update(value => value + 1);
        });

        facade.checkInSavedVersion.set(1);
        fixture.detectChanges();
        toastState.update(value => value + 1);
        fixture.detectChanges();

        expect(toastService.success).toHaveBeenCalledTimes(1);
    });

    it('toggles history accordion with single expanded session', () => {
        component['toggleHistorySession']('session-1');
        expect(component['isHistorySessionExpanded']('session-1')).toBe(true);
        expect(component['getHistoryCheckInToggleKey'](createHistorySession('session-1', 2))).toBe('FASTING.HIDE_HISTORY_CHECK_INS');

        component['toggleHistorySession']('session-2');
        expect(component['isHistorySessionExpanded']('session-1')).toBe(false);
        expect(component['isHistorySessionExpanded']('session-2')).toBe(true);

        component['toggleHistorySession']('session-2');
        expect(component['isHistorySessionExpanded']('session-2')).toBe(false);
        expect(component['getHistoryCheckInToggleKey'](createHistorySession('session-2', 2))).toBe('FASTING.SHOW_HISTORY_CHECK_INS');
    });
});

describe('FastingPageComponent history dialog', () => {
    beforeEach(createComponentAsync);

    it('opens full fasting history in a detail dialog', () => {
        component['openHistory']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        const options = dialogService.open.mock.calls[0]?.[1] as { preset?: string; data?: Record<string, unknown> } | undefined;
        expect(options?.preset).toBe('detail');
        expect(options?.data?.['historyItems']).toBe(component['historyItems']);
        expect(options?.data?.['onHistoryLoadMore']).toBeTypeOf('function');
    });

    it('opens recent session details with all wellbeing check-ins', () => {
        const session = createHistorySession('session-details', 2);

        component['openSessionDetails'](session);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        const options = dialogService.open.mock.calls[0]?.[1] as { preset?: string; data?: Record<string, unknown> } | undefined;
        expect(options?.preset).toBe('detail');
        expect(options?.data?.['session']).toBe(session);
        expect(options?.data?.['checkIns']).toHaveLength(2);
        expect(options?.data?.['chartCheckIns']).toHaveLength(2);
    });
});

describe('FastingPageComponent protocol settings', () => {
    beforeEach(createComponentAsync);

    it('opens settings with the current page facade only while fasting is inactive', () => {
        component['openProtocolSettings']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        const options = dialogService.open.mock.calls[0]?.[1] as { preset?: string; providers?: unknown[] } | undefined;
        expect(options?.preset).toBe('detail');
        expect(options?.providers).toHaveLength(1);

        facade.isActive.set(true);
        component['openProtocolSettings']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
    });
});

describe('FastingPageComponent active session controls', () => {
    beforeEach(createComponentAsync);

    it('opens active management with the page facade without enabling protocol replacement', () => {
        facade.isActive.set(true);
        facade.currentSession.set({ ...createSession(), planType: 'Extended' });

        component['openActiveSessionManagement']();
        component['openProtocolSettings']();

        expect(dialogService.open).toHaveBeenCalledExactlyOnceWith(FastingSessionManagementDialogComponent, {
            preset: 'detail',
            ariaLabel: 'FASTING.REDESIGN.MANAGE',
            providers: [{ provide: FastingFacade, useValue: facade }],
        });
        expect(facade.endFasting).not.toHaveBeenCalled();
    });

    it('does not open duration or cyclic controls for inactive or intermittent sessions', () => {
        component['openActiveSessionManagement']();
        facade.isActive.set(true);
        facade.currentSession.set(createSession());
        component['openActiveSessionManagement']();

        expect(dialogService.open).not.toHaveBeenCalled();
    });
});

describe('FastingPageComponent history labels', () => {
    beforeEach(createComponentAsync);

    it('maps history status to badge key and accent color', () => {
        expect(component['getHistoryBadgeKey']('Completed')).toBe('FASTING.BADGE_COMPLETED');
        expect(component['getHistoryBadgeKey']('Interrupted')).toBe('FASTING.BADGE_INTERRUPTED');
        expect(component['getHistoryBadgeKey']('Skipped')).toBe('FASTING.BADGE_SKIPPED');
        expect(component['getHistoryBadgeKey']('Postponed')).toBe('FASTING.BADGE_POSTPONED');
        expect(component['getHistoryBadgeKey']('Active')).toBe('FASTING.BADGE_INCOMPLETE');

        expect(component['getHistoryAccentColor']({ ...createSession(), status: 'Completed' })).toBe('var(--fd-color-green-500)');
        expect(component['getHistoryAccentColor']({ ...createSession(), status: 'Interrupted' })).toBe('var(--fd-color-orange-500)');
        expect(component['getHistoryAccentColor']({ ...createSession(), status: 'Skipped' })).toBe('var(--fd-color-sky-500)');
        expect(component['getHistoryAccentColor']({ ...createSession(), status: 'Postponed' })).toBe('var(--fd-color-ai)');
        expect(component['getHistoryAccentColor']({ ...createSession(), status: 'Active' })).toBe('var(--fd-color-slate-400)');
    });

    it('distinguishes an early ending from reaching the planned duration', () => {
        const early = {
            ...createSession(),
            status: 'Completed' as const,
            startedAtUtc: utcInstant('2026-10-05T08:00:00Z'),
            endedAtUtc: utcInstant('2026-10-05T09:00:00Z'),
        };
        expect(component['getHistoryBadgeKey'](early.status, early)).toBe('FASTING.BADGE_ENDED_EARLY');
        expect(component['getHistoryAccentColor'](early)).toBe('var(--fd-color-orange-500)');
    });

    it('builds protocol display for cyclic, custom, and adjusted sessions', () => {
        expect(
            component['getHistoryProtocolDisplay']({
                ...createSession(),
                planType: 'Cyclic',
                cyclicFastDays: CYCLIC_FAST_DAYS,
                cyclicEatDays: CYCLIC_EAT_DAYS,
                cyclicEatDayFastHours: CYCLIC_EAT_FAST_HOURS,
                cyclicEatDayEatingWindowHours: CYCLIC_EAT_WINDOW_HOURS,
            }),
        ).toBe('2:1 (14:10)');
        expect(component['getHistoryProtocolDisplay']({ ...createSession(), protocol: 'CustomIntermittent' })).toBe('16:8');
        expect(component['getHistoryProtocolDisplay']({ ...createSession(), protocol: 'Custom', addedDurationHours: 4 })).toBe(
            '16 FASTING.HOURS (+4 FASTING.HOURS)',
        );
        expect(component['getHistoryProtocolDisplay']({ ...createSession(), protocol: 'Custom', addedDurationHours: -2 })).toBe(
            '16 FASTING.HOURS (-2 FASTING.HOURS)',
        );
    });

    it('returns null for empty or invalid relative timestamps', () => {
        expect(component['formatRelativeTime'](null)).toBeNull();
        expect(component['formatRelativeTime']('')).toBeNull();
        expect(component['formatRelativeTime']('not-a-date')).toBeNull();
    });
});

describe('FastingPageComponent check-in history', () => {
    beforeEach(createComponentAsync);

    it('tracks session check-in pagination locally', () => {
        const session = createHistorySession('session-3', SESSION_CHECK_INS_BEYOND_PAGE_SIZE);

        expect(component['getVisibleSessionCheckIns'](session)).toHaveLength(INITIAL_VISIBLE_CHECK_INS);
        expect(component['canLoadMoreSessionCheckIns'](session)).toBe(true);

        component['loadMoreSessionCheckIns'](session.id);

        expect(component['getVisibleSessionCheckIns'](session)).toHaveLength(SESSION_CHECK_INS_BEYOND_PAGE_SIZE);
        expect(component['canLoadMoreSessionCheckIns'](session)).toBe(false);
    });

    it('has no check-ins to display for a session without ratings', () => {
        const session = createHistorySession('session-4', 0);

        expect(component['hasCheckIn'](session)).toBe(false);
        expect(component['getSessionCheckInCount'](session)).toBe(0);
        expect(component['getVisibleSessionCheckIns'](session)).toHaveLength(0);
    });

    it('displays one check-in from the legacy summary', () => {
        const session = {
            ...createHistorySession('session-5', 0),
            checkInAtUtc: utcInstant('2026-04-12T10:00:00Z'),
            hungerLevel: CHECK_IN_HUNGER_LEVEL,
            energyLevel: CHECK_IN_ENERGY_LEVEL,
            moodLevel: CHECK_IN_ENERGY_LEVEL,
            symptoms: ['weakness'],
            checkInNotes: 'legacy',
        };

        expect(component['hasCheckIn'](session)).toBe(true);
        expect(component['getSessionCheckInCount'](session)).toBe(1);
        expect(component['getVisibleSessionCheckIns'](session)).toHaveLength(1);
    });
});

async function createComponentAsync(): Promise<void> {
    facade = createFacadeMock();
    toastService = {
        success: vi.fn(),
    };
    dialogService = {
        open: vi.fn((): { afterClosed: () => Observable<undefined> } => ({ afterClosed: () => of(void 0) })),
    };
    tourService = { start: vi.fn() };
    dialogRef = { close: vi.fn() };

    await TestBed.configureTestingModule({
        imports: [FastingPageComponent],
        providers: [
            provideTranslateTesting(),
            {
                provide: FdUiDialogService,
                useValue: dialogService,
            },
            {
                provide: LocalizationService,
                useValue: {
                    getCurrentLanguage: vi.fn(() => 'ru'),
                },
            },
            { provide: FdUiToastService, useValue: toastService },
            { provide: FdTourService, useValue: tourService },
            { provide: FD_UI_DIALOG_DATA, useFactory: (): FastingHistoryDialogData | FastingSessionDetailsDialogData => dialogData },
            { provide: FastingFacade, useValue: facade },
            { provide: FdUiDialogRef, useValue: dialogRef },
        ],
    })
        .overrideComponent(FastingPageComponent, {
            set: {
                providers: [{ provide: FastingFacade, useValue: facade }],
            },
        })
        .compileComponents();

    fixture = TestBed.createComponent(FastingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function pageHost(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function pageButton(label: string, container = pageHost()): HTMLButtonElement {
    const button = Array.from(container.querySelectorAll('button')).find(candidate => candidate.textContent.trim().endsWith(label));
    if (button === undefined) {
        throw new Error(`No fasting page button with label "${label}"`);
    }
    return button;
}

function renderHistoryDialog(): HTMLElement {
    const historyOptions = dialogService.open.mock.calls[0][1] as { data: FastingHistoryDialogData };
    dialogData = historyOptions.data;
    const history = TestBed.createComponent(FastingHistoryDialogComponent);
    history.detectChanges();
    return history.nativeElement as HTMLElement;
}

type FastingFacadeMock = {
    initialize: ReturnType<typeof vi.fn>;
    loadMoreHistory: ReturnType<typeof vi.fn>;
    startFasting: ReturnType<typeof vi.fn>;
    endFasting: ReturnType<typeof vi.fn>;
    selectMode: ReturnType<typeof vi.fn>;
    selectProtocol: ReturnType<typeof vi.fn>;
    setCustomHours: ReturnType<typeof vi.fn>;
    setCustomIntermittentFastHours: ReturnType<typeof vi.fn>;
    setCyclicPreset: ReturnType<typeof vi.fn>;
    selectCustomCyclicPreset: ReturnType<typeof vi.fn>;
    setCyclicFastDays: ReturnType<typeof vi.fn>;
    setCyclicEatDays: ReturnType<typeof vi.fn>;
    selectCyclicEatDayProtocol: ReturnType<typeof vi.fn>;
    setCyclicEatDayFastHours: ReturnType<typeof vi.fn>;
    setExtendHours: ReturnType<typeof vi.fn>;
    setReduceHours: ReturnType<typeof vi.fn>;
    setHungerLevel: ReturnType<typeof vi.fn>;
    setEnergyLevel: ReturnType<typeof vi.fn>;
    setMoodLevel: ReturnType<typeof vi.fn>;
    toggleSymptom: ReturnType<typeof vi.fn>;
    setCheckInNotes: ReturnType<typeof vi.fn>;
    saveCheckIn: ReturnType<typeof vi.fn>;
    resetCheckInDraft: ReturnType<typeof vi.fn>;
    dismissPrompt: ReturnType<typeof vi.fn>;
    snoozePrompt: ReturnType<typeof vi.fn>;
    extendByHours: ReturnType<typeof vi.fn>;
    reduceTargetByHours: ReturnType<typeof vi.fn>;
    skipCyclicDay: ReturnType<typeof vi.fn>;
    postponeCyclicDay: ReturnType<typeof vi.fn>;
    isPromptVisible: ReturnType<typeof vi.fn>;
    isLoading: ReturnType<typeof signal<boolean>>;
    overviewLoadError: ReturnType<typeof signal<boolean>>;
    isStarting: ReturnType<typeof signal<boolean>>;
    isEnding: ReturnType<typeof signal<boolean>>;
    isExtending: ReturnType<typeof signal<boolean>>;
    isReducing: ReturnType<typeof signal<boolean>>;
    isUpdatingCycle: ReturnType<typeof signal<boolean>>;
    isSavingCheckIn: ReturnType<typeof signal<boolean>>;
    requestError: WritableSignal<string | null>;
    maxReducibleHours: WritableSignal<number>;
    currentSession: WritableSignal<FastingSession | null>;
    stats: WritableSignal<FastingStats | null>;
    history: WritableSignal<FastingSession[]>;
    historyPage: ReturnType<typeof signal<number>>;
    historyTotalPages: ReturnType<typeof signal<number>>;
    isLoadingMoreHistory: ReturnType<typeof signal<boolean>>;
    historyError: ReturnType<typeof signal<string | null>>;
    insightsData: WritableSignal<FastingInsights>;
    checkInSavedVersion: ReturnType<typeof signal<number>>;
    selectedMode: ReturnType<typeof signal<'intermittent' | 'extended' | 'cyclic'>>;
    selectedProtocol: WritableSignal<FastingProtocol>;
    customHours: ReturnType<typeof signal<number>>;
    customIntermittentFastHours: ReturnType<typeof signal<number>>;
    cyclicEatDayProtocol: WritableSignal<IntermittentFastingProtocol>;
    cyclicFastDays: ReturnType<typeof signal<number>>;
    cyclicEatDays: ReturnType<typeof signal<number>>;
    cyclicUsesCustomPreset: ReturnType<typeof signal<boolean>>;
    cyclicEatDayFastHours: ReturnType<typeof signal<number>>;
    extendHours: ReturnType<typeof signal<number>>;
    reduceHours: ReturnType<typeof signal<number>>;
    hungerLevel: ReturnType<typeof signal<number>>;
    energyLevel: ReturnType<typeof signal<number>>;
    moodLevel: ReturnType<typeof signal<number>>;
    selectedSymptoms: ReturnType<typeof signal<string[]>>;
    checkInNotes: ReturnType<typeof signal<string>>;
    progressPercent: ReturnType<typeof signal<number>>;
    elapsedFormatted: ReturnType<typeof signal<string>>;
    remainingFormatted: ReturnType<typeof signal<string>>;
    plannedDurationHours: WritableSignal<number>;
    now: WritableSignal<Date>;
    isOvertime: ReturnType<typeof signal<boolean>>;
    isActive: ReturnType<typeof signal<boolean>>;
    canExtendActiveSession: ReturnType<typeof signal<boolean>>;
};

function createFacadeMock(): FastingFacadeMock {
    return {
        ...createFacadeActions(),
        ...createFacadeState(),
    };
}

function createFacadeActions(): Pick<
    FastingFacadeMock,
    | 'initialize'
    | 'loadMoreHistory'
    | 'startFasting'
    | 'endFasting'
    | 'selectMode'
    | 'selectProtocol'
    | 'setCustomHours'
    | 'setCustomIntermittentFastHours'
    | 'setCyclicPreset'
    | 'selectCustomCyclicPreset'
    | 'setCyclicFastDays'
    | 'setCyclicEatDays'
    | 'selectCyclicEatDayProtocol'
    | 'setCyclicEatDayFastHours'
    | 'setExtendHours'
    | 'setReduceHours'
    | 'setHungerLevel'
    | 'setEnergyLevel'
    | 'setMoodLevel'
    | 'toggleSymptom'
    | 'setCheckInNotes'
    | 'saveCheckIn'
    | 'resetCheckInDraft'
    | 'dismissPrompt'
    | 'snoozePrompt'
    | 'extendByHours'
    | 'reduceTargetByHours'
    | 'skipCyclicDay'
    | 'postponeCyclicDay'
    | 'isPromptVisible'
> {
    return {
        initialize: vi.fn(),
        loadMoreHistory: vi.fn(),
        startFasting: vi.fn(),
        endFasting: vi.fn(),
        selectMode: vi.fn(),
        selectProtocol: vi.fn(),
        setCustomHours: vi.fn(),
        setCustomIntermittentFastHours: vi.fn(),
        setCyclicPreset: vi.fn(),
        selectCustomCyclicPreset: vi.fn(),
        setCyclicFastDays: vi.fn(),
        setCyclicEatDays: vi.fn(),
        selectCyclicEatDayProtocol: vi.fn(),
        setCyclicEatDayFastHours: vi.fn(),
        setExtendHours: vi.fn(),
        setReduceHours: vi.fn(),
        setHungerLevel: vi.fn(),
        setEnergyLevel: vi.fn(),
        setMoodLevel: vi.fn(),
        toggleSymptom: vi.fn(),
        setCheckInNotes: vi.fn(),
        saveCheckIn: vi.fn(),
        resetCheckInDraft: vi.fn(),
        dismissPrompt: vi.fn(),
        snoozePrompt: vi.fn(),
        extendByHours: vi.fn(),
        reduceTargetByHours: vi.fn(),
        skipCyclicDay: vi.fn(),
        postponeCyclicDay: vi.fn(),
        isPromptVisible: vi.fn(() => true),
    };
}

function createFacadeState(): Omit<FastingFacadeMock, keyof ReturnType<typeof createFacadeActions>> {
    return {
        isLoading: signal(false),
        overviewLoadError: signal(false),
        isStarting: signal(false),
        isEnding: signal(false),
        isExtending: signal(false),
        isReducing: signal(false),
        isUpdatingCycle: signal(false),
        isSavingCheckIn: signal(false),
        requestError: signal<string | null>(null),
        maxReducibleHours: signal(0),
        currentSession: signal<FastingSession | null>(null),
        stats: signal<FastingStats | null>(null),
        history: signal<FastingSession[]>([]),
        historyPage: signal(1),
        historyTotalPages: signal(1),
        isLoadingMoreHistory: signal(false),
        historyError: signal<string | null>(null),
        insightsData: signal<FastingInsights>({ alerts: [], insights: [] }),
        checkInSavedVersion: signal(0),
        selectedMode: signal<'intermittent' | 'extended' | 'cyclic'>('intermittent'),
        selectedProtocol: signal<FastingProtocol>('Fast16Eat8'),
        customHours: signal(DEFAULT_SESSION_HOURS),
        customIntermittentFastHours: signal(DEFAULT_SESSION_HOURS),
        cyclicEatDayProtocol: signal<IntermittentFastingProtocol>('Fast16Eat8'),
        cyclicFastDays: signal(1),
        cyclicEatDays: signal(1),
        cyclicUsesCustomPreset: signal(false),
        cyclicEatDayFastHours: signal(DEFAULT_SESSION_HOURS),
        extendHours: signal(DEFAULT_EXTEND_HOURS),
        reduceHours: signal(DEFAULT_REDUCE_HOURS),
        hungerLevel: signal(CHECK_IN_MOOD_LEVEL),
        energyLevel: signal(CHECK_IN_MOOD_LEVEL),
        moodLevel: signal(CHECK_IN_MOOD_LEVEL),
        selectedSymptoms: signal<string[]>([]),
        checkInNotes: signal(''),
        progressPercent: signal(0),
        elapsedFormatted: signal('00:00:00'),
        remainingFormatted: signal('00:00:00'),
        plannedDurationHours: signal(DEFAULT_SESSION_HOURS),
        now: signal(new Date('2026-04-12T11:00:00Z')),
        isOvertime: signal(false),
        isActive: signal(false),
        canExtendActiveSession: signal(false),
    };
}

function createSession(): FastingSession {
    return {
        id: entityId<'fasting-session'>('session-1'),
        startedAtUtc: utcInstant('2026-04-12T06:00:00Z'),
        endedAtUtc: null,
        initialPlannedDurationHours: DEFAULT_SESSION_HOURS,
        addedDurationHours: 0,
        plannedDurationHours: DEFAULT_SESSION_HOURS,
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
    };
}

function createHistorySession(id: string, checkInCount: number): FastingSession {
    const checkIns = Array.from({ length: checkInCount }, (_, index) => ({
        id: `${id}-checkin-${index + 1}`,
        checkedInAtUtc: `2026-04-12T${String(HISTORY_START_HOUR + index).padStart(2, '0')}:00:00Z`,
        hungerLevel: CHECK_IN_HUNGER_LEVEL,
        energyLevel: CHECK_IN_ENERGY_LEVEL,
        moodLevel: CHECK_IN_ENERGY_LEVEL,
        symptoms: index === 0 ? ['weakness'] : [],
        notes: index === 0 ? 'steady' : null,
    }));

    return {
        ...createSession(),
        id: entityId<'fasting-session'>(id),
        checkIns,
        ...createLatestCheckInSessionFields(checkIns[0]),
    };
}

function createLatestCheckInSessionFields(checkIn: FastingSession['checkIns'][number] | undefined): Partial<FastingSession> {
    if (checkIn === undefined) {
        return {
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: [],
            checkInNotes: null,
        };
    }

    return {
        checkInAtUtc: utcInstant(checkIn.checkedInAtUtc),
        hungerLevel: checkIn.hungerLevel,
        energyLevel: checkIn.energyLevel,
        moodLevel: checkIn.moodLevel,
        symptoms: checkIn.symptoms,
        checkInNotes: checkIn.notes,
    };
}
