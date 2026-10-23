import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NEVER, type Observable, of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { AuthService } from '../../../../services/auth.service';
import type {
    BillingOverview,
    BillingPlan,
    BillingProvider,
    CheckoutSessionResponse,
    PortalSessionResponse,
} from '../../../../shared/models/billing.models';
import { PaddleCheckoutService } from '../../lib/paddle-checkout.service';
import { PremiumBillingFacade } from '../../lib/premium-billing.facade';
import { PremiumPlanPricingFacade } from '../../lib/premium-plan-pricing.facade';
import { PremiumAccessPageComponent } from './premium-access-page';

const CHECKOUT_URL = 'https://checkout.example/session';
const PORTAL_URL = 'https://portal.example/session';
const ASYNC_SETTLE_TURNS = 8;
const CHECKOUT_POLL_DELAY_MS = 1500;
const CHECKOUT_POLL_ATTEMPTS = 6;
const DELAYED_ACTIVATION_OVERVIEW_READS = 3;
const DEFAULT_PADDLE_CLIENT_TOKEN = environment.paddleClientToken;

type BillingServiceMock = {
    getOverview: ReturnType<typeof vi.fn<() => Observable<BillingOverview>>>;
    startPremiumTrial: ReturnType<typeof vi.fn<() => Observable<BillingOverview>>>;
    createCheckoutSession: ReturnType<typeof vi.fn<(plan: BillingPlan, provider?: BillingProvider) => Observable<CheckoutSessionResponse>>>;
    createPortalSession: ReturnType<typeof vi.fn<() => Observable<PortalSessionResponse>>>;
};

type AuthServiceMock = {
    isPremium: WritableSignal<boolean>;
    refreshToken: ReturnType<typeof vi.fn<() => Observable<string | null>>>;
};

type ToastServiceMock = {
    success: ReturnType<typeof vi.fn<(message: string) => void>>;
    info: ReturnType<typeof vi.fn<(message: string) => void>>;
    error: ReturnType<typeof vi.fn<(message: string) => void>>;
};

type RouterMock = {
    navigate: ReturnType<typeof vi.fn<(commands: unknown[], extras: Record<string, unknown>) => Promise<boolean>>>;
};

type FakeDocument = {
    location: {
        href: string;
    };
};

let billingService: BillingServiceMock;
let authService: AuthServiceMock;
let toastService: ToastServiceMock;
let router: RouterMock;
let routeStub: { snapshot: { queryParamMap: ReturnType<typeof convertToParamMap> } };
let fakeDocument: FakeDocument;
let queryParams: Record<string, string>;
let paddlePreview: ReturnType<typeof vi.fn<PaddleCheckoutService['previewPlanPricesAsync']>>;
let paddleOpen: ReturnType<typeof vi.fn<PaddleCheckoutService['openTransactionCheckoutAsync']>>;
let currentLanguage: string;

describe('PremiumAccessPageComponent checkout', () => {
    beforeEach(resetMocks);

    it('redirects to checkout session URL for selected provider', async () => {
        const { component } = setupComponent();
        await settleAsync();

        await component['startCheckoutAsync']('monthly', 'paddle');

        expect(billingService.createCheckoutSession).toHaveBeenCalledWith('monthly', 'paddle');
        expect(fakeDocument.location.href).toBe(CHECKOUT_URL);
        expect(component['checkoutLoadingPlan']()).toBeNull();
    });

    it('shows checkout error when session URL is empty', async () => {
        billingService.createCheckoutSession.mockReturnValue(of({ sessionId: 'session-1', url: '', plan: 'monthly' }));
        const { component } = setupComponent();
        await settleAsync();

        await component['startCheckoutAsync']('monthly');

        expect(fakeDocument.location.href).toBe('');
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_CHECKOUT');
        expect(toastService.error).toHaveBeenCalledWith('PREMIUM_PAGE.ERROR_CHECKOUT');
    });

    it('does not create a second checkout session while checkout is loading', async () => {
        billingService.createCheckoutSession.mockReturnValue(NEVER);
        const { component } = setupComponent();
        await settleAsync();

        void component['startCheckoutAsync']('monthly', 'paddle');
        await settleAsync();
        await component['startCheckoutAsync']('yearly', 'paddle');

        expect(billingService.createCheckoutSession).toHaveBeenCalledTimes(1);
        expect(billingService.createCheckoutSession).toHaveBeenCalledWith('monthly', 'paddle');
        expect(component['checkoutLoadingPlan']()).toBe('monthly');
    });
});

