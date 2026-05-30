import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NEVER, type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { PremiumBillingService } from '../../api/premium-billing.service';
import { PaddleCheckoutService } from '../../lib/paddle-checkout.service';
import type {
    BillingOverview,
    BillingPlan,
    BillingProvider,
    CheckoutSessionResponse,
    PortalSessionResponse,
} from '../../models/billing.models';
import { PremiumAccessPageComponent } from './premium-access-page';

const CHECKOUT_URL = 'https://checkout.example/session';
const PORTAL_URL = 'https://portal.example/session';
const ASYNC_SETTLE_TURNS = 8;

type BillingServiceMock = {
    getOverview: ReturnType<typeof vi.fn<() => Observable<BillingOverview>>>;
    startPremiumTrial: ReturnType<typeof vi.fn<() => Observable<BillingOverview>>>;
    createCheckoutSession: ReturnType<typeof vi.fn<(plan: BillingPlan, provider?: BillingProvider) => Observable<CheckoutSessionResponse>>>;
    createPortalSession: ReturnType<typeof vi.fn<() => Observable<PortalSessionResponse>>>;
};

type AuthServiceMock = {
    isPremium: WritableSignal<boolean>;
    refreshToken: ReturnType<typeof vi.fn<() => Observable<boolean>>>;
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
        expect(component['errorMessage']()).toBe('Checkout URL is missing.');
        expect(toastService.error).toHaveBeenCalledWith('Checkout URL is missing.');
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
        expect(component['errorMessage']()).toBe('Network down');
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
        expect(component['errorMessage']()).toBe('Network down');
        expect(component['isLoading']()).toBe(false);
    });
});

function resetMocks(): void {
    billingService = {
        getOverview: vi.fn(() => of(createOverview())),
        startPremiumTrial: vi.fn(() => of(createOverview())),
        createCheckoutSession: vi.fn(() => of({ sessionId: 'session-1', url: CHECKOUT_URL, plan: 'monthly' })),
        createPortalSession: vi.fn(() => of({ url: PORTAL_URL })),
    };
    authService = {
        isPremium: signal(false),
        refreshToken: vi.fn(() => of(true)),
    };
    toastService = {
        success: vi.fn(),
        info: vi.fn(),
        error: vi.fn(),
    };
    router = {
        navigate: vi.fn(async () => {
            await Promise.resolve();
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

function setupComponent(): {
    component: PremiumAccessPageComponent;
} {
    routeStub = {
        snapshot: {
            queryParamMap: convertToParamMap(queryParams),
        },
    };

    TestBed.configureTestingModule({
        providers: getPremiumAccessPageProviders(),
    });
    const component = TestBed.runInInjectionContext(() => new PremiumAccessPageComponent());

    return {
        component,
    };
}

function getPremiumAccessPageProviders(): unknown[] {
    return [
        { provide: PremiumBillingService, useValue: billingService },
        {
            provide: PaddleCheckoutService,
            useValue: {
                openTransactionCheckoutAsync: vi.fn(async () => {
                    await Promise.resolve();
                }),
            },
        },
        { provide: AuthService, useValue: authService },
        { provide: FdUiToastService, useValue: toastService },
        { provide: TranslateService, useValue: createTranslateServiceStub() },
        { provide: ActivatedRoute, useValue: routeStub },
        { provide: Router, useValue: router },
        { provide: DOCUMENT, useValue: fakeDocument },
        { provide: PLATFORM_ID, useValue: 'browser' },
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
        getCurrentLang: (): string => 'en',
        getFallbackLang: (): string => 'en',
        instant: (key: string): string => key,
    };
}

async function settleAsync(): Promise<void> {
    for (let index = 0; index < ASYNC_SETTLE_TURNS; index++) {
        await Promise.resolve();
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
