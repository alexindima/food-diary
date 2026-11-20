import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import type { BillingOverview, CheckoutSessionResponse, PortalSessionResponse } from '../../shared/models/billing.models';
import { PremiumBillingService } from './api/premium-billing.service';
import { PaddleCheckoutService } from './lib/paddle-checkout.service';
import premiumRoutes from './premium.routes';

@Component({ template: '<h1>Another page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherPageComponent {}

const PREMIUM_ROUTE = '/premium';
const RETURNED_TRANSACTION = 'fixture-transaction';
const TRANSACTION_ROUTE = `${PREMIUM_ROUTE}?_ptxn=${RETURNED_TRANSACTION}`;
const OTHER_ROUTE = '/other?tab=details';
const PLANS = 'fd-premium-plans-card';
const PRICED_MONTHLY = '$8.00';
const PRICED_YEARLY = '$80.00';

type PremiumFixture = {
    harness: RouterTestingHarness;
    router: Router;
    createCheckout: Mock<PremiumBillingService['createCheckoutSession']>;
    createPortal: Mock<PremiumBillingService['createPortalSession']>;
    startTrial: Mock<PremiumBillingService['startPremiumTrial']>;
    refreshSession: Mock<AuthService['refreshSession']>;
    openInvoice: Mock<TelegramWebAppService['openInvoiceAsync']>;
    openWidget: Mock<PaddleCheckoutService['openTransactionCheckoutAsync']>;
    previewPrices: Mock<PaddleCheckoutService['previewPlanPricesAsync']>;
    toast: FdUiToastService;
};

describe('Premium checkout return stays with its owning screen', () => {
    it('abandons the overview request on departure and opens only the checkout for a fresh visit', async () => {
        const pending = new Subject<BillingOverview>();
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValueOnce(pending);
        const { harness, router, openWidget, createCheckout } = await renderPremiumAsync(TRANSACTION_ROUTE, readOverview);
        expect(premiumRoot(harness).textContent).toContain('PREMIUM_PAGE.STATUS_LOADING');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        const stillObservingDepartedOverview = pending.observed;
        pending.next(overview());
        pending.complete();
        await redrawAsync(harness);
        expect(openWidget).not.toHaveBeenCalled();
        expect(stillObservingDepartedOverview).toBe(false);
        expect(router.url).toBe(OTHER_ROUTE);

        readOverview.mockReturnValue(of(overview()));
        await harness.navigateByUrl(TRANSACTION_ROUTE);
        await redrawAsync(harness);
        expect(openWidget).toHaveBeenCalledExactlyOnceWith(RETURNED_TRANSACTION, {
            token: 'test_fixture',
            environment: 'sandbox',
            locale: 'en',
        });
        expect(router.url).toBe(PREMIUM_ROUTE);
        expect(createCheckout).not.toHaveBeenCalled();
    });

    it.each(['succeeded', 'failed'] as const)('ignores a provider window that %s after departure', async result => {
        const { harness, router, openWidget, toast } = await renderPremiumAsync('/other');
        const provider = new Subject<void>();
        openWidget.mockReturnValueOnce(firstValueFrom(provider));
        await harness.navigateByUrl(TRANSACTION_ROUTE);
        await redrawAsync(harness);
        expect(openWidget).toHaveBeenCalledOnce();
        expect(router.url).toBe(TRANSACTION_ROUTE);

        await harness.navigateByUrl(OTHER_ROUTE);
        if (result === 'succeeded') {
            provider.next();
            provider.complete();
        } else {
            provider.error(new Error('Provider window closed'));
        }
        await redrawAsync(harness);
        expect(router.url).toBe(OTHER_ROUTE);
        expect(toast.toasts()).toEqual([]);
    });
});

describe('Premium activation return ownership', () => {
    it('abandons a pending activation check when leaving the return screen', async () => {
        const pending = new Subject<BillingOverview>();
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(overview())).mockReturnValueOnce(pending);
        const { harness, router, refreshSession, toast } = await renderPremiumAsync('/premium?checkout=success', readOverview);
        expect(premiumRoot(harness).textContent).toContain('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        const stillObservingDepartedOverview = pending.observed;
        pending.next(overview({ isPremium: true, subscriptionStatus: 'active' }));
        pending.complete();
        await redrawAsync(harness);
        expect(refreshSession).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
        expect(stillObservingDepartedOverview).toBe(false);
        expect(router.url).toBe(OTHER_ROUTE);
        expect(readOverview).toHaveBeenCalledOnce();
    });
});

describe('Premium rendered account and plan controls', () => {
    it('recovers from a failed account read and renders the available prices without starting a checkout', async () => {
        const readOverview = vi
            .fn<PremiumBillingService['getOverview']>()
            .mockReturnValueOnce(throwError(() => new Error('Billing overview unavailable')))
            .mockReturnValueOnce(
                of(
                    overview({
                        availableProviders: [' ', 'paddle'],
                        paddleMonthlyPriceId: 'fixture-monthly',
                        paddleYearlyPriceId: 'fixture-yearly',
                    }),
                ),
            );
        const { harness, createCheckout, previewPrices, openWidget } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const root = premiumRoot(harness);
        expect(root.textContent).toContain('PREMIUM_PAGE.ERROR_GENERIC');
        expect(root.querySelector(PLANS)).toBeNull();

        nativeButton(root, 'PREMIUM_PAGE.RETRY_ACTION').click();
        await redrawAsync(harness);
        expect(root.textContent).not.toContain('PREMIUM_PAGE.ERROR_GENERIC');
        expect(root.querySelector(PLANS)?.textContent).toContain(PRICED_MONTHLY);
        expect(root.querySelector(PLANS)?.textContent).toContain(PRICED_YEARLY);
        expect(root.querySelector('.premium-access-page__provider-actions')).toBeNull();
        expect(previewPrices).toHaveBeenCalledOnce();
        expect(createCheckout).not.toHaveBeenCalled();
        expect(openWidget).not.toHaveBeenCalled();
    });

    it('focuses renewal choices from the ended subscription without starting checkout', async () => {
        const account = overview({ subscriptionStatus: 'canceled', manageBillingAvailable: true, canStartPremiumTrial: false });
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(account));
        const { harness, createCheckout } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const root = premiumRoot(harness);
        const plans = root.querySelector(PLANS);
        if (!(plans instanceof HTMLElement)) {
            throw new Error('Expected the plans for a historical subscription');
        }
        const scroll = vi.fn();
        Object.defineProperty(plans, 'scrollIntoView', { value: scroll });

        nativeButton(root, 'PREMIUM_PAGE.OVERVIEW.RESUME_ACTION').click();
        expect(document.activeElement).toBe(plans);
        expect(scroll).toHaveBeenCalledExactlyOnceWith({ behavior: 'smooth', block: 'start' });
        expect(createCheckout).not.toHaveBeenCalled();
    });
});

describe('Premium direct checkout ownership', () => {
    const session: CheckoutSessionResponse = { sessionId: 'fixture-session', plan: 'monthly', url: 'https://example.test/stars-invoice' };
    const starsOverview = overview({ availableProviders: ['telegramstars'], telegramStarsPrice: 300 });
    const starsSubscribe = 'PREMIUM_PAGE.STARS.SUBSCRIBE';

    it('does not open an invoice delivered to a checkout after its page was left', async () => {
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(starsOverview));
        const { harness, router, createCheckout, openInvoice, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview, true);
        const pending = new Subject<CheckoutSessionResponse>();
        createCheckout.mockReturnValueOnce(pending);
        nativeButton(premiumRoot(harness), starsSubscribe).click();
        expect(createCheckout).toHaveBeenCalledExactlyOnceWith('monthly', 'telegramstars');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        const stillPending = pending.observed;
        pending.next(session);
        pending.complete();
        await redrawAsync(harness);

        expect(openInvoice).not.toHaveBeenCalled();
        expect(stillPending).toBe(false);
        expect(toast.toasts()).toEqual([]);
        expect(router.url).toBe(OTHER_ROUTE);
    });

    it('does not show a pending invoice message after navigation away from a previously opened invoice', async () => {
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(starsOverview));
        const { harness, router, createCheckout, openInvoice, refreshSession, toast } = await renderPremiumAsync(
            PREMIUM_ROUTE,
            readOverview,
            true,
        );
        const invoiceResult = new Subject<string>();
        createCheckout.mockReturnValueOnce(of(session));
        openInvoice.mockReturnValueOnce(firstValueFrom(invoiceResult));
        nativeButton(premiumRoot(harness), starsSubscribe).click();
        await redrawAsync(harness);
        expect(openInvoice).toHaveBeenCalledExactlyOnceWith(session.url);

        await harness.navigateByUrl(OTHER_ROUTE);
        invoiceResult.next('pending');
        invoiceResult.complete();
        await redrawAsync(harness);

        expect(toast.toasts()).toEqual([]);
        expect(refreshSession).not.toHaveBeenCalled();
        expect(readOverview).toHaveBeenCalledOnce();
        expect(router.url).toBe(OTHER_ROUTE);
    });

    it('does not announce pending access when leaving during the direct invoice activation check', async () => {
        const activation = new Subject<BillingOverview>();
        const readOverview = vi
            .fn<PremiumBillingService['getOverview']>()
            .mockReturnValueOnce(of(starsOverview))
            .mockReturnValueOnce(activation);
        const { harness, router, createCheckout, openInvoice, refreshSession, toast } = await renderPremiumAsync(
            PREMIUM_ROUTE,
            readOverview,
            true,
        );
        createCheckout.mockReturnValueOnce(of(session));
        openInvoice.mockResolvedValueOnce('pending');
        nativeButton(premiumRoot(harness), starsSubscribe).click();
        await redrawAsync(harness);
        expect(activation.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        await redrawAsync(harness);
        expect(activation.observed).toBe(false);
        expect(refreshSession).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
        expect(router.url).toBe(OTHER_ROUTE);
    });
});