describe('PremiumAccessPageComponent entitlement recovery', () => {
    beforeEach(resetMocks);

    it('refreshes stale free claims for active access and avoids another refresh once they match', async () => {
        billingService.getOverview.mockReturnValue(of({ ...createOverview(), isPremium: true, premiumTrialActive: true }));
        authService.refreshToken.mockImplementation(() => {
            authService.isPremium.set(true);
            return of('refreshed-session');
        });
        const { component } = setupComponent();
        await settleAsync();
        expect(authService.refreshToken).toHaveBeenCalledTimes(1);
        expect(authService.isPremium()).toBe(true);
        await component['reloadOverviewAsync']();
        expect(authService.refreshToken).toHaveBeenCalledTimes(1);
        expect(component['errorMessage']()).toBeNull();
    });

    it('refreshes stale Premium claims when the trusted overview no longer grants access', async () => {
        authService.isPremium.set(true);
        authService.refreshToken.mockImplementation(() => {
            authService.isPremium.set(false);
            return of('refreshed-session');
        });
        setupComponent();
        await settleAsync();
        expect(authService.refreshToken).toHaveBeenCalledTimes(1);
        expect(authService.isPremium()).toBe(false);
    });

    it('keeps matching free access and SSR overview reads free of session refresh side effects', async () => {
        setupComponent();
        await settleAsync();
        expect(authService.refreshToken).not.toHaveBeenCalled();
        TestBed.resetTestingModule();
        billingService.getOverview.mockReturnValue(of({ ...createOverview(), isPremium: true }));
        setupComponent('server');
        await settleAsync();
        expect(authService.refreshToken).not.toHaveBeenCalled();
    });

    it('explains failed access refresh and recovers through the existing overview retry', async () => {
        billingService.getOverview.mockReturnValue(of({ ...createOverview(), isPremium: true }));
        authService.refreshToken.mockReturnValue(of(null));
        const { component } = setupComponent();
        await settleAsync();
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_ACCESS_REFRESH');
        expect(authService.isPremium()).toBe(false);
        authService.refreshToken.mockImplementation(() => {
            authService.isPremium.set(true);
            return of('refreshed-session');
        });
        await component['reloadOverviewAsync']();
        expect(component['errorMessage']()).toBeNull();
        expect(authService.isPremium()).toBe(true);
    });

    it('keeps the started trial but does not announce successful access when refresh fails', async () => {
        const { component } = setupComponent();
        await settleAsync();
        billingService.startPremiumTrial.mockReturnValue(of({ ...createOverview(), isPremium: true, premiumTrialActive: true }));
        authService.refreshToken.mockReturnValue(of(null));
        await component['startTrialAsync']();
        expect(component['overview']()?.premiumTrialActive).toBe(true);
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_ACCESS_REFRESH');
        expect(toastService.success).not.toHaveBeenCalled();
        expect(component['trialLoading']()).toBe(false);
    });
});

