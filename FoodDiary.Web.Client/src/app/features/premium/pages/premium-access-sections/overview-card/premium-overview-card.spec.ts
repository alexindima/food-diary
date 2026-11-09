import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { BillingOverview } from '../../../../../shared/models/billing.models';
import { buildPremiumOverviewCardViewModel } from '../../premium-access/premium-access-lib/premium-access-view.mapper';
import { PremiumOverviewCardComponent } from './premium-overview-card';

const activeOverview: BillingOverview = {
    isPremium: true,
    subscriptionStatus: 'active',
    plan: 'monthly',
    subscriptionProvider: 'paddle',
    currentPeriodStartUtc: '2026-11-01T00:00:00Z',
    currentPeriodEndUtc: '2026-12-01T00:00:00Z',
    nextBillingAttemptUtc: '2026-12-01T00:00:00Z',
    cancelAtPeriodEnd: false,
    renewalEnabled: true,
    manageBillingAvailable: true,
    premiumTrialStartUtc: null,
    premiumTrialEndUtc: null,
    premiumTrialActive: false,
    premiumTrialUsed: false,
    canStartPremiumTrial: false,
    provider: 'paddle',
    paddleClientToken: null,
    availableProviders: ['paddle'],
};

describe('Subscription overview actions', () => {
    it.each([
        { subscriptionStatus: 'unpaid', isPremium: false, hint: 'PAYMENT_ISSUE_WITHOUT_ACCESS' },
        { subscriptionStatus: 'past_due', isPremium: true, hint: 'PAYMENT_ISSUE_WITH_ACCESS' },
    ])('offers payment repair for $subscriptionStatus with Premium access=$isPremium', async ({ hint, ...state }) => {
        const fixture = await renderOverviewAsync({ ...activeOverview, ...state });
        const manage = vi.fn();
        fixture.componentInstance.manageBilling.subscribe(manage);
        const button = host(fixture).querySelector<HTMLButtonElement>('fd-ui-button:not([hidden]) button');

        expect(button?.textContent).toContain('PREMIUM_PAGE.OVERVIEW.FIX_PAYMENT_ACTION');
        button?.click();
        expect(manage).toHaveBeenCalledOnce();
        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.STATUS.PAYMENT_ISSUE');
        expect(host(fixture).textContent).toContain(`PREMIUM_PAGE.OVERVIEW.${hint}`);
    });

    it('explains how to change access managed outside the portal', async () => {
        const fixture = await renderOverviewAsync({ ...activeOverview, manageBillingAvailable: false, renewalEnabled: false });

        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.OVERVIEW.SUPPORT_MANAGED');
        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.OVERVIEW.RENEWAL_NONE');
    });

    it('does not expose stale actions while the overview is reloading', async () => {
        const fixture = await renderOverviewAsync(activeOverview);
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();

        expect(host(fixture).querySelector('button')).toBeNull();
        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.STATUS_LOADING');
    });
});