describe('Premium purchase and trial actions', () => {
    const starsAccount = overview({ availableProviders: ['telegramstars'], telegramStarsPrice: 300 });
    const starsInvoice: CheckoutSessionResponse = {
        sessionId: 'fixture-stars-checkout',
        plan: 'monthly',
        url: 'https://example.test/mock-stars-invoice',
    };
    const subscribeAction = 'PREMIUM_PAGE.STARS.SUBSCRIBE';
    const trialAction = 'PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION';

    it('prevents a trial while the paid invoice awaits confirmation and explains an unconfirmed result', async () => {
        const activation = new Subject<BillingOverview>();
        const readOverview = vi
            .fn<PremiumBillingService['getOverview']>()
            .mockReturnValueOnce(of(starsAccount))
            .mockReturnValueOnce(activation);
        const { harness, createCheckout, openInvoice, refreshSession, startTrial, toast } = await renderPremiumAsync(
            PREMIUM_ROUTE,
            readOverview,
            true,
        );
        createCheckout.mockReturnValue(of(starsInvoice));
        openInvoice.mockResolvedValueOnce('pending');
        startTrial.mockReturnValue(of(activeTrial()));
        const root = premiumRoot(harness);
        const paid = nativeButton(root, subscribeAction);
        const trial = nativeButton(root, trialAction);
        expect(trial.closest('[hidden]')).toBeNull();

        paid.click();
        await redrawAsync(harness);
        expect(openInvoice).toHaveBeenCalledExactlyOnceWith(starsInvoice.url);
        expect(activation.observed).toBe(true);
        trial.click();
        expect(startTrial).not.toHaveBeenCalled();
        expect(trial.disabled).toBe(true);

        activation.error(new Error('Fixture entitlement check temporarily unavailable'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(paid.disabled).toBe(false);
            expect(trial.disabled).toBe(false);
        });
        expect(refreshSession).toHaveBeenCalledOnce();
        expect(toast.toasts().map(item => item.message)).toEqual(['PREMIUM_PAGE.STARS.PENDING']);
        expect(root.textContent).not.toContain('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE');
    });

    it('prevents paid checkout while the trial request is pending and allows checkout after a rejected trial', async () => {
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(starsAccount));
        const { harness, createCheckout, openInvoice, startTrial, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview, true);
        const pendingTrial = new Subject<BillingOverview>();
        startTrial.mockReturnValueOnce(pendingTrial);
        createCheckout.mockReturnValue(of(starsInvoice));
        const root = premiumRoot(harness);
        const paid = nativeButton(root, subscribeAction);
        const trial = nativeButton(root, trialAction);

        trial.click();
        await redrawAsync(harness);
        expect(startTrial).toHaveBeenCalledOnce();
        expect(pendingTrial.observed).toBe(true);
        paid.click();
        expect(createCheckout).not.toHaveBeenCalled();
        expect(paid.disabled).toBe(true);

        pendingTrial.error(new Error('Fixture trial request rejected'));
        await redrawAsync(harness);
        expect(toast.toasts().map(item => item.message)).toContain('PREMIUM_PAGE.ERROR_TRIAL');
        expect(paid.disabled).toBe(false);
        paid.click();
        await redrawAsync(harness);
        expect(createCheckout).toHaveBeenCalledExactlyOnceWith('monthly', 'telegramstars');
        expect(openInvoice).toHaveBeenCalledExactlyOnceWith(starsInvoice.url);
        expect(root.textContent).toContain('PREMIUM_PAGE.BANNERS.CHECKOUT_CANCELED_MESSAGE');
    });
});