describe('PremiumAccessPageComponent prices', () => {
    beforeEach(resetMocks);

    it('loads visible plan prices from public catalog configuration without creating checkout', async () => {
        billingService.getOverview.mockReturnValue(
            of({
                ...createOverview(),
                paddleClientToken: 'test_token',
                paddleMonthlyPriceId: 'pri_month',
                paddleYearlyPriceId: 'pri_year',
            }),
        );
        const { component } = setupComponent();
        await settleAsync();

        expect(paddlePreview).toHaveBeenCalledWith(
            { monthly: 'pri_month', yearly: 'pri_year' },
            { token: 'test_token', environment: 'sandbox', locale: 'en' },
        );
        expect(component['pricing'].prices().monthly?.formattedTotal).toBe('$8.00');
        expect(billingService.createCheckoutSession).not.toHaveBeenCalled();
        expect(paddleOpen).not.toHaveBeenCalled();
    });

    it('does not request catalog prices when plans are hidden for paid Premium', async () => {
        billingService.getOverview.mockReturnValue(
            of({
                ...createOverview(),
                isPremium: true,
                paddleClientToken: 'test_token',
                paddleMonthlyPriceId: 'pri_month',
                paddleYearlyPriceId: 'pri_year',
            }),
        );
        setupComponent();
        await settleAsync();
        expect(paddlePreview).not.toHaveBeenCalled();
    });
});

describe('PremiumAccessPageComponent overview and portal', () => {
    beforeEach(resetMocks);

    it('opens billing portal session', async () => {
        const { component } = setupComponent();
        await settleAsync();

        await component['openPortalAsync']();

        expect(billingService.createPortalSession).toHaveBeenCalled();
        expect(fakeDocument.location.href).toBe(PORTAL_URL);
        expect(component['portalLoading']()).toBe(false);
    });

    it('does not create a second portal session while portal is loading', async () => {
        billingService.createPortalSession.mockReturnValue(NEVER);
        const { component } = setupComponent();
        await settleAsync();

        void component['openPortalAsync']();
        await settleAsync();
        await component['openPortalAsync']();

        expect(billingService.createPortalSession).toHaveBeenCalledTimes(1);
        expect(component['portalLoading']()).toBe(true);
    });

    it('stores overview load errors when no overview is loaded', async () => {
        billingService.getOverview.mockReturnValue(throwError(() => new Error('Network down')));
        const { component } = setupComponent();
        await settleAsync();

        expect(component['overview']()).toBeNull();
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_GENERIC');
        expect(component['isLoading']()).toBe(false);
    });

    it('starts premium trial and refreshes token', async () => {
        const trialOverview = {
            ...createOverview(),
            isPremium: true,
            subscriptionStatus: 'trialing',
            premiumTrialActive: true,
            premiumTrialUsed: true,
            canStartPremiumTrial: false,
        };
        billingService.startPremiumTrial.mockReturnValue(of(trialOverview));
        const { component } = setupComponent();
        await settleAsync();

        await component['startTrialAsync']();

        expect(billingService.startPremiumTrial).toHaveBeenCalled();
        expect(authService.refreshToken).toHaveBeenCalled();
        expect(component['overview']()).toEqual(trialOverview);
        expect(component['trialLoading']()).toBe(false);
        expect(toastService.success).toHaveBeenCalledWith('PREMIUM_PAGE.BANNERS.TRIAL_STARTED_MESSAGE');
    });

    it('does not start a second premium trial while trial request is loading', async () => {
        billingService.startPremiumTrial.mockReturnValue(NEVER);
        const { component } = setupComponent();
        await settleAsync();

        void component['startTrialAsync']();
        await settleAsync();
        await component['startTrialAsync']();

        expect(billingService.startPremiumTrial).toHaveBeenCalledTimes(1);
        expect(component['trialLoading']()).toBe(true);
    });
});

