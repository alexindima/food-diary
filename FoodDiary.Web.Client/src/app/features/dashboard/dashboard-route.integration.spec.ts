import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeRu from '@angular/common/locales/ru';
import { ChangeDetectionStrategy, Component, DestroyRef, type Provider, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, type Routes, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { FdUiToastHostComponent } from 'fd-ui-kit/toast/fd-ui-toast-host';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { AiInputBarResult } from '../../components/shared/ai-input-bar/ai-input-bar.types';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import type { HydrationEntryHttpResponse } from '../../shared/api/sdk/generated/model/hydration-entry-http-response';
import type { MealHttpResponse } from '../../shared/api/sdk/generated/model/meal-http-response';
import { UserService } from '../../shared/api/user.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../shared/development/development-tools';
import { LocalizationService } from '../../shared/i18n/localization.service';
import type { DashboardSnapshot } from '../../shared/models/dashboard.data';
import type { GoalsResponse } from '../../shared/models/goals.data';
import type { Meal } from '../../shared/models/meal.data';
import { calendarDate, utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { DashboardLayoutSettings, User } from '../../shared/models/user.data';
import { BrowserNotificationCapabilityService } from '../../shared/notifications/browser-notification-capability.service';
import { NotificationService } from '../../shared/notifications/notification.service';
import { PushNotificationService } from '../../shared/notifications/push-notification.service';
import { ViewportService } from '../../shared/platform/viewport.service';
import { PrivacyConsentFacade } from '../../shared/privacy/privacy-consent.facade';
import { CALORIE_GOAL_ACTIONS, type CalorieGoalActions } from '../goals/contracts/calorie-goal-actions';
import { HydrationService } from '../hydration/api/hydration.service';
import { HYDRATION_ACTIONS } from '../hydration/contracts/hydration-actions';
import { AiMealCreateFacade } from '../meals/contracts/ai-meal-create';
import { FAVORITE_MEAL_ACTIONS, MEAL_ACTIONS } from '../meals/contracts/meal-actions';
import { DashboardService } from './api/dashboard.service';
import dashboardRoutes from './dashboard.routes';
import { DashboardComponent } from './pages/dashboard';
import { DashboardQuickAddComponent } from './pages/dashboard-sections/dashboard-quick-add/dashboard-quick-add';

@Component({ template: '<h1>Another page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherPageComponent {}

registerLocaleData(localeRu);

const DASHBOARD_TEST_ROUTES: Routes = [
    { path: 'dashboard', children: dashboardRoutes },
    { path: 'other', component: OtherPageComponent },
    { path: 'fasting', component: OtherPageComponent },
    { path: 'goals', component: OtherPageComponent },
    { path: 'cycle-tracking', component: OtherPageComponent },
    { path: 'meals/add', component: OtherPageComponent },
    { path: 'weight-history', component: OtherPageComponent },
];
const DASHBOARD_DATE = '2026-05-18';
const DASHBOARD_URL = `/dashboard?date=${DASHBOARD_DATE}`;
const OTHER_URL = '/other';
const BASE_LAYOUT: DashboardLayoutSettings = {
    web: ['summary', 'meals', 'fasting', 'hydration'],
    mobile: ['summary', 'meals', 'hydration'],
};
const HIDDEN_HYDRATION: DashboardLayoutSettings = { web: ['summary', 'meals', 'fasting'], mobile: BASE_LAYOUT.mobile };
const HYDRATION_TILE = '[data-tour-id="dashboard-hydration"]';
const HYDRATION_DETAILS_DIALOG = 'fd-hydration-details-dialog';
const EDIT_ACTION = 'DASHBOARD.ACTIONS.OPEN_SETTINGS';
const DIALOG_SELECTOR = 'fd-unsaved-changes-dialog';
const NUTRITION_DIALOG_SELECTOR = 'fd-nutrition-details-dialog';

type DashboardFixture = {
    harness: RouterTestingHarness;
    router: Router;
    updateLayout: Mock<UserService['updateDashboardLayout']>;
    updateGoal: Mock<CalorieGoalActions['updateGoals']>;
    readSnapshot: Mock<DashboardService['getSnapshot']>;
    refreshSnapshot: Mock<DashboardService['getSnapshotSilentlyStrict']>;
    readAccount: Mock<UserService['getInfoSilently']>;
    createAiMeal: Mock<AiMealCreateFacade['createFromAiResult']>;
    developmentScreens: Map<string, DevelopmentScreen>;
    dialogs: FdUiDialogService;
};

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

type DashboardFixtureOptions = {
    startUrl?: string;
    initialRead?: Observable<DashboardSnapshot | null>;
    realMealComposer?: boolean;
    realHydration?: boolean;
};

async function renderDashboardAsync(
    initialSnapshot: DashboardSnapshot | null = makeSnapshot(),
    cachedAccount: User | null = null,
    options: DashboardFixtureOptions = {},
): Promise<DashboardFixture> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const updateLayout = vi.fn<UserService['updateDashboardLayout']>();
    const updateGoal = vi.fn<CalorieGoalActions['updateGoals']>().mockReturnValue(of(null));
    const readSnapshot = vi.fn<DashboardService['getSnapshot']>().mockReturnValue(options.initialRead ?? of(initialSnapshot));
    const refreshSnapshot = vi.fn<DashboardService['getSnapshotSilentlyStrict']>().mockReturnValue(of(initialSnapshot ?? makeSnapshot()));
    const readAccount = vi.fn<UserService['getInfoSilently']>().mockReturnValue(of(null));
    const createAiMeal = vi.fn<AiMealCreateFacade['createFromAiResult']>().mockReturnValue(of(null));
    const developmentScreens = new Map<string, DevelopmentScreen>();
    const testingModule = TestBed.configureTestingModule({
        imports: [DashboardComponent],
        providers: [
            provideRouter(DASHBOARD_TEST_ROUTES, withComponentInputBinding()),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            ...dashboardBoundaryProviders(options.realMealComposer),
            ...(options.realHydration === true ? [{ provide: HYDRATION_ACTIONS, useExisting: HydrationService }] : []),
            ...notificationBoundaryProviders(),
            { provide: CALORIE_GOAL_ACTIONS, useValue: { updateGoals: updateGoal } },
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
            {
                provide: DashboardService,
                useValue: { getSnapshot: readSnapshot, getSnapshotSilentlyStrict: refreshSnapshot },
            },
            {
                provide: UserService,
                useValue: {
                    user: signal(cachedAccount),
                    updateDashboardLayout: updateLayout,
                    getInfoSilently: readAccount,
                },
            },
        ],
    });
    if (options.realMealComposer === true) {
        testingModule.overrideProvider(PrivacyConsentFacade, {
            useValue: { ensureHealthConsentAsync: vi.fn<PrivacyConsentFacade['ensureHealthConsentAsync']>().mockResolvedValue(true) },
        });
    } else {
        testingModule.overrideProvider(AiMealCreateFacade, {
            useValue: { isSaving: signal(false), clearToken: signal(0), createFromAiResult: createAiMeal },
        });
    }
    const translate = TestBed.inject(TranslateService);
    translate.setFallbackLang('en');
    translate.use('en');
    const harness = await RouterTestingHarness.create(options.startUrl ?? DASHBOARD_URL);
    return {
        harness,
        router: TestBed.inject(Router),
        updateLayout,
        updateGoal,
        readSnapshot,
        refreshSnapshot,
        readAccount,
        createAiMeal,
        developmentScreens,
        dialogs: TestBed.inject(FdUiDialogService),
    };
}

function dashboardBoundaryProviders(isPremium = false): Provider[] {
    return [
        { provide: MEAL_ACTIONS, useValue: {} },
        { provide: FAVORITE_MEAL_ACTIONS, useValue: {} },
        { provide: HYDRATION_ACTIONS, useValue: {} },
        { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(isPremium), getUserId: (): null => null } },
        { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
        { provide: ViewportService, useValue: { isMobile: signal(false) } },
    ];
}

function notificationBoundaryProviders(): Provider[] {
    return [
        {
            provide: NotificationService,
            useValue: {
                getNotificationPreferences: vi.fn<NotificationService['getNotificationPreferences']>().mockReturnValue(
                    of({
                        pushNotificationsEnabled: false,
                        fastingPushNotificationsEnabled: true,
                        socialPushNotificationsEnabled: true,
                        fastingCheckInReminderHours: 12,
                        fastingCheckInFollowUpReminderHours: 20,
                    }),
                ),
            },
        },
        { provide: BrowserNotificationCapabilityService, useValue: { getPermission: (): NotificationPermission => 'default' } },
        { provide: PushNotificationService, useValue: { isSupported: signal(false), isSubscribed: signal(false), isBusy: signal(false) } },
        { provide: FrontendObservabilityService, useValue: { recordNotificationSettingsViewed: vi.fn() } },
    ];
}

function makeSnapshot(): DashboardSnapshot {
    return {
        date: DASHBOARD_DATE,
        dateTo: DASHBOARD_DATE,
        dailyGoal: 2100,
        weeklyCalorieGoal: 14700,
        statistics: { totalCalories: 1200, averageProteins: 80, averageFats: 40, averageCarbs: 100, averageFiber: 10 },
        meals: { items: [], total: 0 },
        hydration: { dateUtc: calendarDate(DASHBOARD_DATE), totalMl: 500, goalMl: 2000 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        weightTrend: [],
        waistTrend: [],
        advice: null,
        dashboardLayout: BASE_LAYOUT,
    };
}

function dashboardScreen(developmentScreens: Map<string, DevelopmentScreen>): DevelopmentScreen & {
    setFiltersAsync: NonNullable<DevelopmentScreen['setFiltersAsync']>;
} {
    const screen = developmentScreens.get('dashboard');
    if (screen?.setFiltersAsync === undefined) {
        throw new Error('Expected the dashboard to register its date filter');
    }
    return { ...screen, setFiltersAsync: screen.setFiltersAsync };
}

describe('Dashboard once a first weight measurement has been recorded', () => {
    it('shows the first measurement and opens its add-entry route without a trend chart', async () => {
        const snapshot = makeSnapshot();
        const date = calendarDate(DASHBOARD_DATE);
        const firstWeightKg = 76.5;
        const { harness, router } = await renderDashboardAsync({
            ...snapshot,
            dashboardLayout: {
                web: ['summary', 'weight'],
                mobile: ['summary', 'weight'],
            },
            weight: { ...snapshot.weight, latest: { date, weightKg: firstWeightKg } },
            weightTrend: [{ startDate: date, endDate: date, averageWeightKg: firstWeightKg }],
        });
        const card = element(harness, 'fd-weight-trend-card.dashboard__card--weight-trend');
        expect(card.querySelector('.weight-trend-card__value')?.textContent.trim()).toBe(String(firstWeightKg));
        expect(card.querySelector('.weight-trend-card__sparse')?.textContent).toContain('WEIGHT_TREND_CARD.FIRST_MEASUREMENT');
        expect(card.querySelector('fd-ui-line-chart')).toBeNull();
        expect(card.querySelector('.weight-trend-card__delta')).toBeNull();

        const addMeasurement = element(harness, 'fd-weight-trend-card.dashboard__card--weight-trend .weight-trend-card__action button');
        expect(addMeasurement.textContent).toContain('WEIGHT_TREND_CARD.ADD_WEIGHT');
        addMeasurement.click();
        await vi.waitFor(() => {
            expect(router.url).toBe('/weight-history?addEntry=true');
        });
    });
});

function useDashboardDay(): void {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(`${DASHBOARD_DATE}T12:00:00`));
    onTestFinished(() => {
        vi.useRealTimers();
    });
}

function savedUser(): User {
    return {
        id: entityId<'user'>('layout-user'),
        email: 'layout@example.com',
        isActive: true,
        isEmailConfirmed: true,
        hasPassword: true,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: true,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
        dashboardLayout: HIDDEN_HYDRATION,
    };
}

function element(harness: RouterTestingHarness, selector: string): HTMLElement {
    const found = harness.routeNativeElement?.querySelector<HTMLElement>(selector);
    if (found === null || found === undefined) {
        throw new Error(`Expected Dashboard element ${selector}`);
    }
    return found;
}

function button(root: ParentNode, label: string): HTMLButtonElement {
    const found = Array.from(root.querySelectorAll('button')).find(
        candidate => candidate.getAttribute('aria-label') === label || candidate.textContent.trim() === label,
    );
    if (found === undefined) {
        throw new Error(`Expected Dashboard action ${label}`);
    }
    return found;
}

function redraw(harness: RouterTestingHarness): void {
    TestBed.tick();
    harness.detectChanges();
}

function hideHydration(harness: RouterTestingHarness): void {
    button(element(harness, '.dashboard'), EDIT_ACTION).click();
    redraw(harness);
    element(harness, HYDRATION_TILE).dispatchEvent(new KeyboardEvent('keydown', { key: ' ', cancelable: true, bubbles: true }));
    redraw(harness);
}

async function chooseDepartureAsync(harness: RouterTestingHarness, label: string): Promise<void> {
    await vi.waitFor(() => {
        redraw(harness);
        expect(document.querySelector(DIALOG_SELECTOR)).not.toBeNull();
    });
    const dialog = document.querySelector(DIALOG_SELECTOR);
    if (dialog === null) {
        throw new Error('Expected the route guard dialog');
    }
    button(dialog, label).click();
    redraw(harness);
}

describe('Dashboard native layout editing', () => {
    it('retains a keyboard-edited layout through a failed save and retries while keeping the normal cards locked', async () => {
        const { harness, updateLayout } = await renderDashboardAsync();
        const pending = new Subject<User | null>();
        updateLayout.mockReturnValueOnce(pending).mockReturnValueOnce(of(savedUser()));
        const dashboard = element(harness, '.dashboard');
        expect(element(harness, HYDRATION_TILE).querySelector('fd-hydration-card')).not.toBeNull();

        hideHydration(harness);
        const edit = button(dashboard, EDIT_ACTION);
        expect(edit.getAttribute('aria-pressed')).toBe('true');
        expect(element(harness, HYDRATION_TILE).getAttribute('aria-pressed')).toBe('false');
        expect(element(harness, HYDRATION_TILE).querySelector('[inert]')).not.toBeNull();
        button(dashboard, 'DASHBOARD.SETTINGS.SAVE').click();
        redraw(harness);
        expect(updateLayout).toHaveBeenCalledExactlyOnceWith(HIDDEN_HYDRATION);
        expect(edit.disabled).toBe(true);
        expect(element(harness, HYDRATION_TILE).getAttribute('aria-disabled')).toBe('true');
        pending.error(new Error('Layout unavailable'));
        await vi.waitFor(() => {
            redraw(harness);
            expect(button(dashboard, 'DASHBOARD.SETTINGS.SAVE').disabled).toBe(false);
        });

        expect(dashboard.textContent).toContain('DASHBOARD.SETTINGS.SAVE_ERROR');
        expect(element(harness, HYDRATION_TILE).getAttribute('aria-pressed')).toBe('false');
        button(dashboard, 'DASHBOARD.SETTINGS.SAVE').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(updateLayout).toHaveBeenCalledTimes(2);
            expect(button(dashboard, EDIT_ACTION).getAttribute('aria-pressed')).toBe('false');
        });
        expect(updateLayout.mock.calls.map(call => call[0])).toEqual([HIDDEN_HYDRATION, HIDDEN_HYDRATION]);
        expect(dashboard.querySelector(HYDRATION_TILE)).toBeNull();
        expect(dashboard.textContent).not.toContain('DASHBOARD.SETTINGS.SAVE_ERROR');
    });

    it('finishes an unchanged layout without writing and restores the historical dashboard controls', async () => {
        const { harness, updateLayout } = await renderDashboardAsync();
        const dashboard = element(harness, '.dashboard');
        const edit = button(dashboard, EDIT_ACTION);
        edit.click();
        redraw(harness);
        expect(edit.getAttribute('aria-pressed')).toBe('true');
        expect(dashboard.querySelector('.dashboard__block--fasting[role="button"]')).not.toBeNull();
        edit.click();
        redraw(harness);

        expect(updateLayout).not.toHaveBeenCalled();
        expect(edit.getAttribute('aria-pressed')).toBe('false');
        expect(dashboard.querySelector('.dashboard__block--fasting')).toBeNull();
        expect(element(harness, HYDRATION_TILE).querySelector('[inert]')).toBeNull();
    });
});

it('changes fasting card visibility by keyboard in edit mode and opens fasting outside edit mode', async () => {
    const durationHours = 16;
    const snapshot: DashboardSnapshot = {
        ...makeSnapshot(),
        currentFastingSession: {
            id: entityId<'fasting-session'>('dashboard-active-fast'),
            startedAtUtc: utcInstant(new Date().toISOString()),
            endedAtUtc: null,
            initialPlannedDurationHours: durationHours,
            addedDurationHours: 0,
            plannedDurationHours: durationHours,
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
        },
    };
    const { harness, router, updateLayout } = await renderDashboardAsync(
        snapshot,
        { ...savedUser(), dashboardLayout: { ...BASE_LAYOUT, mobile: BASE_LAYOUT.web } },
        { startUrl: '/dashboard' },
    );
    const fasting = (): HTMLElement => element(harness, '.dashboard__block--fasting[role="button"]');
    const dashboard = element(harness, '.dashboard');
    const edit = button(dashboard, EDIT_ACTION);
    expect(fasting().getAttribute('aria-label')).toBe('FASTING.TITLE');

    edit.click();
    redraw(harness);
    expect(fasting().getAttribute('aria-pressed')).toBe('true');
    fasting().focus();
    expect(document.activeElement).toBe(fasting());
    fasting().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true, bubbles: true }));
    redraw(harness);
    expect(fasting().getAttribute('aria-pressed')).toBe('false');
    expect(router.url).toBe('/dashboard');

    fasting().dispatchEvent(new KeyboardEvent('keydown', { key: ' ', cancelable: true, bubbles: true }));
    redraw(harness);
    expect(fasting().getAttribute('aria-pressed')).toBe('true');
    edit.click();
    redraw(harness);
    expect(updateLayout).not.toHaveBeenCalled();
    expect(fasting().getAttribute('aria-pressed')).toBeNull();

    fasting().focus();
    expect(document.activeElement).toBe(fasting());
    fasting().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true, bubbles: true }));
    await vi.waitFor(() => {
        expect(router.url).toBe('/fasting');
    });
});

