import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../../services/localization.service';
import type { BillingOverview } from '../../../../premium/models/billing.models';
import type { BillingViewModel } from '../../user-manage/user-manage.types';
import { UserManageBillingSummaryComponent } from './user-manage-billing-summary.component';

const BILLING_OVERVIEW: BillingOverview = {
    isPremium: true,
    subscriptionStatus: 'active',
    plan: 'monthly',
    subscriptionProvider: 'stripe',
    currentPeriodStartUtc: '2026-01-01T00:00:00Z',
    currentPeriodEndUtc: '2026-02-01T00:00:00Z',
    nextBillingAttemptUtc: '2026-02-01T00:00:00Z',
    cancelAtPeriodEnd: false,
    renewalEnabled: true,
    manageBillingAvailable: true,
    provider: 'stripe',
    paddleClientToken: null,
    availableProviders: ['stripe'],
};

describe('UserManageBillingSummaryComponent state', () => {
    it('derives billing labels from overview', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;

        expect(component.billingPlanLabelKey()).toBe('USER_MANAGE.BILLING_PLAN_PREMIUM_MONTHLY');
        expect(component.billingStatusLabelKey()).toBe('USER_MANAGE.BILLING_STATUS_ACTIVE');
        expect(component.billingProviderLabel()).toBe('Stripe');
        expect(component.billingRenewalLabelKey()).toBe('USER_MANAGE.BILLING_RENEWAL_ENABLED');
    });

    it('formats billing dates with current language', async () => {
        const fixture = await createComponentAsync();

        expect(fixture.componentInstance.formatDate('2026-01-01T00:00:00Z')).toBe('Jan 1, 2026');
    });
});

describe('UserManageBillingSummaryComponent actions', () => {
    it('emits billing actions', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const billingPortalOpen = vi.fn();
        const premiumPageOpen = vi.fn();
        component.billingPortalOpen.subscribe(billingPortalOpen);
        component.premiumPageOpen.subscribe(premiumPageOpen);

        component.billingPortalOpen.emit();
        component.premiumPageOpen.emit();

        expect(billingPortalOpen).toHaveBeenCalledOnce();
        expect(premiumPageOpen).toHaveBeenCalledOnce();
    });
});

async function createComponentAsync(
    overrides: Partial<BillingSummaryInputs> = {},
): Promise<ComponentFixture<UserManageBillingSummaryComponent>> {
    const localizationService = { getCurrentLanguage: vi.fn(() => 'en') };

    await TestBed.configureTestingModule({
        imports: [UserManageBillingSummaryComponent, TranslateModule.forRoot()],
        providers: [{ provide: LocalizationService, useValue: localizationService }],
    }).compileComponents();

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    const fixture = TestBed.createComponent(UserManageBillingSummaryComponent);
    fixture.componentRef.setInput('billing', overrides.billing ?? createBillingView());
    fixture.componentRef.setInput('isOpeningBillingPortal', overrides.isOpeningBillingPortal ?? false);
    fixture.detectChanges();
    return fixture;
}

function createBillingView(): BillingViewModel {
    return {
        overview: BILLING_OVERVIEW,
        statusTone: 'success',
        endLabelKey: 'USER_MANAGE.BILLING_PERIOD_END',
        showNextAttempt: true,
        premiumActionVariant: 'secondary',
        premiumActionLabelKey: 'USER_MANAGE.BILLING_VIEW_PREMIUM',
        showManagedSupportNote: false,
    };
}

type BillingSummaryInputs = {
    billing: BillingViewModel;
    isOpeningBillingPortal: boolean;
};