describe('PremiumAccessPageComponent checkout return', () => {
    beforeEach(resetMocks);

    it('handles successful checkout return state and removes query params', async () => {
        queryParams = { checkout: 'success' };
        const premiumOverview = { ...createOverview(), isPremium: true, subscriptionStatus: 'active' };
        billingService.getOverview.mockReturnValue(of(premiumOverview));
        const { component } = setupComponent();
        await settleAsync();

        expect(authService.refreshToken).toHaveBeenCalled();
        expect(component['overview']()).toEqual(premiumOverview);
        expect(component['checkoutReturnState']()).toBe('success');
        expect(toastService.success).toHaveBeenCalledWith('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE');
        expect(router.navigate).toHaveBeenCalledWith([], {
            relativeTo: routeStub,
            queryParams: {},
            replaceUrl: true,
        });
    });

    it('keeps premium overview from checkout polling when follow-up overview reload fails', async () => {
        queryParams = { checkout: 'success' };
        const premiumOverview = { ...createOverview(), isPremium: true, subscriptionStatus: 'active' };
        billingService.getOverview
            .mockReturnValueOnce(of(premiumOverview))
            .mockReturnValueOnce(throwError(() => new Error('Network down')));
        const { component } = setupComponent();
        await settleAsync();

        expect(authService.refreshToken).toHaveBeenCalled();
        expect(component['overview']()).toEqual(premiumOverview);
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_GENERIC');
        expect(component['isLoading']()).toBe(false);
    });
});

describe('PremiumAccessPageComponent delayed checkout activation', () => {
    beforeEach(() => {
        resetMocks();
        vi.useFakeTimers();
        queryParams = { checkout: 'success' };
    });
    afterEach(() => {
        vi.useRealTimers();
    });

    it('waits for the next activation poll before refreshing newly granted access', async () => {
        const premiumOverview = { ...createOverview(), isPremium: true, subscriptionStatus: 'active' };
        billingService.getOverview.mockReturnValueOnce(of(createOverview())).mockReturnValue(of(premiumOverview));
        authService.refreshToken.mockImplementation(() => {
            authService.isPremium.set(true);
            return of('premium-session');
        });
        const { component } = setupComponent();
        await settleAsync();
        expect(billingService.getOverview).toHaveBeenCalledTimes(1);
        expect(authService.refreshToken).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(CHECKOUT_POLL_DELAY_MS - 1);
        expect(billingService.getOverview).toHaveBeenCalledTimes(1);
        await vi.advanceTimersByTimeAsync(1);
        await settleAsync();
        expect(authService.refreshToken).toHaveBeenCalledOnce();
        expect(billingService.getOverview).toHaveBeenCalledTimes(DELAYED_ACTIVATION_OVERVIEW_READS);
        expect(component['overview']()).toEqual(premiumOverview);
        expect(component['isLoading']()).toBe(false);
        expect(router.navigate).toHaveBeenCalledWith([], { relativeTo: routeStub, queryParams: {}, replaceUrl: true });
    });
});

describe('PremiumAccessPageComponent activation polling recovery', () => {
    beforeEach(() => {
        resetMocks();
        vi.useFakeTimers();
        queryParams = { checkout: 'success' };
    });
    afterEach(() => {
        vi.useRealTimers();
    });

    it('stops activation polling at its limit and refreshes the session once before the normal overview load', async () => {
        const { component } = setupComponent();
        await settleAsync();
        await vi.advanceTimersByTimeAsync(CHECKOUT_POLL_DELAY_MS * (CHECKOUT_POLL_ATTEMPTS - 1));
        expect(billingService.getOverview).toHaveBeenCalledTimes(CHECKOUT_POLL_ATTEMPTS);
        expect(authService.refreshToken).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(CHECKOUT_POLL_DELAY_MS);
        await settleAsync();
        expect(authService.refreshToken).toHaveBeenCalledOnce();
        expect(billingService.getOverview).toHaveBeenCalledTimes(CHECKOUT_POLL_ATTEMPTS + 1);
        expect(component['isPremium']()).toBe(false);
        expect(component['isLoading']()).toBe(false);
        expect(router.navigate).toHaveBeenCalledOnce();
        await vi.advanceTimersByTimeAsync(CHECKOUT_POLL_DELAY_MS * CHECKOUT_POLL_ATTEMPTS);
        expect(billingService.getOverview).toHaveBeenCalledTimes(CHECKOUT_POLL_ATTEMPTS + 1);
    });

    it('falls back to one session refresh after a polling read fails and then reloads the overview', async () => {
        billingService.getOverview.mockReturnValueOnce(throwError(() => new Error('Activation read failed')));
        const { component } = setupComponent();
        await settleAsync();
        expect(authService.refreshToken).toHaveBeenCalledOnce();
        expect(billingService.getOverview).toHaveBeenCalledTimes(2);
        expect(component['overview']()).toEqual(createOverview());
        expect(component['errorMessage']()).toBeNull();
        expect(router.navigate).toHaveBeenCalledWith([], { relativeTo: routeStub, queryParams: {}, replaceUrl: true });
        await vi.advanceTimersByTimeAsync(CHECKOUT_POLL_DELAY_MS * CHECKOUT_POLL_ATTEMPTS);
        expect(billingService.getOverview).toHaveBeenCalledTimes(2);
    });
});