describe('Dashboard layout initialization', () => {
    it('keeps the first customized layout visible when the save response omits the saved layout', async () => {
        const { harness, updateLayout } = await renderDashboardAsync({ ...makeSnapshot(), dashboardLayout: null });
        updateLayout.mockReturnValueOnce(of({ ...savedUser(), dashboardLayout: null }));
        const dashboard = element(harness, '.dashboard');
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).not.toBeNull();
        expect(dashboard.querySelector(HYDRATION_TILE)).not.toBeNull();
        expect(dashboard.querySelector('[data-tour-id="dashboard-summary"]')).not.toBeNull();

        const edit = button(dashboard, EDIT_ACTION);
        edit.click();
        redraw(harness);
        const nutrition = element(harness, '[role="button"][aria-label="NUTRITION_TREND.TITLE"]');
        expect(nutrition.getAttribute('aria-pressed')).toBe('true');
        nutrition.click();
        redraw(harness);
        expect(nutrition.getAttribute('aria-pressed')).toBe('false');

        button(dashboard, 'DASHBOARD.SETTINGS.SAVE').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(edit.getAttribute('aria-pressed')).toBe('false');
        });

        expect(updateLayout).toHaveBeenCalledOnce();
        const savedLayout = updateLayout.mock.lastCall?.[0];
        expect(savedLayout?.web).toEqual(expect.arrayContaining(['summary', 'meals', 'hydration', 'nutrition-hidden']));
        expect(savedLayout?.mobile).toEqual(expect.arrayContaining(['summary', 'meals', 'hydration']));
        expect(savedLayout?.mobile).not.toContain('nutrition-hidden');
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).toBeNull();
        expect(dashboard.querySelector(HYDRATION_TILE)).not.toBeNull();
    });

    it('keeps editing clean when the saved layout arrives after the settings button was opened', async () => {
        const pending = new Subject<DashboardSnapshot>();
        const { harness, updateLayout } = await renderDashboardAsync(makeSnapshot(), null, { initialRead: pending });
        const dashboard = element(harness, '.dashboard');
        const edit = button(dashboard, EDIT_ACTION);
        edit.click();
        redraw(harness);
        expect(edit.getAttribute('aria-pressed')).toBe('true');

        pending.next(makeSnapshot());
        pending.complete();
        redraw(harness);
        expect(element(harness, HYDRATION_TILE).getAttribute('aria-pressed')).toBe('true');
        expect(dashboard.textContent).not.toContain('DASHBOARD.SETTINGS.SAVE');

        edit.click();
        redraw(harness);
        expect(edit.getAttribute('aria-pressed')).toBe('false');
        expect(updateLayout).not.toHaveBeenCalled();
        expect(element(harness, HYDRATION_TILE).querySelector('fd-hydration-card')).not.toBeNull();
    });

    it.each(['empty', 'error'] as const)('restores saved widget visibility after an initial %s response and Retry', async failure => {
        const failedRead = failure === 'empty' ? of(null) : throwError(() => new Error('Dashboard temporarily unavailable'));
        const snapshot = { ...makeSnapshot(), dashboardLayout: HIDDEN_HYDRATION };
        const { harness, readSnapshot, updateLayout } = await renderDashboardAsync(snapshot, null, { initialRead: failedRead });
        const dashboard = element(harness, '.dashboard');
        expect(dashboard.textContent).toContain('DASHBOARD.LOAD_ERROR');

        readSnapshot.mockReturnValueOnce(of(snapshot));
        button(dashboard, 'ERRORS.RETRY').click();
        redraw(harness);
        expect(dashboard.textContent).not.toContain('DASHBOARD.LOAD_ERROR');
        expect(dashboard.querySelector('[data-tour-id="dashboard-summary"]')).not.toBeNull();
        expect(dashboard.querySelector(HYDRATION_TILE)).toBeNull();
        expect(readSnapshot).toHaveBeenCalledTimes(2);
        expect(updateLayout).not.toHaveBeenCalled();
    });
});

