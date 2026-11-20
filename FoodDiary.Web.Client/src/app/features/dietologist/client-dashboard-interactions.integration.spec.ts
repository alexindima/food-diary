import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { ClientTask, DietologistRecommendation } from '../../shared/models/dietologist.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { DietologistService } from './api/dietologist.service';
import { dietologistRoutes } from './dietologist.routes';
import type { DietologistDashboardSnapshot } from './lib/dietologist-dashboard.data';
import { createClient } from './pages/clients/dietologist-clients-lib/dietologist-clients.test-data';

const CLIENTS_URL = '/dietologist';
const FIRST_DASHBOARD_URL = '/dietologist/clients/alex';
const SECOND_DASHBOARD_URL = '/dietologist/clients/mira';
const CONFIRM_DIALOG = 'fd-ui-confirm-dialog';
const DISCONNECT_ACTION = 'DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT';
const CONFIRM_DISCONNECT_ACTION = 'DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_CONFIRM';
const CLIENTS_HEADING = 'DIETOLOGIST.CLIENTS.HEADING';
const PROFILE_PERMISSIONS = { ...createClient().permissions, shareMeals: false };
const FIRST_CLIENT = createClient({ firstName: 'Alex', userId: entityId<'user'>('alex'), permissions: PROFILE_PERMISSIONS });
const SECOND_CLIENT = createClient({
    firstName: 'Mira',
    userId: entityId<'user'>('mira'),
    gender: 'Female',
    permissions: PROFILE_PERMISSIONS,
});

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

function setupDashboardRouter(): {
    listClients: Mock<DietologistService['getMyClients']>;
    loadDashboard: Mock<DietologistService['getClientDashboard']>;
    disconnect: Mock<DietologistService['disconnectClient']>;
    createTask: Mock<DietologistService['createTask']>;
    loadRecommendations: Mock<DietologistService['getRecommendationsForClient']>;
    createRecommendation: Mock<DietologistService['createRecommendation']>;
    toastError: Mock<FdUiToastService['error']>;
    toastInfo: Mock<FdUiToastService['info']>;
    toastSuccess: Mock<FdUiToastService['success']>;
} {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const listClients = vi.fn<DietologistService['getMyClients']>().mockReturnValue(of([FIRST_CLIENT, SECOND_CLIENT]));
    const loadDashboard = vi.fn<DietologistService['getClientDashboard']>();
    const disconnect = vi.fn<DietologistService['disconnectClient']>().mockReturnValue(of(void 0));
    const createTask = vi.fn<DietologistService['createTask']>();
    const loadRecommendations = vi.fn<DietologistService['getRecommendationsForClient']>().mockReturnValue(of([]));
    const createRecommendation = vi.fn<DietologistService['createRecommendation']>();
    const toastError = vi.fn<FdUiToastService['error']>();
    const toastInfo = vi.fn<FdUiToastService['info']>();
    const toastSuccess = vi.fn<FdUiToastService['success']>();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([{ path: 'dietologist', children: dietologistRoutes }]),
            {
                provide: DietologistService,
                useValue: {
                    getMyClients: listClients,
                    getClientDashboard: loadDashboard,
                    getAttentionSignals: vi.fn(() => of([])),
                    getTasksForClient: vi.fn(() => of([])),
                    createTask,
                    getRecommendationsForClient: loadRecommendations,
                    createRecommendation,
                    searchRecommendationTemplates: vi.fn(() => of([])),
                    disconnectClient: disconnect,
                },
            },
            { provide: FdUiToastService, useValue: { info: toastInfo, error: toastError, success: toastSuccess } },
        ],
    });
    return {
        listClients,
        loadDashboard,
        disconnect,
        createTask,
        loadRecommendations,
        createRecommendation,
        toastError,
        toastInfo,
        toastSuccess,
    };
}