describe('PremiumAccessPageComponent Paddle transaction return', () => {
    beforeEach(() => {
        resetMocks();
        queryParams = { _ptxn: 'txn_returned' };
    });
    afterEach(() => {
        environment.paddleClientToken = DEFAULT_PADDLE_CLIENT_TOKEN;
    });

    it.each([
        { token: ' test_checkout ', expectedEnvironment: 'sandbox', language: 'ru' },
        { token: ' live_checkout ', expectedEnvironment: 'production', language: 'en' },
    ])(
        'opens the returned transaction in $expectedEnvironment with its token and locale',
        async ({ token, expectedEnvironment, language }) => {
            currentLanguage = language;
            environment.paddleClientToken = 'test_environment_fallback';
            billingService.getOverview.mockReturnValue(of({ ...createOverview(), paddleClientToken: token }));
            setupComponent();
            await settleAsync();
            expect(paddleOpen).toHaveBeenCalledExactlyOnceWith('txn_returned', {
                token: token.trim(),
                environment: expectedEnvironment,
                locale: language,
            });
            expect(router.navigate).toHaveBeenCalledWith([], { relativeTo: routeStub, queryParams: {}, replaceUrl: true });
            expect(billingService.createCheckoutSession).not.toHaveBeenCalled();
        },
    );

    it('uses the configured client token when the overview has no client token', async () => {
        environment.paddleClientToken = ' live_fallback ';
        setupComponent();
        await settleAsync();
        expect(paddleOpen).toHaveBeenCalledExactlyOnceWith('txn_returned', {
            token: 'live_fallback',
            environment: 'production',
            locale: 'en',
        });
        expect(router.navigate).toHaveBeenCalledOnce();
    });

    it('shows a recoverable error without opening checkout or clearing the transaction when no client token exists', async () => {
        environment.paddleClientToken = '';
        const { component } = setupComponent();
        await settleAsync();
        expect(paddleOpen).not.toHaveBeenCalled();
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_CHECKOUT');
        expect(toastService.error).toHaveBeenCalledWith('PREMIUM_PAGE.ERROR_CHECKOUT');
        expect(router.navigate).not.toHaveBeenCalled();
    });
});

