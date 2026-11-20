import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { entityId } from '../../shared/models/semantics/entity-id';
import { dietologistRoutes } from './dietologist.routes';
import { DietologistFacade } from './lib/dietologist.facade';
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
    listClients: ReturnType<typeof vi.fn<DietologistFacade['getMyClients']>>;
    loadDashboard: Mock<DietologistFacade['getClientDashboard']>;
    disconnect: Mock<DietologistFacade['disconnectClient']>;
    toastError: Mock<FdUiToastService['error']>;
    toastInfo: Mock<FdUiToastService['info']>;
} {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const listClients = vi.fn<DietologistFacade['getMyClients']>().mockReturnValue(of([FIRST_CLIENT, SECOND_CLIENT]));
    const loadDashboard = vi.fn<DietologistFacade['getClientDashboard']>();
    const disconnect = vi.fn<DietologistFacade['disconnectClient']>().mockReturnValue(of(void 0));
    const toastError = vi.fn<FdUiToastService['error']>();
    const toastInfo = vi.fn<FdUiToastService['info']>();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([{ path: 'dietologist', children: dietologistRoutes }]),
            {
                provide: DietologistFacade,
                useValue: {
                    getMyClients: listClients,
                    getClientDashboard: loadDashboard,
                    getAttentionSignals: vi.fn(() => of([])),
                    getTasksForClient: vi.fn(() => of([])),
                    getRecommendationsForClient: vi.fn(() => of([])),
                    searchRecommendationTemplates: vi.fn(() => of([])),
                    disconnectClient: disconnect,
                },
            },
            { provide: FdUiToastService, useValue: { info: toastInfo, error: toastError } },
        ],
    });
    return { listClients, loadDashboard, disconnect, toastError, toastInfo };
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