describe('Dietologist dashboard route interactions', () => {
    it('dismisses the original confirmation after client change and away navigation', async () => {
        const { disconnect } = setupDashboardRouter();
        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        openDisconnect(harness);
        expect(document.querySelector(CONFIRM_DIALOG)).not.toBeNull();

        await harness.navigateByUrl(SECOND_DASHBOARD_URL);
        expect(dashboardHost(harness).querySelector('h1')?.textContent).toContain('Mira');
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        expect(disconnect).not.toHaveBeenCalled();

        openDisconnect(harness);
        expect(document.querySelector(CONFIRM_DIALOG)).not.toBeNull();
        await harness.navigateByUrl(CLIENTS_URL);
        expect(dashboardHost(harness).textContent).toContain(CLIENTS_HEADING);
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        expect(disconnect).not.toHaveBeenCalled();

        await harness.navigateByUrl(SECOND_DASHBOARD_URL);
        openDisconnect(harness);
        const confirmation = document.querySelector(CONFIRM_DIALOG);
        if (confirmation === null) {
            throw new Error('Expected a new disconnect confirmation');
        }
        buttonByLabel(confirmation, CONFIRM_DISCONNECT_ACTION).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(TestBed.inject(Router).url).toBe(CLIENTS_URL);
        });
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(SECOND_CLIENT.userId);
    });

    it('updates shared profile labels in place when the application language changes', async () => {
        const { listClients } = setupDashboardRouter();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            USER_MANAGE: { GENDER_OPTIONS: { F: 'Female' }, ACTIVITY_LEVEL_OPTIONS: { MODERATE: 'Moderate' } },
        });
        translate.setTranslation('ru', {
            USER_MANAGE: { GENDER_OPTIONS: { F: 'Женский' }, ACTIVITY_LEVEL_OPTIONS: { MODERATE: 'Умеренный' } },
        });
        await firstValueFrom(translate.use('en'));
        const harness = await RouterTestingHarness.create(SECOND_DASHBOARD_URL);
        const header = dashboardHost(harness).querySelector('fd-client-dashboard-header');
        expect(header?.textContent).toContain('Female');
        expect(header?.textContent).toContain('Moderate');

        await firstValueFrom(translate.use('ru'));
        harness.detectChanges();
        expect(header?.textContent).toContain('Женский');
        expect(header?.textContent).toContain('Умеренный');
        expect(listClients).toHaveBeenCalledOnce();
    });
});

function openDisconnect(harness: RouterTestingHarness): void {
    buttonByLabel(dashboardHost(harness), DISCONNECT_ACTION).click();
    harness.detectChanges();
}

describe('Dietologist dashboard task ownership', () => {
    it('cancels the old task request when switching clients and keeps only the active client’s saved task', async () => {
        const { createTask, toastSuccess } = setupDashboardRouter();
        const firstSave = new Subject<ClientTask>();
        const secondSave = new Subject<ClientTask>();
        createTask.mockReturnValueOnce(firstSave).mockReturnValueOnce(secondSave);
        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        const firstTitle = 'Log breakfast';
        const secondTitle = 'Track water';

        submitDashboardTask(harness, firstTitle);
        expect(createTask).toHaveBeenCalledExactlyOnceWith(FIRST_CLIENT.userId, { title: firstTitle, details: null, dueAtUtc: null });
        expect(firstSave.observed).toBe(true);

        await harness.navigateByUrl(SECOND_DASHBOARD_URL);
        expect(dashboardHost(harness).querySelector('h1')?.textContent).toContain('Mira');
        expect(firstSave.observed).toBe(false);
        submitDashboardTask(harness, secondTitle);
        expect(createTask).toHaveBeenLastCalledWith(SECOND_CLIENT.userId, { title: secondTitle, details: null, dueAtUtc: null });
        expect(secondSave.observed).toBe(true);

        const saved: ClientTask = {
            id: entityId<'client-task'>('saved-task'),
            dietologistUserId: entityId<'user'>('coach'),
            clientUserId: SECOND_CLIENT.userId,
            title: secondTitle,
            details: null,
            dueAtUtc: null,
            status: 'Open',
            isOverdue: false,
            createdAtUtc: utcInstant('2026-05-20T00:00:00Z'),
            statusChangedAtUtc: null,
        };
        firstSave.next({ ...saved, clientUserId: FIRST_CLIENT.userId, title: firstTitle });
        firstSave.complete();
        expect(toastSuccess).not.toHaveBeenCalled();
        secondSave.next(saved);
        secondSave.complete();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(
                dashboardHost(harness).querySelector('[aria-labelledby="dietologist-client-tasks-title"] article')?.textContent,
            ).toContain(secondTitle);
        });
        expect(dashboardHost(harness).textContent).not.toContain(firstTitle);
        expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('CLIENT_TASKS.CREATED');
    });
});