describe('Subscription renewal details', () => {
    it('shows the access end and hides future charge attempts when renewal is canceled', async () => {
        const fixture = await renderOverviewAsync({ ...activeOverview, cancelAtPeriodEnd: true, renewalEnabled: false });
        const text = host(fixture).textContent;

        expect(text).toContain('PREMIUM_PAGE.OVERVIEW.ACCESS_UNTIL');
        expect(text).toContain('Dec 1, 2026');
        expect(text).toContain('PREMIUM_PAGE.BANNERS.CANCEL_AT_PERIOD_END_MESSAGE');
        expect(text).not.toContain('PREMIUM_PAGE.OVERVIEW.RENEWAL_OFF');
        expect(text).not.toContain('PREMIUM_PAGE.OVERVIEW.PERIOD');
        expect(text).not.toContain('PREMIUM_PAGE.OVERVIEW.NEXT_CHARGE');
    });

    it('describes a card-free trial without automatic charges or a support cancellation request', async () => {
        const fixture = await renderOverviewAsync({
            ...activeOverview,
            premiumTrialActive: true,
            subscriptionProvider: null,
            manageBillingAvailable: false,
            renewalEnabled: false,
        });
        const text = host(fixture).textContent;

        expect(text).toContain('PREMIUM_PAGE.OVERVIEW.RENEWAL_TRIAL');
        expect(text).toContain('PREMIUM_PAGE.OVERVIEW.TRIAL_ENDS_ON');
        expect(text).toContain('PREMIUM_PAGE.STATUS.TRIALING');
        expect(text).not.toContain('PREMIUM_PAGE.OVERVIEW.SUPPORT_MANAGED');
        expect(text).not.toContain('Paddle');
    });

    it('keeps paid access active with a quiet renewal-off message after cancellation', async () => {
        const fixture = await renderOverviewAsync({
            ...activeOverview,
            subscriptionStatus: 'canceled',
            cancelAtPeriodEnd: true,
            renewalEnabled: false,
        });
        const badge = host(fixture).querySelector('.fd-ui-pill');

        expect(badge?.textContent).toContain('PREMIUM_PAGE.STATUS.ACCESS_ACTIVE');
        expect(badge?.classList.contains('fd-ui-pill--success')).toBe(true);
        expect(host(fixture).querySelector('.premium-access-page__renewal-note')?.textContent).toContain(
            'PREMIUM_PAGE.BANNERS.CANCEL_AT_PERIOD_END_MESSAGE',
        );
        expect(host(fixture).querySelector('fd-notice-banner')).toBeNull();
    });

    it.each([
        { subscriptionStatus: 'canceled', subscriptionProvider: 'paddle', manageBillingAvailable: true },
        { subscriptionStatus: 'active', subscriptionProvider: 'TelegramStars', manageBillingAvailable: false },
    ])('shows expired access for $subscriptionProvider with billing status $subscriptionStatus', async subscription => {
        const fixture = await renderOverviewAsync({
            ...activeOverview,
            ...subscription,
            isPremium: false,
            cancelAtPeriodEnd: true,
            renewalEnabled: false,
            nextBillingAttemptUtc: null,
        });

        const resumeButton = host(fixture).querySelector<HTMLButtonElement>('fd-ui-button[hidden] button');
        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.OVERVIEW.SUBSCRIPTION_ENDED');
        expect(host(fixture).querySelector('.fd-ui-pill')?.textContent).toContain('PREMIUM_PAGE.STATUS.EXPIRED');
        expect(host(fixture).textContent).not.toContain('PREMIUM_PAGE.OVERVIEW.ACCESS_UNTIL');
        expect(host(fixture).querySelector('.premium-access-page__renewal-note')).toBeNull();
        expect(host(fixture).querySelector('.fd-ui-pill--success')).toBeNull();
        expect(resumeButton?.textContent).toContain('PREMIUM_PAGE.OVERVIEW.RESUME_ACTION');

        const resume = vi.fn();
        fixture.componentInstance.resumeSubscription.subscribe(resume);
        fixture.componentRef.setInput('plansAvailable', true);
        fixture.detectChanges();
        host(fixture).querySelector<HTMLButtonElement>('fd-ui-button:not([hidden]) button')?.click();
        expect(resume).toHaveBeenCalledOnce();
    });
});

async function renderOverviewAsync(overview: BillingOverview): Promise<ComponentFixture<PremiumOverviewCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [PremiumOverviewCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(PremiumOverviewCardComponent);
    const viewModel = buildPremiumOverviewCardViewModel(overview, overview.isPremium, true, {
        periodEnd: 'Dec 1, 2026',
        periodStart: 'Nov 1, 2026',
        nextBillingAttempt: 'Dec 1, 2026',
    });
    for (const [name, value] of Object.entries({ overview, viewModel, portalLoading: false, trialLoading: false, isLoading: false })) {
        fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture;
}

function host(fixture: ComponentFixture<PremiumOverviewCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