describe('Dashboard setup and layout write ownership', () => {
    it('waits for setup dismissal before editing and keeps dismissal unavailable during layout save', async () => {
        useDashboardDay();
        const account: User = {
            ...savedUser(),
            birthDate: calendarDate('1990-03-04'),
            gender: 'M',
            dashboardLayout: BASE_LAYOUT,
        };
        const { harness, updateLayout } = await renderDashboardAsync(makeSnapshot(), account);
        const dismissRequest = new Subject<User | null>();
        const layoutRequest = new Subject<User | null>();
        updateLayout.mockReturnValueOnce(dismissRequest).mockReturnValueOnce(layoutRequest);
        const dashboard = element(harness, '.dashboard');
        const setupCard = element(harness, 'fd-dashboard-setup-card');
        const dismiss = button(setupCard, 'DASHBOARD.INITIAL_SETUP.DISMISS');
        const edit = button(dashboard, EDIT_ACTION);
        expect(setupCard.closest('[hidden]')).toBeNull();
        expect(button(setupCard, 'DASHBOARD.INITIAL_SETUP.PROFILE').disabled).toBe(false);

        dismiss.click();
        redraw(harness);
        expect(updateLayout).toHaveBeenCalledExactlyOnceWith({ ...BASE_LAYOUT, setupHintDismissed: true });
        edit.click();
        redraw(harness);

        expect(edit.getAttribute('aria-pressed')).toBe('false');
        expect(edit.disabled).toBe(true);
        expect(dismiss.disabled).toBe(true);
        expect(setupCard.closest('[hidden]')).toBeNull();
        expect(updateLayout).toHaveBeenCalledOnce();

        dismissRequest.next(null);
        dismissRequest.complete();
        await vi.waitFor(() => {
            redraw(harness);
            expect(setupCard.querySelector('[role="alert"]')?.textContent).toContain('DASHBOARD.INITIAL_SETUP.SAVE_ERROR');
        });
        expect(edit.disabled).toBe(false);
        expect(dismiss.disabled).toBe(false);

        hideHydration(harness);
        expect(setupCard.closest('[hidden]')).not.toBeNull();
        button(dashboard, 'DASHBOARD.SETTINGS.SAVE').click();
        redraw(harness);
        expect(setupCard.closest('[hidden]')).not.toBeNull();
        expect(updateLayout).toHaveBeenCalledTimes(2);
        expect(updateLayout).toHaveBeenLastCalledWith(HIDDEN_HYDRATION);

        layoutRequest.next({ ...account, dashboardLayout: HIDDEN_HYDRATION });
        layoutRequest.complete();
        await vi.waitFor(() => {
            redraw(harness);
            expect(setupCard.closest('[hidden]')).toBeNull();
            expect(edit.disabled).toBe(false);
        });
    });
});

describe('Dashboard layout route guard', () => {
    it('keeps a dirty draft when staying and restores persisted visibility after discarding and returning', async () => {
        const { harness, updateLayout, router } = await renderDashboardAsync();
        hideHydration(harness);
        const attempted = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.STAY');
        expect(await attempted).toBe(false);
        expect(router.url).toBe(DASHBOARD_URL);
        expect(element(harness, HYDRATION_TILE).getAttribute('aria-pressed')).toBe('false');

        const discarded = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.DISCARD');
        expect(await discarded).toBe(true);
        expect(updateLayout).not.toHaveBeenCalled();
        expect(router.url).toBe(OTHER_URL);
        await harness.navigateByUrl(DASHBOARD_URL);
        expect(element(harness, HYDRATION_TILE).querySelector('fd-hydration-card')).not.toBeNull();
        expect(button(element(harness, '.dashboard'), EDIT_ACTION).getAttribute('aria-pressed')).toBe('false');
    });

    it('waits for persistence before leaving and allows another attempt if it fails', async () => {
        const { harness, updateLayout, router } = await renderDashboardAsync();
        const first = new Subject<User | null>();
        const retry = new Subject<User | null>();
        updateLayout.mockReturnValueOnce(first).mockReturnValueOnce(retry);
        hideHydration(harness);
        const attempted = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.SAVE');
        expect(first.observed).toBe(true);
        expect(router.url).toBe(DASHBOARD_URL);
        first.error(new Error('Layout unavailable'));
        expect(await attempted).toBe(false);
        redraw(harness);
        expect(element(harness, '.dashboard').textContent).toContain('DASHBOARD.SETTINGS.SAVE_ERROR');

        const saved = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.SAVE');
        expect(retry.observed).toBe(true);
        expect(router.url).toBe(DASHBOARD_URL);
        retry.next(savedUser());
        expect(await saved).toBe(true);
        expect(router.url).toBe(OTHER_URL);
        expect(updateLayout.mock.calls.map(call => call[0])).toEqual([HIDDEN_HYDRATION, HIDDEN_HYDRATION]);
    });
});

describe('Dashboard snapshot recovery', () => {
    it('retries the current date after an empty response and mounts its cards', async () => {
        const { harness, readSnapshot } = await renderDashboardAsync(null);
        const dashboard = element(harness, '.dashboard');
        expect(dashboard.textContent).toContain('DASHBOARD.LOAD_ERROR');
        expect(dashboard.querySelector(HYDRATION_TILE)).toBeNull();
        readSnapshot.mockReturnValueOnce(of(makeSnapshot()));
        button(dashboard, 'ERRORS.RETRY').click();
        redraw(harness);

        expect(dashboard.querySelector(HYDRATION_TILE)).not.toBeNull();
        expect(dashboard.textContent).not.toContain('DASHBOARD.LOAD_ERROR');
        expect(readSnapshot).toHaveBeenCalledTimes(2);
        expect(readSnapshot.mock.calls[0][0].date).toEqual(readSnapshot.mock.calls[1][0].date);
    });
});