describe('Premium billing and trial ownership', () => {
    it('abandons the portal request on departure and does not toast a late failure', async () => {
        const subscriber = overview({ isPremium: true, subscriptionStatus: 'active', manageBillingAvailable: true });
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(subscriber));
        const { harness, router, createPortal, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const pending = new Subject<PortalSessionResponse>();
        createPortal.mockReturnValueOnce(pending);
        const manage = nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.OVERVIEW.MANAGE_ACTION');
        expect(manage.closest('[hidden]')).toBeNull();
        manage.click();
        expect(createPortal).toHaveBeenCalledOnce();
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        const stillPending = pending.observed;
        pending.error(new Error('Portal temporarily unavailable'));
        await redrawAsync(harness);

        expect(toast.toasts()).toEqual([]);
        expect(stillPending).toBe(false);
        expect(router.url).toBe(OTHER_ROUTE);
    });

    it('does not refresh shared access from a trial response after leaving Premium', async () => {
        const { harness, router, startTrial, refreshSession, toast } = await renderPremiumAsync(PREMIUM_ROUTE);
        const pending = new Subject<BillingOverview>();
        startTrial.mockReturnValueOnce(pending);
        nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION').click();
        expect(startTrial).toHaveBeenCalledOnce();
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        const stillPending = pending.observed;
        pending.next(activeTrial());
        pending.complete();
        await redrawAsync(harness);

        expect(refreshSession).not.toHaveBeenCalled();
        expect(stillPending).toBe(false);
        expect(toast.toasts()).toEqual([]);
        expect(router.url).toBe(OTHER_ROUTE);
    });

    it('does not announce the trial after leaving during access refresh', async () => {
        const { harness, router, startTrial, refreshSession, toast } = await renderPremiumAsync(PREMIUM_ROUTE);
        const pendingRefresh = new Subject<boolean>();
        startTrial.mockReturnValueOnce(of(activeTrial()));
        refreshSession.mockReturnValueOnce(pendingRefresh);
        nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION').click();
        await redrawAsync(harness);
        expect(refreshSession).toHaveBeenCalledOnce();
        expect(pendingRefresh.observed).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        pendingRefresh.next(true);
        pendingRefresh.complete();
        await redrawAsync(harness);

        expect(toast.toasts()).toEqual([]);
        expect(router.url).toBe(OTHER_ROUTE);
    });
});

