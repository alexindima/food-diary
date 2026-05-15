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
import { PremiumAccessPageComponent } from './premium-access-page.component';

const CHECKOUT_URL = 'https://checkout.example/session';
const PORTAL_URL = 'https://portal.example/session';

type BillingServiceMock = {
    getOverview: ReturnType<typeof vi.fn<() => Observable<BillingOverview>>>;
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

        await component.startCheckoutAsync('monthly', 'paddle');

        expect(billingService.createCheckoutSession).toHaveBeenCalledWith('monthly', 'paddle');
        expect(fakeDocument.location.href).toBe(CHECKOUT_URL);
        expect(component.checkoutLoadingPlan()).toBeNull();
    });

    it('shows checkout error when session URL is empty', async () => {
        billingService.createCheckoutSession.mockReturnValue(of({ sessionId: 'session-1', url: '', plan: 'monthly' }));
        const { component } = setupComponent();
        await settleAsync();

        await component.startCheckoutAsync('monthly');

        expect(fakeDocument.location.href).toBe('');
        expect(component.errorMessage()).toBe('Checkout URL is missing.');
        expect(toastService.error).toHaveBeenCalledWith('Checkout URL is missing.');
    });
});

describe('PremiumAccessPageComponent overview and portal', () => {
    beforeEach(resetMocks);

    it('opens billing portal session', async () => {
        const { component } = setupComponent();
        await settleAsync();

        await component.openPortalAsync();

        expect(billingService.createPortalSession).toHaveBeenCalled();
        expect(fakeDocument.location.href).toBe(PORTAL_URL);
        expect(component.portalLoading()).toBe(false);
    });

    it('stores overview load errors and clears overview', async () => {
        billingService.getOverview.mockReturnValue(throwError(() => new Error('Network down')));
        const { component } = setupComponent();
        await settleAsync();

        expect(component.overview()).toBeNull();
        expect(component.errorMessage()).toBe('Network down');
        expect(component.isLoading()).toBe(false);
    });

    it('handles successful checkout return state and removes query params', async () => {
        queryParams = { checkout: 'success' };
        const { component } = setupComponent();
        await settleAsync();

        expect(authService.refreshToken).toHaveBeenCalled();
        expect(component.checkoutReturnState()).toBe('success');
        expect(toastService.success).toHaveBeenCalledWith('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE');
        expect(router.navigate).toHaveBeenCalledWith([], {
            relativeTo: routeStub,
            queryParams: {},
            replaceUrl: true,
        });
    });
});

function resetMocks(): void {
    billingService = {
        getOverview: vi.fn(() => of(createOverview())),
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
    await Promise.resolve();
    await Promise.resolve();
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
        provider: 'none',
        paddleClientToken: null,
        availableProviders: ['paddle'],
    };
}