describe('Dashboard hydration undo during snapshot retry', () => {
    it('undoes the saved water entry while retrying its dashboard snapshot', async () => {
        useDashboardDay();
        const initial = makeSnapshot();
        const { harness, readSnapshot, refreshSnapshot } = await renderDashboardAsync(initial, null, { realHydration: true });
        const toast = TestBed.createComponent(FdUiToastHostComponent);
        const toastRoot = toast.nativeElement as HTMLElement;
        const http = TestBed.inject(HttpTestingController);
        onTestFinished(() => {
            http.verify();
        });
        const amountMl = 250;
        const entryId = entityId<'hydration-entry'>('00000000-0000-4000-8000-000000000002');
        const timestampUtc = utcInstant(new Date().toISOString());
        const hydrationUrl = environment.apiUrls.hydration;
        const receipt = { id: entryId, timestampUtc, amountMl } satisfies HydrationEntryHttpResponse;
        refreshSnapshot.mockReturnValueOnce(throwError(() => new Error('Dashboard snapshot unavailable')));
        button(element(harness, HYDRATION_TILE), `+${amountMl} GENERAL.UNITS.ML`).click();
        const adding = http.expectOne({ method: 'POST', url: hydrationUrl });
        expect(adding.request.body).toEqual({ amountMl, timestampUtc });
        adding.flush(receipt);
        redraw(harness);
        toast.detectChanges();

        const retry = new Subject<DashboardSnapshot>();
        readSnapshot.mockReturnValueOnce(retry);
        const retryButton = button(element(harness, '.dashboard'), 'ERRORS.RETRY');
        expect(retryButton.disabled).toBe(false);
        retryButton.click();
        redraw(harness);
        expect(retry.observed).toBe(true);
        expect(readSnapshot.mock.lastCall?.[0].date).toEqual(readSnapshot.mock.calls[0][0].date);
        expect(element(harness, '.dashboard__refresh-overlay').getAttribute('role')).toBe('status');
        const undo = button(toastRoot, 'HYDRATION_CARD.UNDO');
        expect(undo.disabled).toBe(false);
        undo.click();
        toast.detectChanges();
        expect(undo.disabled).toBe(true);

        const deleteRequest = { method: 'DELETE', url: `${hydrationUrl}/${entryId}` };
        const deletion = http.expectOne(deleteRequest);
        undo.click();
        http.expectNone(deleteRequest);

        const restored = new Subject<DashboardSnapshot>();
        refreshSnapshot.mockReturnValueOnce(restored);
        deletion.flush(null, { status: 204, statusText: 'No Content' });
        redraw(harness);
        expect(restored.observed).toBe(true);
        expect(refreshSnapshot).toHaveBeenCalledTimes(2);
        expect(refreshSnapshot.mock.lastCall?.[0].date).toEqual(readSnapshot.mock.calls[0][0].date);
        restored.next(initial);
        restored.complete();
        redraw(harness);
        expect(harness.routeNativeElement?.querySelector('.dashboard__refresh-overlay')).toBeNull();

        expect(retry.observed).toBe(true);
        const staleTotal = 750;
        retry.next({ ...initial, hydration: { dateUtc: calendarDate(DASHBOARD_DATE), totalMl: staleTotal, goalMl: 2000 } });
        retry.complete();
        redraw(harness);
        toast.detectChanges();
        expect(element(harness, `${HYDRATION_TILE} [role="progressbar"]`).getAttribute('aria-valuenow')).toBe('500');
        http.expectNone(deleteRequest);
        expect(toastRoot.querySelector('.fd-ui-toast-host__item:not(.is-leaving) .fd-ui-toast-host__action')).toBeNull();
    });
});

describe('Dashboard date filter and route state', () => {
    it('publishes loading and applied period independently and rejects an invalid calendar date', async () => {
        const pending = new Subject<DashboardSnapshot>();
        const { harness, developmentScreens, readSnapshot, router } = await renderDashboardAsync(makeSnapshot(), null, {
            initialRead: pending,
        });
        const screen = dashboardScreen(developmentScreens);
        expect(screen.read()).toMatchObject({ date: DASHBOARD_DATE, loading: true, error: null, appliedPeriod: null });
        expect(element(harness, '.dashboard__loader').textContent).toContain('DASHBOARD.LOADING');

        await expect(screen.setFiltersAsync({ date: '2026-02-30' })).rejects.toThrow('Expected a valid calendar date');
        expect(router.url).toBe(DASHBOARD_URL);
        expect(readSnapshot).toHaveBeenCalledTimes(1);
        pending.next(makeSnapshot());
        pending.complete();
        redraw(harness);

        expect(screen.read()).toMatchObject({
            date: DASHBOARD_DATE,
            loading: false,
            error: null,
            appliedPeriod: { dateFrom: DASHBOARD_DATE, dateTo: DASHBOARD_DATE },
            calories: 1200,
        });
        expect(element(harness, HYDRATION_TILE).querySelector('fd-hydration-card')).not.toBeNull();
        expect(screen.isDirty?.()).toBe(false);
        hideHydration(harness);
        expect(screen.isDirty?.()).toBe(true);
    });

    it('accepts the already explicit date without restarting or changing the current route', async () => {
        const url = `${DASHBOARD_URL}&campaign=diary`;
        const { developmentScreens, readSnapshot, router } = await renderDashboardAsync(makeSnapshot(), null, { startUrl: url });
        const screen = dashboardScreen(developmentScreens);

        await expect(screen.setFiltersAsync({ date: DASHBOARD_DATE })).resolves.toBeUndefined();
        expect(router.url).toBe(url);
        expect(readSnapshot).toHaveBeenCalledTimes(1);
    });

    it('normalizes duplicate date values to a single explicit selection', async () => {
        const { developmentScreens, readSnapshot, router } = await renderDashboardAsync(makeSnapshot(), null, {
            startUrl: `${DASHBOARD_URL}&date=2026-05-12&campaign=diary`,
        });
        const screen = dashboardScreen(developmentScreens);

        await screen.setFiltersAsync({ date: DASHBOARD_DATE });

        expect(router.parseUrl(router.url).queryParams).toEqual({ date: DASHBOARD_DATE, campaign: 'diary' });
        expect(readSnapshot).toHaveBeenCalledTimes(1);
    });

    it('pins the current day when the route follows today and no date is explicit', async () => {
        useDashboardDay();
        const { developmentScreens, readSnapshot, router } = await renderDashboardAsync(makeSnapshot(), null, {
            startUrl: '/dashboard?campaign=diary',
        });
        const screen = dashboardScreen(developmentScreens);

        await screen.setFiltersAsync({ date: DASHBOARD_DATE });

        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'diary', date: DASHBOARD_DATE });
        expect(readSnapshot).toHaveBeenCalledTimes(1);
    });
});

describe('Dashboard date selection recovery and localization', () => {
    it('rejects a pending registered date change when a different route supersedes it', async () => {
        const { harness, developmentScreens, readSnapshot, router } = await renderDashboardAsync();
        const screen = dashboardScreen(developmentScreens);
        const change = screen.setFiltersAsync({ date: '2026-05-12' });
        const leave = harness.navigateByUrl(OTHER_URL);
        await expect(change).rejects.toThrow('Dashboard date navigation was cancelled.');
        await leave;

        expect(router.url).toBe(OTHER_URL);
        expect(developmentScreens.has('dashboard')).toBe(false);
        expect(readSnapshot).toHaveBeenCalledOnce();
    });

    it('retries the newly requested historical date and preserves the unrelated route query', async () => {
        const day = '2026-05-12';
        const { harness, developmentScreens, readSnapshot, router } = await renderDashboardAsync(makeSnapshot(), null, {
            startUrl: `${DASHBOARD_URL}&campaign=diary`,
        });
        const screen = dashboardScreen(developmentScreens);
        readSnapshot.mockReturnValueOnce(throwError(() => new Error('Dashboard request unavailable')));
        await screen.setFiltersAsync({ date: day });
        redraw(harness);

        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'diary', date: day });
        expect(readSnapshot.mock.lastCall?.[0].date.toISOString()).toBe(`${day}T00:00:00.000Z`);
        expect(screen.read()).toMatchObject({ date: day, loading: false, error: 'DASHBOARD.LOAD_ERROR', appliedPeriod: null });
        const alert = element(harness, '.dashboard__body > [role="alert"]');
        expect(alert.hidden).toBe(false);
        expect(alert.textContent).toContain('DASHBOARD.LOAD_ERROR');

        readSnapshot.mockReturnValueOnce(of({ ...makeSnapshot(), date: day, dateTo: day }));
        button(alert, 'ERRORS.RETRY').click();
        redraw(harness);

        expect(readSnapshot.mock.calls.map(([query]) => query.date.toISOString())).toEqual([
            `${DASHBOARD_DATE}T00:00:00.000Z`,
            `${day}T00:00:00.000Z`,
            `${day}T00:00:00.000Z`,
        ]);
        expect(alert.hidden).toBe(true);
        expect(screen.read()).toMatchObject({ date: day, error: null, appliedPeriod: { dateFrom: day, dateTo: day } });
    });

    it('updates the date button label and silently reloads the selected date when the language changes', async () => {
        const { harness, readSnapshot, refreshSnapshot, router } = await renderDashboardAsync();
        const translate = TestBed.inject(TranslateService);
        const dateButtonSelector = '.dashboard__date-button button';
        expect(element(harness, dateButtonSelector).getAttribute('aria-label')).toBe('18 May 2026');

        translate.use('ru');
        redraw(harness);

        expect(element(harness, dateButtonSelector).getAttribute('aria-label')).toBe('18 мая 2026');
        expect(router.url).toBe(DASHBOARD_URL);
        expect(readSnapshot).toHaveBeenCalledTimes(1);
        expect(refreshSnapshot).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ date: new Date(`${DASHBOARD_DATE}T00:00:00.000Z`), locale: 'ru' }),
            expect.anything(),
        );
    });
});