function submitDashboardTask(harness: RouterTestingHarness, title: string): void {
    const host = dashboardHost(harness);
    const form = host.querySelector<HTMLFormElement>('[aria-labelledby="dietologist-client-tasks-title"] form');
    const input = form?.querySelector<HTMLInputElement>('fd-ui-input input') ?? null;
    if (form === null || input === null) {
        throw new Error('Expected the client task form');
    }
    input.value = title;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
    form.requestSubmit();
}

describe('Dietologist dashboard recommendation delivery', () => {
    it('retries the draft and retains the saved recommendation when the initial list arrives late', async () => {
        const { loadRecommendations, createRecommendation, toastError, toastSuccess } = setupDashboardRouter();
        const initialRead = new Subject<DietologistRecommendation[]>();
        const firstSave = new Subject<DietologistRecommendation>();
        const note = 'Check in again next week';
        const saved: DietologistRecommendation = {
            id: entityId<'recommendation'>('new-recommendation'),
            dietologistUserId: entityId<'user'>('coach'),
            dietologistFirstName: null,
            dietologistLastName: null,
            text: note,
            isRead: false,
            createdAtUtc: utcInstant('2026-05-20T00:00:00Z'),
            readAtUtc: null,
        };
        const earlier = {
            ...saved,
            id: entityId<'recommendation'>('earlier-recommendation'),
            createdAtUtc: utcInstant('2026-05-19T00:00:00Z'),
            text: 'Earlier recommendation',
        };
        loadRecommendations.mockReturnValueOnce(initialRead);
        createRecommendation.mockReturnValueOnce(firstSave).mockReturnValueOnce(of(saved));

        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        const host = dashboardHost(harness);
        const form = host.querySelector('.client-dashboard__recommendation-form');
        const draft = form?.querySelector('textarea');
        const send = form?.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (draft === null || draft === undefined || send === null || send === undefined) {
            throw new Error('Expected the client recommendation form');
        }
        expect(initialRead.observed).toBe(true);

        enterRecommendation(harness, draft, '   ');
        send.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(draft.value).toBe('');
        });
        expect(createRecommendation).not.toHaveBeenCalled();
        await harness.fixture.whenStable();

        enterRecommendation(harness, draft, `  ${note}  `);
        send.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(createRecommendation).toHaveBeenCalledOnce();
            expect(firstSave.observed).toBe(true);
            expect(send.disabled).toBe(true);
        });
        expect(draft.disabled).toBe(true);
        send.click();
        expect(createRecommendation).toHaveBeenCalledExactlyOnceWith(FIRST_CLIENT.userId, { text: note });

        firstSave.error(new Error('Recommendation delivery unavailable'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(send.disabled).toBe(false);
        });
        expect(draft.value).toBe(`  ${note}  `);
        expect(toastError).toHaveBeenCalledExactlyOnceWith('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SEND_ERROR');
        await harness.fixture.whenStable();

        send.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(draft.value).toBe('');
            expect(recommendationTexts(host)).toEqual([note]);
        });
        expect(createRecommendation.mock.calls).toEqual([
            [FIRST_CLIENT.userId, { text: note }],
            [FIRST_CLIENT.userId, { text: note }],
        ]);

        initialRead.next([saved, earlier]);
        initialRead.complete();
        harness.detectChanges();
        expect(recommendationTexts(host)).toEqual([note, earlier.text]);
        expect(toastSuccess).toHaveBeenCalledExactlyOnceWith('DIETOLOGIST.CLIENT_DASHBOARD.RECOMMENDATIONS.SENT');
    });
});

function enterRecommendation(harness: RouterTestingHarness, draft: HTMLTextAreaElement, value: string): void {
    draft.value = value;
    draft.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
}

function recommendationTexts(host: HTMLElement): string[] {
    return Array.from(host.querySelectorAll('.client-dashboard__recommendation > p'), item => item.textContent.trim());
}

