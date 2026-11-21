import { Location } from '@angular/common';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { filter, firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import type { BillingOverviewHttpResponse } from '../../shared/api/sdk/generated/model/billing-overview-http-response';
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
const BILLING_URL = environment.apiUrls.billing;
const CANCELLATION_KEY = 'PREMIUM_PAGE.CANCELLATION';

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
        expect(openWidget).toHaveBeenCalledExactlyOnceWith(
            RETURNED_TRANSACTION,
            { token: 'test_fixture', environment: 'sandbox', locale: 'en' },
            expect.objectContaining({ destroyed: false }),
        );
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

describe('Premium deferred Paddle checkout', () => {
    it('opens only the fresh checkout after going back while Paddle loads', async () => {
        const api = createDeferredPaddleFixture();
        TestBed.overrideProvider(PaddleCheckoutService, { useFactory: (): PaddleCheckoutService => new PaddleCheckoutService(), deps: [] });
        const { harness, router, createCheckout, toast } = await renderPremiumAsync(OTHER_ROUTE);
        router.setUpLocationChangeListener();

        await harness.navigateByUrl(TRANSACTION_ROUTE);
        await redrawAsync(harness);
        expect(router.url).toBe(TRANSACTION_ROUTE);
        expect(api.waitingForLoad()).toBe(true);
        expect(api.open).not.toHaveBeenCalled();

        const returned = firstValueFrom(
            router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === OTHER_ROUTE)),
        );
        TestBed.inject(Location).back();
        await returned;
        expect(harness.routeNativeElement?.textContent).toContain('Another page');

        api.completeLoad();
        await redrawAsync(harness);
        expect(api.initialize).toHaveBeenCalledOnce();
        expect(api.open).not.toHaveBeenCalled();
        expect(router.url).toBe(OTHER_ROUTE);
        expect(toast.toasts()).toEqual([]);

        const nextTransaction = 'txn_fixture_return_next';
        await harness.navigateByUrl(`${PREMIUM_ROUTE}?_ptxn=${nextTransaction}`);
        await vi.waitFor(async () => {
            await redrawAsync(harness);
            expect(api.open).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ transactionId: nextTransaction }));
            expect(router.url).toBe(PREMIUM_ROUTE);
        });
        expect(api.initialize).toHaveBeenCalledOnce();
        expect(createCheckout).not.toHaveBeenCalled();
    });
});

type PaddleApi = NonNullable<Window['Paddle']>;

function createDeferredPaddleFixture(): {
    waitingForLoad: () => boolean;
    initialize: Mock<PaddleApi['Initialize']>;
    open: Mock<PaddleApi['Checkout']['open']>;
    completeLoad: () => void;
} {
    const script = document.createElement('script');
    script.type = 'text/plain';
    script.src = 'https://cdn.paddle.com/paddle/v2/paddle.js';
    document.head.append(script);
    const listen = vi.spyOn(script, 'addEventListener');
    const initialize = vi.fn<PaddleApi['Initialize']>();
    const open = vi.fn<PaddleApi['Checkout']['open']>();
    const previous = window.Paddle;
    Reflect.deleteProperty(window, 'Paddle');
    onTestFinished(() => {
        script.remove();
        if (previous === undefined) {
            Reflect.deleteProperty(window, 'Paddle');
        } else {
            window.Paddle = previous;
        }
    });
    const completeLoad = (): void => {
        window.Paddle = { Environment: { set: vi.fn<PaddleApi['Environment']['set']>() }, Initialize: initialize, Checkout: { open } };
        script.dispatchEvent(new Event('load'));
    };
    const waitingForLoad = (): boolean => listen.mock.calls.some(([type]) => type === 'load');
    return { waitingForLoad, initialize, open, completeLoad };
}