describe('Dashboard recognition quick add', () => {
    it('keeps the snapshot while writing and refreshes after a meal is actually created', async () => {
        useDashboardDay();
        const { harness, developmentScreens, createAiMeal, readSnapshot, refreshSnapshot } = await renderDashboardAsync();
        const screen = dashboardScreen(developmentScreens);
        const pending = new Subject<Meal | null>();
        const quickAdd = harness.fixture.debugElement.query(By.directive(DashboardQuickAddComponent))
            .componentInstance as DashboardQuickAddComponent;
        const result: AiInputBarResult = {
            source: 'Text',
            recognizedAtUtc: `${DASHBOARD_DATE}T12:00:00.000Z`,
            items: [{ nameEn: 'Soup', amount: 250, unit: 'g', calories: 300, proteins: 14, fats: 8, carbs: 35, fiber: 5, alcohol: 0 }],
        };
        const created: Meal = {
            id: entityId<'meal'>('soup'),
            date: utcInstant(result.recognizedAtUtc),
            mealType: 'Lunch',
            totalCalories: 300,
            totalProteins: 14,
            totalFats: 8,
            totalCarbs: 35,
            totalFiber: 5,
            totalAlcohol: 0,
            isNutritionAutoCalculated: true,
            items: [],
            aiSessions: [],
        };
        quickAdd.mealCreateRequested.emit(result);
        expect(createAiMeal).toHaveBeenCalledExactlyOnceWith(result);
        expect(refreshSnapshot).not.toHaveBeenCalled();

        createAiMeal.mockReturnValueOnce(pending);
        quickAdd.mealCreateRequested.emit(result);
        expect(pending.observed).toBe(true);
        expect(screen.read()).toMatchObject({ calories: 1200, loading: false });
        expect(refreshSnapshot).not.toHaveBeenCalled();

        const snapshot = makeSnapshot();
        refreshSnapshot.mockReturnValueOnce(of({ ...snapshot, statistics: { ...snapshot.statistics, totalCalories: 1500 } }));
        pending.next(created);
        pending.complete();
        redraw(harness);

        expect(readSnapshot).toHaveBeenCalledTimes(1);
        expect(refreshSnapshot).toHaveBeenCalledTimes(1);
        expect(screen.read()).toMatchObject({ calories: 1500, loading: false });
    });
});

const DASHBOARD_AI_MEAL_NAME = 'Lentil soup';
const DASHBOARD_AI_MEAL_AMOUNT = 200;
const DASHBOARD_AI_MEAL_NUTRIENTS = { calories: 160, protein: 8, fat: 4, carbs: 24, fiber: 6, alcohol: 0 };

describe('Dashboard independent meal recognition drafts', () => {
    it.each(['dinner', 'quick add'] as const)('clears only the submitting composer after %s saves a meal', async source => {
        useDashboardDay();
        const { harness } = await renderDashboardAsync(
            makeSnapshot(),
            { ...savedUser(), dashboardLayout: BASE_LAYOUT, aiConsentAcceptedAt: utcInstant(`${DASHBOARD_DATE}T11:00:00Z`) },
            { realMealComposer: true },
        );
        const http = TestBed.inject(HttpTestingController);
        onTestFinished(() => {
            http.verify();
        });
        const quickAdd = element(harness, 'fd-dashboard-quick-add');
        const dinner = dashboardMealSlot(harness, 'DINNER');
        button(dinner, 'MEALS_PREVIEW.ADD_TO_MEAL').click();
        redraw(harness);

        const [submitting, other] = source === 'dinner' ? [dinner, quickAdd] : [quickAdd, dinner];
        const otherDraft = 'A separate unsent meal description';
        typeComposerText(other, otherDraft);

        const review = await reviewDashboardAiMealAsync(harness, http, submitting);
        button(review, 'MEAL_LIST.VOICE_CREATE_MEAL').click();
        const write = http.expectOne(request => request.method === 'POST' && request.url.endsWith('/meals'));
        expect(write.request.body).toMatchObject({ aiSessions: [{ source: 'Text' }] });
        expect(composerInput(other).value).toBe(otherDraft);
        write.flush(dashboardAiMealResponse());
        redraw(harness);

        expect(composerInput(other).value).toBe(otherDraft);
        expect(composerInput(submitting).value).toBe('');
        expect(submitting.querySelector('fd-ai-photo-result')).toBeNull();
        http.expectNone(request => request.method === 'POST' && request.url.endsWith('/meals'));
    });
});

it('keeps Breakfast open when an earlier Dinner save clears its own draft', async () => {
    useDashboardDay();
    const { harness } = await renderDashboardAsync(
        makeSnapshot(),
        { ...savedUser(), dashboardLayout: BASE_LAYOUT, aiConsentAcceptedAt: utcInstant(`${DASHBOARD_DATE}T11:00:00Z`) },
        { realMealComposer: true },
    );
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
    });
    const breakfast = dashboardMealSlot(harness, 'BREAKFAST');
    const dinner = dashboardMealSlot(harness, 'DINNER');
    const breakfastButton = button(breakfast, 'MEALS_PREVIEW.ADD_TO_MEAL');
    const dinnerButton = button(dinner, 'MEALS_PREVIEW.ADD_TO_MEAL');
    const breakfastDraft = 'An unsent breakfast description';
    breakfastButton.click();
    redraw(harness);
    typeComposerText(breakfast, breakfastDraft);
    const breakfastInput = composerInput(breakfast);
    dinnerButton.click();
    redraw(harness);
    expect(breakfastButton.getAttribute('aria-expanded')).toBe('false');
    const review = await reviewDashboardAiMealAsync(harness, http, dinner);

    button(review, 'MEAL_LIST.VOICE_CREATE_MEAL').click();
    const write = http.expectOne(request => request.method === 'POST' && request.url.endsWith('/meals'));
    expect(write.request.body).toMatchObject({ mealType: 'DINNER', aiSessions: [{ source: 'Text' }] });
    expect(breakfastButton.disabled).toBe(false);
    breakfastButton.click();
    redraw(harness);
    expect(breakfastButton.getAttribute('aria-expanded')).toBe('true');
    expect(breakfastButton.disabled).toBe(true);
    expect(dinnerButton.getAttribute('aria-expanded')).toBe('false');

    write.flush(dashboardAiMealResponse());
    redraw(harness);

    expect(breakfastButton.getAttribute('aria-expanded')).toBe('true');
    expect(breakfastButton.disabled).toBe(false);
    expect(composerInput(breakfast)).toBe(breakfastInput);
    expect(breakfastInput.value).toBe(breakfastDraft);
    expect(composerInput(dinner).value).toBe('');
    expect(dinner.querySelector('fd-ai-photo-result')).toBeNull();
    http.expectNone(request => request.method === 'POST' && request.url.endsWith('/meals'));
});

function dashboardMealSlot(harness: RouterTestingHarness, slot: 'BREAKFAST' | 'DINNER'): Element {
    const entries = element(harness, 'fd-meals-preview').querySelectorAll('fd-meals-preview-entry');
    const entry = Array.from(entries).find(item => item.textContent.includes(`MEAL_CARD.MEAL_TYPES.${slot}`));
    if (entry === undefined) {
        throw new Error(`Expected the empty ${slot} slot`);
    }
    return entry;
}

async function reviewDashboardAiMealAsync(harness: RouterTestingHarness, http: HttpTestingController, composer: Element): Promise<Element> {
    const name = DASHBOARD_AI_MEAL_NAME;
    const amount = DASHBOARD_AI_MEAL_AMOUNT;
    const nutrients = DASHBOARD_AI_MEAL_NUTRIENTS;
    typeComposerText(composer, name);
    redraw(harness);
    button(composer, 'AI_INPUT_BAR.ADD_ACTION').click();
    const parsing = await vi.waitFor(() => http.expectOne(request => request.url.endsWith('/ai/food/text')));
    expect(parsing.request.body).toEqual({ text: name });
    parsing.flush({ items: [{ nameEn: name, amount, unit: 'g', confidence: 1 }] });
    http.expectOne(request => request.url.endsWith('/ai/food/nutrition')).flush({
        ...nutrients,
        items: [{ name, amount, unit: 'g', ...nutrients }],
    });
    redraw(harness);
    const review = composer.querySelector('fd-ai-photo-result');
    if (review === null) {
        throw new Error('Expected the submitting composer to show its meal review');
    }
    expect(review.textContent).toContain(name);
    return review;
}

function dashboardAiMealResponse(): MealHttpResponse {
    const nutrients = DASHBOARD_AI_MEAL_NUTRIENTS;
    return {
        id: '32de1050-da71-4ebb-9e46-7344b563b407',
        date: `${DASHBOARD_DATE}T12:00:00Z`,
        totalCalories: nutrients.calories,
        totalProteins: nutrients.protein,
        totalFats: nutrients.fat,
        totalCarbs: nutrients.carbs,
        totalFiber: nutrients.fiber,
        totalAlcohol: nutrients.alcohol,
        isNutritionAutoCalculated: true,
        items: [],
    };
}

function composerInput(composer: ParentNode): HTMLInputElement {
    const input = composer.querySelector('input.ai-input-bar__input');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the rendered meal description input');
    }
    return input;
}