describe('Dietologist dashboard period retry', () => {
    it.each(['initial period', 'selected period'] as const)('retries the exact failed %s from the rendered date form', async failure => {
        const { listClients, loadDashboard } = setupDashboardRouter();
        const initialCalories = 140;
        const recoveredCalories = 260;
        const unavailable = new Subject<DietologistDashboardSnapshot>();
        const recovery = new Subject<DietologistDashboardSnapshot>();
        const firstAttempt = failure === 'initial period';
        listClients.mockReturnValueOnce(of([{ ...FIRST_CLIENT, permissions: { ...PROFILE_PERMISSIONS, shareStatistics: true } }]));
        if (!firstAttempt) {
            loadDashboard.mockReturnValueOnce(of(dashboardSnapshot(initialCalories)));
        }
        loadDashboard.mockReturnValueOnce(unavailable).mockReturnValueOnce(recovery);

        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        const host = dashboardHost(harness);
        if (!firstAttempt) {
            expect(statisticsText(host)).toContain(String(initialCalories));
            buttonByLabel(host, 'DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.LAST_14').click();
            harness.detectChanges();
        }
        const attemptsBeforeRetry = firstAttempt ? 1 : 2;
        expect(loadDashboard).toHaveBeenCalledTimes(attemptsBeforeRetry);
        expect(unavailable.observed).toBe(true);
        const failedPeriod = loadDashboard.mock.calls.at(-1);
        unavailable.error(new Error('Shared dashboard unavailable'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(host.querySelector('fd-client-dashboard-notices')?.textContent).toContain(
                'DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR',
            );
        });

        const apply = buttonByLabel(host, 'DIETOLOGIST.CLIENT_DASHBOARD.DATE_FILTER.APPLY');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(apply.disabled).toBe(false);
        });
        apply.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(loadDashboard).toHaveBeenCalledTimes(attemptsBeforeRetry + 1);
        });
        expect(loadDashboard.mock.calls.at(-1)).toEqual(failedPeriod);
        expect(recovery.observed).toBe(true);
        expect(apply.disabled).toBe(true);
        apply.click();
        expect(loadDashboard).toHaveBeenCalledTimes(attemptsBeforeRetry + 1);

        recovery.next(dashboardSnapshot(recoveredCalories));
        recovery.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(statisticsText(host)).toContain(String(recoveredCalories));
        });
        expect(host.querySelector('fd-client-dashboard-notices')?.textContent).not.toContain(
            'DIETOLOGIST.CLIENT_DASHBOARD.PARTIAL_LOAD_ERROR',
        );
        apply.click();
        harness.detectChanges();
        expect(loadDashboard).toHaveBeenCalledTimes(attemptsBeforeRetry + 1);
        expect(listClients).toHaveBeenCalledOnce();
    });
});