describe('PremiumAccessPageComponent Paddle transaction failures and SSR', () => {
    beforeEach(() => {
        resetMocks();
        queryParams = { _ptxn: 'txn_returned' };
        billingService.getOverview.mockReturnValue(of({ ...createOverview(), paddleClientToken: 'test_checkout' }));
    });

    it('reports provider failure and retains the returned transaction for retry', async () => {
        paddleOpen.mockRejectedValue(new Error('Paddle unavailable'));
        const { component } = setupComponent();
        await settleAsync();
        expect(paddleOpen).toHaveBeenCalledOnce();
        expect(component['errorMessage']()).toBe('PREMIUM_PAGE.ERROR_CHECKOUT');
        expect(toastService.error).toHaveBeenCalledWith('PREMIUM_PAGE.ERROR_CHECKOUT');
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('does not open a Paddle transaction or rewrite its URL during server rendering', async () => {
        const { component } = setupComponent('server');
        await settleAsync();
        expect(billingService.getOverview).toHaveBeenCalledOnce();
        expect(paddleOpen).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(component['errorMessage']()).toBeNull();
    });
});

function resetMocks(): void {
    currentLanguage = 'en';
    paddlePreview = vi.fn().mockResolvedValue({
        monthly: { formattedTotal: '$8.00', currencyCode: 'USD' },
        yearly: { formattedTotal: '$80.00', currencyCode: 'USD' },
    });
    paddleOpen = vi.fn(async () => {
        await waitForAsyncTasksAsync();
    });
    billingService = {
        getOverview: vi.fn(() => of(createOverview())),
        startPremiumTrial: vi.fn(() => of(createOverview())),
        createCheckoutSession: vi.fn(() => of({ sessionId: 'session-1', url: CHECKOUT_URL, plan: 'monthly' })),
        createPortalSession: vi.fn(() => of({ url: PORTAL_URL })),
    };
    authService = {
        isPremium: signal(false),
        refreshToken: vi.fn<() => Observable<string | null>>(() => of('refreshed-session')),
    };
    toastService = {
        success: vi.fn(),
        info: vi.fn(),
        error: vi.fn(),
    };
    router = {
        navigate: vi.fn(async () => {
            await waitForAsyncTasksAsync();
            return true;
        }),
    };
    fakeDocument = {
        location: {
            href: '',
        },
    };
    queryParams = {};
}

function setupComponent(platformId: 'browser' | 'server' = 'browser'): {
    component: PremiumAccessPageComponent;
} {
    routeStub = {
        snapshot: {
            queryParamMap: convertToParamMap(queryParams),
        },
    };

    TestBed.configureTestingModule({
        providers: getPremiumAccessPageProviders(platformId),
    });
    const component = TestBed.runInInjectionContext(() => new PremiumAccessPageComponent());

    return {
        component,
    };
}

function getPremiumAccessPageProviders(platformId: 'browser' | 'server'): unknown[] {
    return [
        PremiumPlanPricingFacade,
        { provide: PremiumBillingFacade, useValue: billingService },
        {
            provide: PaddleCheckoutService,
            useValue: {
                previewPlanPricesAsync: paddlePreview,
                openTransactionCheckoutAsync: paddleOpen,
            },
        },
        { provide: AuthService, useValue: authService },
        { provide: FdUiToastService, useValue: toastService },
        { provide: TranslateService, useValue: createTranslateServiceStub() },
        { provide: ActivatedRoute, useValue: routeStub },
        { provide: Router, useValue: router },
        { provide: DOCUMENT, useValue: fakeDocument },
        { provide: PLATFORM_ID, useValue: platformId },
    ];
}

function createTranslateServiceStub(): {
    onLangChange: typeof NEVER;
    getCurrentLang: () => string;
    getFallbackLang: () => string;
    instant: (key: string) => string;
} {
    return {
        onLangChange: NEVER,
        getCurrentLang: (): string => currentLanguage,
        getFallbackLang: (): string => 'en',
        instant: (key: string): string => key,
    };
}

async function settleAsync(): Promise<void> {
    for (let index = 0; index < ASYNC_SETTLE_TURNS; index++) {
        await waitForAsyncTasksAsync();
    }
}

function createOverview(): BillingOverview {
    return {
        isPremium: false,
        subscriptionStatus: null,
        plan: null,
        subscriptionProvider: null,
        currentPeriodStartUtc: null,
        currentPeriodEndUtc: null,
        nextBillingAttemptUtc: null,
        cancelAtPeriodEnd: false,
        renewalEnabled: false,
        manageBillingAvailable: false,
        premiumTrialStartUtc: null,
        premiumTrialEndUtc: null,
        premiumTrialActive: false,
        premiumTrialUsed: false,
        canStartPremiumTrial: true,
        provider: 'none',
        paddleClientToken: null,
        availableProviders: ['paddle'],
    };
}