function typeComposerText(composer: ParentNode, text: string): void {
    const input = composerInput(composer);
    input.value = text;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

describe('Dashboard lazy toolbar dialogs', () => {
    it.each([
        ['DASHBOARD.ACTIONS.OPEN_APPEARANCE', 'fd-appearance-dialog'],
        ['DASHBOARD.ACTIONS.OPEN_NOTIFICATION_SETTINGS', 'fd-dashboard-notification-settings-dialog'],
    ])('renders the dialog for %s while the dashboard still owns the action', async (action, selector) => {
        const { harness, dialogs } = await renderDashboardAsync();
        button(element(harness, '.dashboard'), action).click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(document.querySelector(selector)).not.toBeNull();
        });
        dialogs.closeAll();
        expect(document.querySelector(selector)).toBeNull();
    });

    it.each([
        [
            'DASHBOARD.ACTIONS.OPEN_APPEARANCE',
            async (): Promise<unknown> => import('../../components/shared/appearance-dialog/appearance-dialog'),
        ],
        [
            'DASHBOARD.ACTIONS.OPEN_NOTIFICATION_SETTINGS',
            async (): Promise<unknown> => import('./dialogs/dashboard-notification-settings-dialog/dashboard-notification-settings-dialog'),
        ],
    ] as const)('ignores %s when its owner closes before the imported dialog is ready', async (action, loadDialog) => {
        const { harness, dialogs } = await renderDashboardAsync();
        const open = vi.spyOn(dialogs, 'open');
        button(element(harness, '.dashboard'), action).click();
        harness.fixture.destroy();
        await loadDialog();

        expect(open).not.toHaveBeenCalled();
    });
});

describe('Dashboard repeated toolbar activation', () => {
    it.each([
        ['DASHBOARD.ACTIONS.OPEN_APPEARANCE', 'fd-appearance-dialog'],
        ['DASHBOARD.ACTIONS.OPEN_NOTIFICATION_SETTINGS', 'fd-dashboard-notification-settings-dialog'],
    ] as const)('opens one dialog for repeated %s clicks and allows reopening after close', async (action, selector) => {
        const { harness } = await renderDashboardAsync();
        const trigger = button(element(harness, '.dashboard'), action);
        trigger.click();
        trigger.click();
        await vi.dynamicImportSettled();
        redraw(harness);

        const opened = document.querySelectorAll<HTMLElement>(selector);
        expect(opened).toHaveLength(1);
        button(opened[0], 'COMMON.CLOSE').click();
        redraw(harness);
        expect(document.querySelector(selector)).toBeNull();

        trigger.click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(document.querySelectorAll(selector)).toHaveLength(1);
        });
    });
});