function dashboardSnapshot(totalCalories: number): DietologistDashboardSnapshot {
    return {
        date: '2026-05-19',
        dateTo: '2026-05-26',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: { totalCalories, averageProteins: 0, averageFats: 0, averageCarbs: 0, averageFiber: 0 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        meals: { items: [], total: 0 },
    };
}

function statisticsText(host: HTMLElement): string {
    return host.querySelector('fd-client-dashboard-metric-list')?.textContent ?? '';
}

function buttonByLabel(host: ParentNode, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(item => item.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected dashboard button ${label}`);
    }
    return button;
}

function dashboardHost(harness: RouterTestingHarness): HTMLElement {
    if (harness.routeNativeElement === null) {
        throw new Error('Expected the Dietologist route outlet to be populated');
    }
    return harness.routeNativeElement;
}

describe('Dietologist dashboard disconnect lifecycle', () => {
    it('opens one confirmation for rapid repeat activation and can be opened later after canceling', async () => {
        const { disconnect } = setupDashboardRouter();
        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        const button = buttonByLabel(dashboardHost(harness), DISCONNECT_ACTION);
        button.click();
        button.click();
        harness.detectChanges();

        expect(document.querySelectorAll(CONFIRM_DIALOG)).toHaveLength(1);
        buttonByLabel(disconnectDialog(), 'COMMON.CANCEL').click();
        harness.detectChanges();
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        expect(disconnect).not.toHaveBeenCalled();

        openDisconnect(harness);
        expect(document.querySelectorAll(CONFIRM_DIALOG)).toHaveLength(1);
    });

    it('opens the client from the actual directory and locks disconnect through a failed request until retry', async () => {
        const { listClients, disconnect, toastError, toastInfo } = setupDashboardRouter();
        const pendingDisconnect = new Subject<void>();
        disconnect.mockReturnValueOnce(pendingDisconnect);
        const harness = await RouterTestingHarness.create(CLIENTS_URL);
        const router = TestBed.inject(Router);
        const card = dashboardHost(harness).querySelector<HTMLElement>('fd-ui-card[aria-label="Alex Ivanov"]');
        if (card === null) {
            throw new Error('Expected the first client in the actual directory');
        }
        card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(FIRST_DASHBOARD_URL);
            expect(dashboardHost(harness).querySelector('h1')?.textContent).toContain('Alex');
        });

        openDisconnect(harness);
        buttonByLabel(disconnectDialog(), CONFIRM_DISCONNECT_ACTION).click();
        harness.detectChanges();
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(FIRST_CLIENT.userId);
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();
        const action = buttonByLabel(dashboardHost(harness), DISCONNECT_ACTION);
        expect(action.disabled).toBe(true);
        action.click();
        harness.detectChanges();
        expect(document.querySelector(CONFIRM_DIALOG)).toBeNull();

        pendingDisconnect.error(new Error('Disconnect unavailable'));
        harness.detectChanges();
        expect(router.url).toBe(FIRST_DASHBOARD_URL);
        expect(toastError).toHaveBeenCalledExactlyOnceWith('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECT_ERROR');
        expect(toastInfo).not.toHaveBeenCalled();
        expect(action.disabled).toBe(false);

        listClients.mockReturnValue(of([SECOND_CLIENT]));
        openDisconnect(harness);
        buttonByLabel(disconnectDialog(), CONFIRM_DISCONNECT_ACTION).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(CLIENTS_URL);
            expect(dashboardHost(harness).querySelector('h1')?.textContent).toContain(CLIENTS_HEADING);
        });
        expect(dashboardHost(harness).querySelector('fd-ui-card[aria-label="Alex Ivanov"]')).toBeNull();
        expect(dashboardHost(harness).querySelector('fd-ui-card[aria-label="Mira Ivanov"]')).not.toBeNull();
        expect(disconnect.mock.calls).toEqual([[FIRST_CLIENT.userId], [FIRST_CLIENT.userId]]);
        expect(toastInfo).toHaveBeenCalledExactlyOnceWith('DIETOLOGIST.CLIENT_DASHBOARD.DISCONNECTED');
    });
});

describe('Dietologist dashboard disconnect ownership', () => {
    it('cancels the first client request on route change without leaving the new dashboard or locking its action', async () => {
        const { disconnect, toastInfo, toastError } = setupDashboardRouter();
        const pendingDisconnect = new Subject<void>();
        disconnect.mockReturnValueOnce(pendingDisconnect);
        const harness = await RouterTestingHarness.create(FIRST_DASHBOARD_URL);
        openDisconnect(harness);
        buttonByLabel(disconnectDialog(), CONFIRM_DISCONNECT_ACTION).click();
        expect(pendingDisconnect.observed).toBe(true);
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(FIRST_CLIENT.userId);

        await harness.navigateByUrl(SECOND_DASHBOARD_URL);
        expect(pendingDisconnect.observed).toBe(false);
        pendingDisconnect.next(undefined);
        pendingDisconnect.complete();
        harness.detectChanges();
        expect(TestBed.inject(Router).url).toBe(SECOND_DASHBOARD_URL);
        expect(dashboardHost(harness).querySelector('h1')?.textContent).toContain('Mira');
        expect(toastInfo).not.toHaveBeenCalled();
        expect(toastError).not.toHaveBeenCalled();

        expect(buttonByLabel(dashboardHost(harness), DISCONNECT_ACTION).disabled).toBe(false);
        openDisconnect(harness);
        expect(document.querySelectorAll(CONFIRM_DIALOG)).toHaveLength(1);
    });
});

function disconnectDialog(): HTMLElement {
    const dialog = document.querySelector<HTMLElement>(CONFIRM_DIALOG);
    if (dialog === null) {
        throw new Error('Expected the client disconnect confirmation');
    }
    return dialog;
}