function activeTrial(): BillingOverview {
    return overview({ isPremium: true, premiumTrialActive: true, premiumTrialUsed: true, canStartPremiumTrial: false });
}

async function renderPremiumAsync(
    target: string,
    readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(overview())),
    isTelegram = false,
): Promise<PremiumFixture> {
    const createCheckout = vi.fn<PremiumBillingService['createCheckoutSession']>();
    const createPortal = vi.fn<PremiumBillingService['createPortalSession']>();
    const startTrial = vi.fn<PremiumBillingService['startPremiumTrial']>();
    const openInvoice = vi.fn<TelegramWebAppService['openInvoiceAsync']>().mockResolvedValue('cancelled');
    const openWidget = vi.fn<PaddleCheckoutService['openTransactionCheckoutAsync']>().mockResolvedValue(undefined);
    const previewPrices = vi.fn<PaddleCheckoutService['previewPlanPricesAsync']>().mockResolvedValue({
        monthly: { formattedTotal: PRICED_MONTHLY, currencyCode: 'USD' },
        yearly: { formattedTotal: PRICED_YEARLY, currencyCode: 'USD' },
    });
    const refreshSession = vi.fn<AuthService['refreshSession']>().mockReturnValue(of(true));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'premium', children: premiumRoutes },
                { path: 'other', component: OtherPageComponent },
            ]),
            provideTranslateTesting(),
            {
                provide: PremiumBillingService,
                useValue: {
                    getOverview: readOverview,
                    createCheckoutSession: createCheckout,
                    createPortalSession: createPortal,
                    startPremiumTrial: startTrial,
                },
            },
            { provide: TelegramWebAppService, useValue: { isMiniAppLaunch: (): boolean => isTelegram, openInvoiceAsync: openInvoice } },
            {
                provide: PaddleCheckoutService,
                useValue: { openTransactionCheckoutAsync: openWidget, previewPlanPricesAsync: previewPrices },
            },
            { provide: AuthService, useValue: { isPremium: signal(false), refreshSession } },
        ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('en'));
    const harness = await RouterTestingHarness.create(target);
    await redrawAsync(harness);
    return {
        harness,
        router: TestBed.inject(Router),
        createCheckout,
        createPortal,
        startTrial,
        refreshSession,
        openInvoice,
        openWidget,
        previewPrices,
        toast: TestBed.inject(FdUiToastService),
    };
}

async function redrawAsync(harness: RouterTestingHarness): Promise<void> {
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function premiumRoot(harness: RouterTestingHarness): HTMLElement {
    const root = harness.routeNativeElement;
    if (!(root instanceof HTMLElement)) {
        throw new Error('Expected a rendered Premium screen');
    }
    return root;
}

function nativeButton(root: HTMLElement, label: string): HTMLButtonElement {
    const button = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the Premium action ${label}`);
    }
    return button;
}

function overview(overrides: Partial<BillingOverview> = {}): BillingOverview {
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
        paddleClientToken: 'test_fixture',
        availableProviders: ['paddle'],
        ...overrides,
    };
}