describe('Dashboard nutrition detail from the day summary', () => {
    it('leaves nutrition details unopened when the dashboard is destroyed during the lazy import', async () => {
        const { harness, dialogs, refreshSnapshot } = await renderDashboardAsync();
        const open = vi.spyOn(dialogs, 'open');
        const calories = element(harness, '[aria-label="DASHBOARD.NUTRITION_DETAILS.OPEN_CALORIES"]');

        calories.click();
        harness.fixture.destroy();
        await import('./dialogs/nutrition-details-dialog/nutrition-details-dialog');

        expect(open).not.toHaveBeenCalled();
        expect(refreshSnapshot).not.toHaveBeenCalled();
        expect(document.querySelector(NUTRITION_DIALOG_SELECTOR)).toBeNull();
    });

    it('retries the selected historical day and closes back to the same dashboard', async () => {
        const { harness, router, readSnapshot, refreshSnapshot } = await renderDashboardAsync();
        const dashboard = element(harness, '.dashboard');
        const calories = element(harness, '[aria-label="DASHBOARD.NUTRITION_DETAILS.OPEN_CALORIES"]');
        const firstRead = new Subject<DashboardSnapshot>();
        refreshSnapshot.mockReturnValueOnce(firstRead);
        expect(calories.getAttribute('role')).toBe('button');
        calories.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        const dialog = await findNutritionDialogAsync(harness);
        expect(dialog.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.LOADING');
        expect(refreshSnapshot).toHaveBeenCalledTimes(1);
        const originalRequest = refreshSnapshot.mock.lastCall?.[0];

        firstRead.error(new Error('Failed to load the full day'));
        await harness.fixture.whenStable();
        redraw(harness);
        expect(dialog.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.ERROR_TITLE');
        expect(dialog.querySelector('.nutrition-details__summary')).toBeNull();

        const empty = makeSnapshot();
        empty.statistics.totalCalories = 0;
        refreshSnapshot.mockReturnValueOnce(of(empty));
        button(dialog, 'DASHBOARD.NUTRITION_DETAILS.RETRY').click();
        await harness.fixture.whenStable();
        redraw(harness);
        expect(dialog.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.EMPTY');
        expect(dialog.querySelector('.nutrition-details__summary')).not.toBeNull();
        expect(refreshSnapshot).toHaveBeenLastCalledWith(originalRequest);
        expect(originalRequest?.date).toEqual(readSnapshot.mock.calls[0][0].date);

        button(dialog, 'COMMON.CLOSE').click();
        redraw(harness);
        expect(document.querySelector(NUTRITION_DIALOG_SELECTOR)).toBeNull();
        expect(router.url).toBe(DASHBOARD_URL);
        expect(element(harness, '.dashboard')).toBe(dashboard);
    });
});

describe('Dashboard hydration details', () => {
    it('removes the open hydration details when leaving Dashboard', async () => {
        const { harness } = await renderDashboardAsync();

        button(element(harness, HYDRATION_TILE), 'HYDRATION_CARD.OPEN_DETAILS').click();
        redraw(harness);
        expect(document.querySelector(HYDRATION_DETAILS_DIALOG)?.textContent).toContain('HYDRATION_CARD.RECORDED');

        await harness.navigateByUrl(OTHER_URL, OtherPageComponent);
        redraw(harness);
        expect(harness.routeNativeElement?.textContent).toContain('Another page');
        expect(document.querySelector(HYDRATION_DETAILS_DIALOG)).toBeNull();
    });

    it('opens goal settings from the rendered hydration history', async () => {
        const timestampUtc = utcInstant('2026-05-18T08:15:00Z');
        const snapshot: DashboardSnapshot = {
            ...makeSnapshot(),
            hydrationHistory: {
                entries: [{ id: entityId<'hydration-entry'>('00000000-0000-4000-8000-000000000001'), timestampUtc, amountMl: 175 }],
                hasMore: false,
            },
        };
        const { harness, router } = await renderDashboardAsync(snapshot);

        button(element(harness, HYDRATION_TILE), 'HYDRATION_CARD.OPEN_DETAILS').click();
        redraw(harness);
        const dialog = document.querySelector(HYDRATION_DETAILS_DIALOG);
        if (dialog === null) {
            throw new Error('Expected hydration details');
        }
        expect(dialog.querySelector('dd')?.textContent).toContain('500');
        expect(dialog.querySelector('time')?.getAttribute('datetime')).toBe(timestampUtc);
        expect(dialog.querySelector('.hydration-history__amount')?.textContent).toContain('175');

        button(dialog, 'HYDRATION_CARD.EDIT_GOAL').click();
        await harness.fixture.whenStable();
        expect(router.url).toBe('/goals');
        expect(document.querySelector(HYDRATION_DETAILS_DIALOG)).toBeNull();
    });
});

describe('Dashboard hydration details localization', () => {
    it('updates the recorded amounts in the same open history dialog when the app language changes', async () => {
        const timestampUtc = utcInstant('2026-05-18T08:15:00Z');
        const totalMl = 1500;
        const amountMl = 1250;
        const snapshot: DashboardSnapshot = {
            ...makeSnapshot(),
            hydration: { dateUtc: calendarDate(DASHBOARD_DATE), totalMl, goalMl: 2000 },
            hydrationHistory: {
                entries: [{ id: entityId<'hydration-entry'>('00000000-0000-4000-8000-000000000001'), timestampUtc, amountMl }],
                hasMore: false,
            },
        };
        const { harness, readSnapshot } = await renderDashboardAsync(snapshot);
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { HYDRATION_CARD: { RECORDED: 'Recorded' } }, true);
        translate.setTranslation('ru', { HYDRATION_CARD: { RECORDED: 'Выпито' } }, true);
        await firstValueFrom(translate.use('en'));

        button(element(harness, HYDRATION_TILE), 'HYDRATION_CARD.OPEN_DETAILS').click();
        redraw(harness);
        const dialog = document.querySelector(HYDRATION_DETAILS_DIALOG);
        if (dialog === null) {
            throw new Error('Expected hydration details');
        }
        const english = readHydrationDetails(dialog);
        expect(english.recorded).toContain('Recorded');
        expect(english.total).toContain('1,500');
        expect(english.amount).toContain('1,250');
        expect(english.timestamp).toBe(timestampUtc);

        await firstValueFrom(translate.use('ru'));
        redraw(harness);

        expect(document.querySelector(HYDRATION_DETAILS_DIALOG)).toBe(dialog);
        const russian = readHydrationDetails(dialog);
        expect(russian.recorded).toContain('Выпито');
        expect(russian.total).toContain('1\u00A0500');
        expect(russian.amount).toContain('1\u00A0250');
        expect(russian.timestamp).toBe(timestampUtc);
        expect(readSnapshot).toHaveBeenCalledOnce();
    });
});

function readHydrationDetails(dialog: Element): {
    recorded: string | undefined;
    total: string | undefined;
    amount: string | undefined;
    timestamp: string | null | undefined;
} {
    return {
        recorded: dialog.querySelector('dt')?.textContent,
        total: dialog.querySelector('dd')?.textContent,
        amount: dialog.querySelector('.hydration-history__amount')?.textContent,
        timestamp: dialog.querySelector('time')?.getAttribute('datetime'),
    };
}

describe('Dashboard TDEE goal request ownership', () => {
    const target = 1950;
    const apply = 'TDEE_CARD.APPLY';
    const dialogSelector = 'fd-tdee-insight-dialog';

    it('cancels the card request on route departure without showing its failure on the next page', async () => {
        const { harness, updateGoal, readSnapshot, refreshSnapshot } = await renderDashboardAsync(dashboardSnapshotWithTdee(target));
        const toastError = vi.spyOn(TestBed.inject(FdUiToastService), 'error');
        const saving = new Subject<GoalsResponse | null>();
        updateGoal.mockReturnValueOnce(saving);
        const card = element(harness, 'fd-dashboard-tdee-block');
        const readsBefore = readSnapshot.mock.calls.length;
        button(card, apply).click();
        redraw(harness);
        expect(updateGoal).toHaveBeenCalledExactlyOnceWith({ dailyCalorieTarget: target });
        expect(saving.observed).toBe(true);
        expect(button(card, apply).disabled).toBe(true);

        await harness.navigateByUrl(OTHER_URL, OtherPageComponent);
        await harness.fixture.whenStable();
        await new Promise<void>(resolve => {
            setTimeout(resolve, 0);
        });
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Another page');
        expect(saving.observed).toBe(false);
        expect(readSnapshot).toHaveBeenCalledTimes(readsBefore);
        expect(refreshSnapshot).not.toHaveBeenCalled();
        expect(toastError).not.toHaveBeenCalled();
    });

    it('completes the card and details Apply through a single pending goal update', async () => {
        const { harness, updateGoal, refreshSnapshot } = await renderDashboardAsync(dashboardSnapshotWithTdee(target));
        const saving = new Subject<GoalsResponse | null>();
        updateGoal.mockReturnValueOnce(saving);
        const card = element(harness, 'fd-dashboard-tdee-block');
        button(card, apply).click();
        redraw(harness);
        expect(saving.observed).toBe(true);

        button(card, 'TDEE_CARD.TITLE').click();
        await vi.dynamicImportSettled();
        await harness.fixture.whenStable();
        redraw(harness);
        const dialog = document.querySelector(dialogSelector);
        if (dialog === null) {
            throw new Error('Expected to open the TDEE details dialog while the card saves');
        }
        const dialogApply = [...dialog.querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button')].find(control =>
            control.textContent.trim().endsWith(apply),
        );
        if (dialogApply === undefined) {
            throw new Error('Expected the TDEE suggestion action in the dialog footer');
        }
        dialogApply.click();
        redraw(harness);
        expect(dialogApply.disabled).toBe(true);
        expect(updateGoal).toHaveBeenCalledExactlyOnceWith({ dailyCalorieTarget: target });

        saving.next({ dailyCalorieTarget: target, calorieCyclingEnabled: false });
        saving.complete();
        await vi.waitFor(() => {
            redraw(harness);
            expect(document.querySelector(dialogSelector)).toBeNull();
        });
        expect(refreshSnapshot).toHaveBeenCalledOnce();
        expect(updateGoal).toHaveBeenCalledOnce();
    });
});

it('does not open TDEE details after Dashboard destruction during its lazy import', async () => {
    const target = 1950;
    const { harness, dialogs, updateGoal, refreshSnapshot } = await renderDashboardAsync(dashboardSnapshotWithTdee(target));
    const open = vi.spyOn(dialogs, 'open');
    const routeOwner = harness.routeDebugElement?.injector.get(DestroyRef);
    const card = element(harness, 'fd-dashboard-tdee-block');
    expect(routeOwner?.destroyed).toBe(false);

    button(card, 'TDEE_CARD.TITLE').click();
    harness.fixture.destroy();
    expect(routeOwner?.destroyed).toBe(true);
    await import('./dialogs/tdee-insight-dialog/tdee-insight-dialog');
    await vi.dynamicImportSettled();

    expect(open).not.toHaveBeenCalled();
    expect(document.querySelector('fd-tdee-insight-dialog')).toBeNull();
    expect(updateGoal).not.toHaveBeenCalled();
    expect(refreshSnapshot).not.toHaveBeenCalled();
});

it('closes the open TDEE dialog without writing a calorie goal already saved from the card', async () => {
    const target = 1950;
    const apply = 'TDEE_CARD.APPLY';
    const dialogSelector = 'fd-tdee-insight-dialog';
    const snapshot = dashboardSnapshotWithTdee(target);
    const insight = snapshot.tdeeInsight;
    if (insight === null || insight === undefined) {
        throw new Error('Expected the initial TDEE recommendation');
    }
    const { harness, updateGoal, refreshSnapshot } = await renderDashboardAsync(snapshot);
    const saving = new Subject<GoalsResponse | null>();
    updateGoal.mockReturnValueOnce(saving);
    refreshSnapshot.mockReturnValueOnce(of({ ...snapshot, dailyGoal: target, tdeeInsight: { ...insight, currentCalorieTarget: target } }));

    const card = element(harness, 'fd-dashboard-tdee-block');
    button(card, apply).click();
    redraw(harness);
    expect(saving.observed).toBe(true);

    button(card, 'TDEE_CARD.TITLE').click();
    const dialog = await vi.waitFor(() => {
        redraw(harness);
        const mounted = document.querySelector(dialogSelector);
        if (mounted === null) {
            throw new Error('Expected to open the TDEE details dialog while the card saves');
        }
        return mounted;
    });
    const dialogApply = [...dialog.querySelectorAll<HTMLButtonElement>('fd-tdee-insight-dialog-footer button')].find(control =>
        control.textContent.trim().endsWith(apply),
    );
    if (dialogApply === undefined) {
        throw new Error('Expected the TDEE suggestion action in the dialog footer');
    }

    saving.next({ dailyCalorieTarget: target, calorieCyclingEnabled: false });
    saving.complete();
    await vi.waitFor(() => {
        redraw(harness);
        expect(refreshSnapshot).toHaveBeenCalledOnce();
        expect(element(harness, 'fd-dashboard-tdee-block').querySelector('.tdee-card__suggestion')).toBeNull();
        expect(document.querySelector(dialogSelector)).toBe(dialog);
        expect(dialogApply.disabled).toBe(false);
    });
    expect(saving.observed).toBe(false);
    expect(updateGoal).toHaveBeenCalledExactlyOnceWith({ dailyCalorieTarget: target });

    dialogApply.click();

    expect(updateGoal).toHaveBeenCalledExactlyOnceWith({ dailyCalorieTarget: target });
    await vi.waitFor(() => {
        redraw(harness);
        expect(document.querySelector(dialogSelector)).toBeNull();
    });
});

function dashboardSnapshotWithTdee(target: number): DashboardSnapshot {
    const snapshot = makeSnapshot();
    return {
        ...snapshot,
        dashboardLayout: { web: ['summary', 'tdee'], mobile: ['summary', 'tdee'] },
        tdeeInsight: {
            estimatedTdee: 2300,
            adaptiveTdee: null,
            bmr: null,
            suggestedCalorieTarget: target,
            currentCalorieTarget: snapshot.dailyGoal,
            weightTrendPerWeek: null,
            confidence: 'low',
            dataDaysUsed: 0,
            goalAdjustmentHint: null,
        },
    };
}

describe('Mounted Dashboard dialog ownership', () => {
    const anotherDashboardDay = '/dashboard?date=2026-05-17';

    it.each([
        ['DASHBOARD.ACTIONS.OPEN_APPEARANCE', 'fd-appearance-dialog'],
        ['DASHBOARD.ACTIONS.OPEN_NOTIFICATION_SETTINGS', 'fd-dashboard-notification-settings-dialog'],
    ])('keeps %s while changing the Dashboard date and closes it on route departure', async (action, selector) => {
        const { harness } = await renderDashboardAsync();
        const dashboard = element(harness, '.dashboard');
        button(dashboard, action).click();
        const dialog = await vi.waitFor(() => {
            redraw(harness);
            const mounted = document.querySelector(selector);
            expect(mounted).not.toBeNull();
            return mounted;
        });

        await harness.navigateByUrl(anotherDashboardDay, DashboardComponent);
        redraw(harness);
        expect(element(harness, '.dashboard')).toBe(dashboard);
        expect(document.querySelector(selector)).toBe(dialog);

        await harness.navigateByUrl(OTHER_URL, OtherPageComponent);
        redraw(harness);
        expect(harness.routeNativeElement?.textContent).toContain('Another page');
        expect(document.querySelector(selector)).toBeNull();
    });

    it('keeps a mounted nutrition details request through Dashboard date changes and cancels it on departure', async () => {
        const { harness, refreshSnapshot } = await renderDashboardAsync();
        const pending = new Subject<DashboardSnapshot>();
        refreshSnapshot.mockReturnValueOnce(pending);
        element(harness, '[aria-label="DASHBOARD.NUTRITION_DETAILS.OPEN_CALORIES"]').click();
        const dialog = await findNutritionDialogAsync(harness);
        expect(dialog.textContent).toContain('DASHBOARD.NUTRITION_DETAILS.LOADING');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(anotherDashboardDay, DashboardComponent);
        redraw(harness);
        expect(document.querySelector(NUTRITION_DIALOG_SELECTOR)).toBe(dialog);
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_URL, OtherPageComponent);
        redraw(harness);
        expect(harness.routeNativeElement?.textContent).toContain('Another page');
        expect(document.querySelector(NUTRITION_DIALOG_SELECTOR)).toBeNull();
        expect(pending.observed).toBe(false);
    });
});

describe('Dashboard controls with partial data', () => {
    it('offers a hydration goal when the snapshot omits hydration without refetching a cached account', async () => {
        const snapshot = { ...makeSnapshot(), hydration: null };
        const account = { ...savedUser(), dashboardLayout: BASE_LAYOUT };
        const { harness, readAccount, router } = await renderDashboardAsync(snapshot, account);
        const hydration = element(harness, HYDRATION_TILE);
        expect(readAccount).not.toHaveBeenCalled();
        expect(hydration.querySelector('[role="progressbar"]')).toBeNull();
        expect(hydration.querySelector('.hydration-card__total')?.textContent.trim()).toBe('0');
        expect(hydration.textContent).toContain('HYDRATION_CARD.GOAL_TITLE');

        button(hydration, 'HYDRATION_CARD.GOAL_ACTION').click();
        await harness.fixture.whenStable();
        expect(router.url).toBe('/goals');
    });

    it('launches the welcome tour from the toolbar and disables it while arranging cards', async () => {
        const { harness } = await renderDashboardAsync();
        const dashboard = element(harness, '.dashboard');
        const launch = button(dashboard, 'DASHBOARD.ACTIONS.START_TOUR');
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        launch.click();
        expect(start).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'dashboard-welcome' }), { force: true });

        button(dashboard, EDIT_ACTION).click();
        expect(launch.disabled).toBe(false);
        launch.click();
        expect(start).toHaveBeenCalledOnce();
        redraw(harness);
        expect(launch.disabled).toBe(true);
        launch.click();
        expect(start).toHaveBeenCalledOnce();
    });
});

