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
    it('opens self-service management for an unpaid account without Premium access', async () => {
        const fixture = await renderOverviewAsync({ ...activeOverview, isPremium: false, subscriptionStatus: 'unpaid' });
        const manage = vi.fn();
        fixture.componentInstance.manageBilling.subscribe(manage);
        const button = host(fixture).querySelector<HTMLButtonElement>('button');

        expect(button?.textContent).toContain('PREMIUM_PAGE.OVERVIEW.MANAGE_ACTION');
        button?.click();
        expect(manage).toHaveBeenCalledOnce();
        expect(host(fixture).textContent).toContain('PREMIUM_PAGE.STATUS.UNPAID');
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

        expect(text).toContain('PREMIUM_PAGE.OVERVIEW.ENDS_ON');
        expect(text).toContain('Dec 1, 2026');
        expect(text).toContain('PREMIUM_PAGE.OVERVIEW.RENEWAL_OFF');
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
        expect(text).not.toContain('PREMIUM_PAGE.OVERVIEW.SUPPORT_MANAGED');
        expect(text).not.toContain('Paddle');
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