describe('Premium activation return ownership', () => {
    it('clears an unfamiliar checkout return code and renders normal Premium without an activation result', async () => {
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(overview()));
        const { harness, router, refreshSession, createCheckout, toast } = await renderPremiumAsync(
            '/premium?checkout=unrecognized',
            readOverview,
        );
        await vi.waitFor(async () => {
            await redrawAsync(harness);
            expect(readOverview).toHaveBeenCalledOnce();
            expect(premiumRoot(harness).querySelector(PLANS)).not.toBeNull();
        });
        const root = premiumRoot(harness);

        expect(router.url).toBe(PREMIUM_ROUTE);
        expect(root.querySelector('fd-premium-access-banners fd-notice-banner')).toBeNull();
        expect(toast.toasts()).toEqual([]);
        expect(refreshSession).not.toHaveBeenCalled();
        expect(createCheckout).not.toHaveBeenCalled();
    });

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
    it('starts the Premium help tour again after the user completed it', async () => {
        const { harness, createCheckout } = await renderPremiumAsync(PREMIUM_ROUTE);
        const tour = TestBed.inject(FdTourService);
        const help = premiumRoot(harness).querySelector<HTMLButtonElement>('button[aria-label="PREMIUM_PAGE.TOUR.START_TOUR"]');
        expect(help).not.toBeNull();
        try {
            help?.click();
            expect(tour.activeTour()?.id).toBe('premium-access');
            expect(tour.activeStep()?.id).toBe('overview');
            tour.complete();
            expect(tour.activeTour()).toBeNull();

            help?.click();
            expect(tour.activeTour()?.id).toBe('premium-access');
            expect(tour.activeStep()?.id).toBe('overview');
            expect(createCheckout).not.toHaveBeenCalled();
        } finally {
            tour.close();
            tour.resetCompletion('premium-access');
        }
    });

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

it('shows the configured YooKassa plans when the account cannot start a trial', async () => {
    const account = overview({
        canStartPremiumTrial: false,
        provider: 'yookassa',
        paddleClientToken: null,
        availableProviders: ['yookassa'],
    });
    const { harness, openWidget, previewPrices } = await renderPremiumAsync(PREMIUM_ROUTE, undefined, false, account);
    const http = TestBed.inject(HttpTestingController);
    const root = premiumRoot(harness);
    expect(root.textContent).toContain('PREMIUM_PAGE.OVERVIEW.CHECKOUT_HINT');
    const checkout = nativeButton(root, 'PREMIUM_PAGE.PLANS.MONTHLY.ACTION');
    expect(checkout.disabled).toBe(false);
    checkout.click();

    const request = http.expectOne({ method: 'POST', url: `${BILLING_URL}/checkout-session` });
    expect(request.request.body).toEqual({ plan: 'monthly', provider: 'yookassa' });
    request.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    await redrawAsync(harness);

    expect(root.textContent).toContain('PREMIUM_PAGE.ERROR_CHECKOUT');
    expect(checkout.disabled).toBe(false);
    expect(openWidget).not.toHaveBeenCalled();
    expect(previewPrices).not.toHaveBeenCalled();
    http.verify();
});

describe('Premium payment recovery details', () => {
    it('relocalizes outstanding billing dates and opens payment help without starting a billing action', async () => {
        const periodStart = '2026-11-04T12:00:00Z';
        const retryDate = '2026-11-18T12:00:00Z';
        const account = overview({
            subscriptionStatus: 'past_due',
            subscriptionProvider: 'paddle',
            currentPeriodStartUtc: periodStart,
            currentPeriodEndUtc: null,
            nextBillingAttemptUtc: retryDate,
            renewalEnabled: true,
            canStartPremiumTrial: false,
            availableProviders: [],
        });
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(account));
        const { harness, createCheckout, createPortal, openWidget, startTrial } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const root = premiumRoot(harness);
        const details = root.querySelector('.premium-access-page__details');
        const terms = root.querySelector<HTMLElement>('#premium-subscription-terms');
        const disclosure = root.querySelector<HTMLButtonElement>('button[aria-controls="premium-subscription-terms"]');
        if (details === null || terms === null || disclosure === null) {
            throw new Error('Expected the dates and support controls on the Premium route');
        }
        const english = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium' });
        const russian = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'medium' });
        const billingDates = [new Date(periodStart), new Date(retryDate)];
        expect(details.textContent).toContain('PREMIUM_PAGE.OVERVIEW.PERIOD_START');
        expect(details.textContent).toContain('PREMIUM_PAGE.OVERVIEW.NEXT_CHARGE');
        for (const date of billingDates) {
            expect(details.textContent).toContain(english.format(date));
        }

        await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
        await redrawAsync(harness);
        for (const date of billingDates) {
            expect(details.textContent).toContain(russian.format(date));
            expect(details.textContent).not.toContain(english.format(date));
        }
        expect(readOverview).toHaveBeenCalledOnce();

        expect(terms.hidden).toBe(true);
        const help = nativeButton(root, 'PREMIUM_PAGE.OVERVIEW.PAYMENT_HELP_ACTION');
        expect(help.closest('[hidden]')).toBeNull();
        help.click();
        await redrawAsync(harness);
        expect(terms.hidden).toBe(false);
        expect(disclosure.getAttribute('aria-expanded')).toBe('true');
        expect(document.activeElement).toBe(disclosure);
        expect(createCheckout).not.toHaveBeenCalled();
        expect(createPortal).not.toHaveBeenCalled();
        expect(startTrial).not.toHaveBeenCalled();
        expect(openWidget).not.toHaveBeenCalled();
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

describe('Premium billing error recovery', () => {
    it('handles an empty portal URL and enables the same billing action for a successful retry', async () => {
        const subscriber = overview({ isPremium: true, subscriptionStatus: 'active', manageBillingAvailable: true });
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(subscriber));
        const { harness, createPortal, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const root = premiumRoot(harness);
        const manage = nativeButton(root, 'PREMIUM_PAGE.OVERVIEW.MANAGE_ACTION');
        const portalResponse = new Subject<PortalSessionResponse>();
        const startingUrl = document.location.href;
        const destination = new URL(startingUrl);
        destination.hash = 'fixture-billing-portal';
        createPortal.mockReturnValueOnce(portalResponse).mockReturnValueOnce(of({ url: destination.href }));

        manage.click();
        await redrawAsync(harness);
        expect(portalResponse.observed).toBe(true);
        expect(manage.disabled).toBe(true);

        portalResponse.next({ url: '' });
        portalResponse.complete();
        await redrawAsync(harness);
        expect(root.textContent).toContain('PREMIUM_PAGE.ERROR_PORTAL');
        expect(toast.toasts().map(item => item.message)).toEqual(['PREMIUM_PAGE.ERROR_PORTAL']);
        expect(manage.disabled).toBe(false);
        expect(document.location.href).toBe(startingUrl);

        try {
            manage.click();
            await redrawAsync(harness);
            expect(createPortal).toHaveBeenCalledTimes(2);
            expect(document.location.href).toBe(destination.href);
            expect(root.textContent).not.toContain('PREMIUM_PAGE.ERROR_PORTAL');
        } finally {
            history.replaceState(history.state, '', startingUrl);
        }
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

describe('Premium cancellation on the billing route', () => {
    it('keeps renewal disabled after confirmation even if feedback and session refresh fail', async () => {
        const active = cancellableSubscription();
        const { harness, refreshSession, toast } = await renderPremiumCancellationAsync(active);
        refreshSession.mockReturnValueOnce(throwError(() => new DOMException('Cookie session lock unavailable', 'SecurityError')));
        const http = TestBed.inject(HttpTestingController);
        const root = premiumRoot(harness);
        const action = nativeButton(root, `${CANCELLATION_KEY}.ACTION`);
        expect(action.closest('[hidden]')).toBeNull();
        action.click();
        action.click();
        await redrawAsync(harness);

        const dialog = cancellationDialog();
        expect(document.body.querySelectorAll('fd-premium-cancel-dialog')).toHaveLength(1);
        const comment = dialog.querySelector('textarea');
        if (comment === null) {
            throw new Error('Expected the optional cancellation feedback field');
        }
        comment.value = '  Prefer to renew later  ';
        comment.dispatchEvent(new Event('input', { bubbles: true }));
        await redrawAsync(harness);
        nativeButton(dialog, `${CANCELLATION_KEY}.CONFIRM`).click();
        await redrawAsync(harness);

        const write = http.expectOne({ method: 'POST', url: `${BILLING_URL}/cancel` });
        expect(write.request.body).toBeNull();
        expect(action.disabled).toBe(true);
        expect(root.querySelector('.premium-access-page__renewal-note')).toBeNull();
        write.flush({
            cancellationId: 'local-cancellation',
            overview: { ...active, cancellationAvailable: false, renewalEnabled: false, cancelAtPeriodEnd: true },
        });
        await redrawAsync(harness);

        const feedback = http.expectOne({ method: 'POST', url: `${BILLING_URL}/cancellations/local-cancellation/feedback` });
        expect(feedback.request.body).toEqual({ reasonCode: null, comment: 'Prefer to renew later' });
        expect(refreshSession).toHaveBeenCalledOnce();
        expect(root.querySelector('.premium-access-page__renewal-note')?.textContent).toContain(
            'PREMIUM_PAGE.BANNERS.CANCEL_AT_PERIOD_END_MESSAGE',
        );
        expect(action.isConnected).toBe(false);
        feedback.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await redrawAsync(harness);

        expect(toast.toasts().map(item => item.message)).toEqual([`${CANCELLATION_KEY}.SUCCESS`, `${CANCELLATION_KEY}.FEEDBACK_ERROR`]);
        expect(root.querySelector('.premium-access-page__renewal-note')).not.toBeNull();
        http.verify();
    });

    it('closes an unconfirmed cancellation when leaving its billing route', async () => {
        const { harness, router, refreshSession, toast } = await renderPremiumCancellationAsync(cancellableSubscription());
        const http = TestBed.inject(HttpTestingController);
        nativeButton(premiumRoot(harness), `${CANCELLATION_KEY}.ACTION`).click();
        await redrawAsync(harness);
        expect(cancellationDialog().isConnected).toBe(true);

        await harness.navigateByUrl(OTHER_ROUTE);
        await redrawAsync(harness);

        expect(router.url).toBe(OTHER_ROUTE);
        expect(harness.routeNativeElement?.textContent).toContain('Another page');
        expect(document.body.querySelector('fd-premium-cancel-dialog')).toBeNull();
        http.expectNone({ method: 'POST', url: `${BILLING_URL}/cancel` });
        expect(refreshSession).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
        http.verify();
    });
});

async function renderPremiumCancellationAsync(active: BillingOverview): Promise<PremiumFixture> {
    stubScrollIntoView();
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    // RouterTestingHarness owns its location separately from the dialog's browser history entry.
    const pushHistory = vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
    onTestFinished(() => {
        pushHistory.mockRestore();
        vi.unstubAllGlobals();
    });
    return renderPremiumAsync(PREMIUM_ROUTE, undefined, false, active);
}

function cancellableSubscription(): BillingOverview {
    return overview({
        isPremium: true,
        subscriptionProvider: 'paddle',
        subscriptionStatus: 'active',
        plan: 'monthly',
        currentPeriodEndUtc: '2026-12-01T10:00:00Z',
        canStartPremiumTrial: false,
        manageBillingAvailable: true,
        cancellationAvailable: true,
        renewalEnabled: true,
    });
}

function cancellationDialog(): HTMLElement {
    const dialog = document.body.querySelector<HTMLElement>('fd-premium-cancel-dialog');
    if (dialog === null) {
        throw new Error('Expected the visible Premium cancellation dialog');
    }
    return dialog;
}

function activeTrial(): BillingOverview {
    return overview({ isPremium: true, premiumTrialActive: true, premiumTrialUsed: true, canStartPremiumTrial: false });
}

describe('Premium actions when the host is destroyed after a response', () => {
    it('does not open a Stars invoice once the checkout view has been destroyed', async () => {
        const readOverview = vi
            .fn<PremiumBillingService['getOverview']>()
            .mockReturnValue(of(overview({ availableProviders: ['telegramstars'], telegramStarsPrice: 300 })));
        const { harness, createCheckout, openInvoice, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview, true);
        const response = new Subject<CheckoutSessionResponse>();
        createCheckout.mockReturnValueOnce(response);
        nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.STARS.SUBSCRIBE').click();
        expect(createCheckout).toHaveBeenCalledExactlyOnceWith('monthly', 'telegramstars');
        expect(response.observed).toBe(true);

        response.next({ sessionId: 'fixture-session', plan: 'monthly', url: 'https://example.test/stars-invoice' });
        expect(response.observed).toBe(false);
        harness.fixture.destroy();
        await waitForAsyncTasksAsync();

        expect(openInvoice).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
    });

    it('does not redirect to the billing portal once the account view has been destroyed', async () => {
        const subscriber = overview({ isPremium: true, subscriptionStatus: 'active', manageBillingAvailable: true });
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(subscriber));
        const { harness, createPortal, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        const response = new Subject<PortalSessionResponse>();
        const startingUrl = document.location.href;
        const destination = new URL(startingUrl);
        destination.hash = 'fixture-departed-billing';
        createPortal.mockReturnValueOnce(response);
        nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.OVERVIEW.MANAGE_ACTION').click();
        expect(createPortal).toHaveBeenCalledOnce();
        expect(response.observed).toBe(true);

        try {
            response.next({ url: destination.href });
            expect(response.observed).toBe(false);
            harness.fixture.destroy();
            await waitForAsyncTasksAsync();

            expect(document.location.href).toBe(startingUrl);
            expect(toast.toasts()).toEqual([]);
        } finally {
            history.replaceState(history.state, '', startingUrl);
        }
    });

    it('does not refresh shared access or announce a trial after the trial view is destroyed', async () => {
        const { harness, startTrial, refreshSession, previewPrices, toast } = await renderPremiumAsync(PREMIUM_ROUTE);
        const response = new Subject<BillingOverview>();
        startTrial.mockReturnValueOnce(response);
        previewPrices.mockClear();
        nativeButton(premiumRoot(harness), 'PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION').click();
        expect(startTrial).toHaveBeenCalledOnce();
        expect(response.observed).toBe(true);

        response.next(activeTrial());
        expect(response.observed).toBe(false);
        harness.fixture.destroy();
        await waitForAsyncTasksAsync();

        expect(refreshSession).not.toHaveBeenCalled();
        expect(previewPrices).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
    });

    it('does not refresh shared access from an overview received just before the host closes', async () => {
        const response = new Subject<BillingOverview>();
        const readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValueOnce(response);
        const { harness, refreshSession, previewPrices, toast } = await renderPremiumAsync(PREMIUM_ROUTE, readOverview);
        expect(premiumRoot(harness).textContent).toContain('PREMIUM_PAGE.STATUS_LOADING');
        expect(response.observed).toBe(true);

        response.next(overview({ isPremium: true, subscriptionStatus: 'active' }));
        expect(response.observed).toBe(false);
        harness.fixture.destroy();
        await waitForAsyncTasksAsync();

        expect(refreshSession).not.toHaveBeenCalled();
        expect(previewPrices).not.toHaveBeenCalled();
        expect(toast.toasts()).toEqual([]);
    });
});

async function renderPremiumAsync(
    target: string,
    readOverview = vi.fn<PremiumBillingService['getOverview']>().mockReturnValue(of(overview())),
    isTelegram = false,
    billingHttpResponse?: BillingOverviewHttpResponse,
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
    const billingProviders =
        billingHttpResponse === undefined
            ? [
                  {
                      provide: PremiumBillingService,
                      useValue: {
                          getOverview: readOverview,
                          createCheckoutSession: createCheckout,
                          createPortalSession: createPortal,
                          startPremiumTrial: startTrial,
                      },
                  },
              ]
            : [provideHttpClient(), provideHttpClientTesting()];
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'premium', children: premiumRoutes },
                { path: 'other', component: OtherPageComponent },
            ]),
            provideTranslateTesting(),
            ...billingProviders,
            { provide: TelegramWebAppService, useValue: { isMiniAppLaunch: (): boolean => isTelegram, openInvoiceAsync: openInvoice } },
            {
                provide: PaddleCheckoutService,
                useValue: { openTransactionCheckoutAsync: openWidget, previewPlanPricesAsync: previewPrices },
            },
            { provide: AuthService, useValue: { isPremium: signal(billingHttpResponse?.isPremium ?? false), refreshSession } },
        ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('en'));
    const harness = await RouterTestingHarness.create(target);
    if (billingHttpResponse !== undefined) {
        const http = TestBed.inject(HttpTestingController);
        http.expectOne({ method: 'GET', url: `${BILLING_URL}/overview` }).flush(billingHttpResponse);
    }
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