async function findNutritionDialogAsync(harness: RouterTestingHarness): Promise<HTMLElement> {
    return vi.waitFor(() => {
        redraw(harness);
        const dialog = document.querySelector(NUTRITION_DIALOG_SELECTOR);
        if (!(dialog instanceof HTMLElement)) {
            throw new Error('Expected the nutrition details dialog');
        }
        return dialog;
    });
}
describe('Dashboard cycle setup and aside visibility', () => {
    const cycleOnlyAside: DashboardLayoutSettings = {
        web: ['summary', 'cycle'],
        mobile: ['summary'],
    };

    it('uses the full dashboard width when the account hides its only aside card', async () => {
        const { harness } = await renderDashboardAsync({ ...makeSnapshot(), dashboardLayout: cycleOnlyAside, cycleHidden: true });
        const dashboard = element(harness, '.dashboard');

        expect(dashboard.querySelector('fd-cycle-summary-card')).toBeNull();
        expect(dashboard.querySelector('.dashboard__grid--single')).not.toBeNull();
        const aside = element(harness, '.dashboard__column--aside');
        expect(aside.hidden).toBe(true);
        expect(getComputedStyle(aside).display).toBe('none');
    });

    it('opens cycle tracking from the enabled card even before the first recorded cycle', async () => {
        const { harness, router } = await renderDashboardAsync({ ...makeSnapshot(), dashboardLayout: cycleOnlyAside, cycleHidden: false });
        const card = element(harness, 'fd-cycle-summary-card');

        expect(element(harness, '.dashboard__column--aside').contains(card)).toBe(true);
        expect(element(harness, '.dashboard__grid').classList.contains('dashboard__grid--single')).toBe(false);
        button(card, 'CYCLE_CARD.NO_DATA_ACTION').click();
        await vi.waitFor(() => {
            expect(router.url).toBe('/cycle-tracking');
            expect(harness.routeNativeElement?.textContent).toContain('Another page');
        });
    });
});

describe('Dashboard native layout by measured content width', () => {
    it('changes the visible mode on resize and saves the chosen card only in the narrow layout', async () => {
        const observers: DashboardResizeObserver[] = [];
        class DashboardResizeObserver implements ResizeObserver {
            public readonly observe = vi.fn<ResizeObserver['observe']>();
            public readonly unobserve = vi.fn<ResizeObserver['unobserve']>();
            public readonly disconnect = vi.fn<ResizeObserver['disconnect']>();

            public constructor(private readonly callback: ResizeObserverCallback) {
                observers.push(this);
            }

            public resize(target: Element, width: number): void {
                this.callback(
                    [
                        {
                            target,
                            contentRect: new DOMRect(0, 0, width),
                            borderBoxSize: [],
                            contentBoxSize: [],
                            devicePixelContentBoxSize: [],
                        },
                    ],
                    this,
                );
            }
        }
        const wideWidth = 1200;
        const narrowWidth = 500;
        vi.stubGlobal('ResizeObserver', DashboardResizeObserver);
        const originalLayout: DashboardLayoutSettings = {
            web: ['summary', 'hydration'],
            mobile: ['summary', 'nutrition-hidden'],
        };
        const savedLayout: DashboardLayoutSettings = {
            web: originalLayout.web,
            mobile: ['summary', 'hydration', 'nutrition-hidden'],
        };
        const { harness, router, updateLayout, readSnapshot } = await renderDashboardAsync({
            ...makeSnapshot(),
            dashboardLayout: originalLayout,
        });
        const dashboard = element(harness, '.dashboard');
        const resize = observers.find(observer => observer.observe.mock.calls.some(([observed]) => observed === dashboard));
        if (resize === undefined) {
            throw new Error('Expected an observer for the dashboard content');
        }
        updateLayout.mockReturnValueOnce(of({ ...savedUser(), dashboardLayout: savedLayout }));
        resize.resize(dashboard, wideWidth);
        redraw(harness);
        expect(dashboard.querySelector(HYDRATION_TILE)).not.toBeNull();
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).not.toBeNull();

        resize.resize(dashboard, narrowWidth);
        redraw(harness);
        expect(dashboard.querySelector(HYDRATION_TILE)).toBeNull();
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).toBeNull();
        expect(dashboard.querySelector('.dashboard__grid--single')).not.toBeNull();
        button(dashboard, EDIT_ACTION).click();
        redraw(harness);
        const hydration = element(harness, HYDRATION_TILE);
        expect(hydration.getAttribute('aria-pressed')).toBe('false');
        hydration.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', cancelable: true, bubbles: true }));
        redraw(harness);
        expect(hydration.getAttribute('aria-pressed')).toBe('true');
        button(dashboard, 'DASHBOARD.SETTINGS.SAVE').click();
        await vi.waitFor(() => {
            redraw(harness);
            expect(button(dashboard, EDIT_ACTION).getAttribute('aria-pressed')).toBe('false');
        });
        expect(updateLayout).toHaveBeenCalledExactlyOnceWith(savedLayout);
        expect(dashboard.querySelector(HYDRATION_TILE)).not.toBeNull();
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).toBeNull();

        resize.resize(dashboard, wideWidth);
        redraw(harness);
        expect(dashboard.querySelector('fd-nutrition-weekly-trend-card')).not.toBeNull();
        expect(readSnapshot).toHaveBeenCalledOnce();
        await expect(router.navigateByUrl(OTHER_URL)).resolves.toBe(true);
        expect(resize.disconnect).toHaveBeenCalledOnce();
    });
});

describe('Dashboard native meal navigation', () => {
    it('shows a historical meal without a recorded slot once and leaves out the current-day placeholders', async () => {
        useDashboardDay();
        const historicalDate = '2026-05-17';
        const meal: Meal = {
            id: entityId<'meal'>('historical-meal-without-slot'),
            date: utcInstant(`${historicalDate}T12:00:00.000Z`),
            mealType: null,
            comment: 'Imported lunch note',
            totalCalories: 300,
            totalProteins: 14,
            totalFats: 8,
            totalCarbs: 35,
            totalFiber: 5,
            totalAlcohol: 0,
            isNutritionAutoCalculated: true,
            items: [],
        };
        const snapshot = {
            ...makeSnapshot(),
            date: historicalDate,
            dateTo: historicalDate,
            meals: { items: [meal], total: 1 },
        };
        const { harness, router, readSnapshot } = await renderDashboardAsync(snapshot, null, {
            startUrl: `/dashboard?date=${historicalDate}`,
        });
        const mealPreview = element(harness, '[data-tour-id="dashboard-meals"]');
        const mealCards = mealPreview.querySelectorAll('fd-meal-card');

        expect(mealCards).toHaveLength(1);
        expect(mealCards[0].textContent).toContain('Imported lunch note');
        expect(mealPreview.querySelector('.meals-preview__placeholder')).toBeNull();
        expect(router.url).toBe(`/dashboard?date=${historicalDate}`);
        expect(readSnapshot).toHaveBeenCalledOnce();
    });

    it('routes an expanded breakfast placeholder to the manual meal editor with its meal slot', async () => {
        useDashboardDay();
        const { harness, router } = await renderDashboardAsync();
        const dashboard = element(harness, '.dashboard');
        const breakfast = Array.from(dashboard.querySelectorAll<HTMLButtonElement>('.meals-preview__placeholder-main')).find(candidate =>
            candidate.textContent.includes('MEAL_CARD.MEAL_TYPES.BREAKFAST'),
        );
        if (breakfast === undefined) {
            throw new Error('Expected a breakfast placeholder on the current dashboard');
        }
        breakfast.click();
        redraw(harness);

        expect(breakfast.getAttribute('aria-expanded')).toBe('true');
        button(element(harness, '.meals-preview__placeholder--expanded'), 'MEALS_PREVIEW.MANUAL_ACTION').click();
        await vi.waitFor(() => {
            const url = router.parseUrl(router.url);
            expect(url.root.children['primary'].segments.map(segment => segment.path)).toEqual(['meals', 'add']);
            expect(url.queryParams).toEqual({ mealType: 'BREAKFAST', manual: '1' });
        });
    });
});
