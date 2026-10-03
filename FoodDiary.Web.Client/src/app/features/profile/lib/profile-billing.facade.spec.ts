import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import type { BillingOverview, PortalSessionResponse } from '../../../shared/models/billing.models';
import { BILLING_ACCOUNT_ACTIONS } from '../../premium/contracts/billing-account-actions';
import { ProfileBillingFacade } from './profile-billing.facade';

describe('Profile billing read lifetime', () => {
    it('cancels a previous refresh before accepting the latest overview', () => {
        const first = new Subject<BillingOverview>();
        const second = new Subject<BillingOverview>();
        const actions = { getOverview: vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(second), createPortalSession: vi.fn() };
        const facade = setup(actions);
        facade.loadBillingOverview();
        facade.reloadBillingOverview();
        expect(first.observed).toBe(false);
        second.next(overview(true));
        first.next(overview(false));
        expect(facade.billingOverview()?.isPremium).toBe(true);
        expect(facade.isLoadingBilling()).toBe(false);
    });

    it('preserves the previous overview during a failed refresh and allows retry', () => {
        const actions = {
            getOverview: vi
                .fn()
                .mockReturnValueOnce(of(overview(true)))
                .mockReturnValueOnce(throwError(() => new Error('unavailable')))
                .mockReturnValue(of(overview(false))),
            createPortalSession: vi.fn(),
        };
        const facade = setup(actions);
        facade.loadBillingOverview();
        facade.reloadBillingOverview();
        expect(facade.billingOverview()?.isPremium).toBe(true);
        expect(facade.billingError()).toBe('USER_MANAGE.BILLING_LOAD_ERROR');
        facade.reloadBillingOverview();
        expect(facade.billingOverview()?.isPremium).toBe(false);
        expect(facade.billingError()).toBeNull();
    });

    it('unsubscribes pending overview and portal requests with its owner', () => {
        const pending = new Subject<BillingOverview>();
        const portal = new Subject<PortalSessionResponse>();
        const facade = setup({ getOverview: vi.fn(() => pending), createPortalSession: vi.fn(() => portal) });
        facade.loadBillingOverview();
        facade.openBillingPortal();
        expect(pending.observed).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(portal.observed).toBe(false);
    });
});

function setup(actions: { getOverview: ReturnType<typeof vi.fn>; createPortalSession: ReturnType<typeof vi.fn> }): ProfileBillingFacade {
    TestBed.configureTestingModule({
        providers: [
            ProfileBillingFacade,
            { provide: BILLING_ACCOUNT_ACTIONS, useValue: actions },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            { provide: FdUiToastService, useValue: { error: vi.fn() } },
        ],
    });
    return TestBed.inject(ProfileBillingFacade);
}

function overview(isPremium: boolean): BillingOverview {
    return {
        isPremium,
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
        canStartPremiumTrial: false,
        provider: 'test',
        paddleClientToken: null,
        availableProviders: [],
    };
}
