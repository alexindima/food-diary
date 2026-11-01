import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import type { BillingOverview } from '../../../../shared/models/billing.models';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { PremiumBillingFacade } from '../../lib/premium-billing.facade';
import type { CancellationConfirmation, CancellationFeedback } from '../../models/cancellation.models';
import { PremiumCancellationActionsComponent } from './premium-cancellation-actions';

const cancel = vi.fn<PremiumBillingFacade['cancelSubscription']>();
const feedback = vi.fn<PremiumBillingFacade['submitCancellationFeedback']>();
const dialog = { open: vi.fn() };
const toast = { success: vi.fn(), info: vi.fn(), error: vi.fn() };
const overview: BillingOverview = {
    isPremium: true,
    subscriptionStatus: 'active',
    plan: 'monthly',
    subscriptionProvider: 'Paddle',
    currentPeriodStartUtc: '2026-11-01T10:00:00Z',
    currentPeriodEndUtc: '2026-12-01T10:00:00Z',
    nextBillingAttemptUtc: '2026-12-01T10:00:00Z',
    cancelAtPeriodEnd: false,
    renewalEnabled: true,
    manageBillingAvailable: true,
    cancellationAvailable: true,
    premiumTrialStartUtc: null,
    premiumTrialEndUtc: null,
    premiumTrialActive: false,
    premiumTrialUsed: true,
    canStartPremiumTrial: false,
    provider: 'Paddle',
    paddleClientToken: null,
    availableProviders: ['Paddle'],
};
const confirmed: CancellationConfirmation = {
    cancellationId: entityId<'billing-cancellation'>('owned-cancellation'),
    overview: {
        ...overview,
        cancellationAvailable: false,
        cancelAtPeriodEnd: true,
        renewalEnabled: false,
        nextBillingAttemptUtc: null,
    },
};

beforeEach(() => {
    vi.resetAllMocks();
    TestBed.configureTestingModule({
        providers: [
            { provide: PremiumBillingFacade, useValue: { cancelSubscription: cancel, submitCancellationFeedback: feedback } },
            { provide: FdUiDialogService, useValue: dialog },
            { provide: FdUiToastService, useValue: toast },
            {
                provide: TranslateService,
                useValue: {
                    instant: (key: string): string => key,
                    getCurrentLang: (): string => 'en',
                    getFallbackLang: (): string => 'en',
                },
            },
            { provide: AuthService, useValue: { refreshSession: (): Observable<boolean> => of(true) } },
        ],
    }).overrideComponent(PremiumCancellationActionsComponent, { set: { template: '', imports: [] } });
    dialog.open.mockReturnValue({ afterClosed: () => of({ reasonCode: null, comment: null }) });
    cancel.mockReturnValue(of(confirmed));
    feedback.mockReturnValue(of(undefined));
});

function setup(): { component: PremiumCancellationActionsComponent; changed: ReturnType<typeof vi.fn> } {
    const fixture = TestBed.createComponent(PremiumCancellationActionsComponent);
    fixture.componentRef.setInput('overview', overview);
    const changed = vi.fn();
    fixture.componentInstance.overviewChanged.subscribe(changed);
    return { component: fixture.componentInstance, changed };
}

describe('Premium cancellation', () => {
    it('cancels with the survey skipped and sends no feedback request', async () => {
        const { component, changed } = setup();
        await component['cancelAsync']();
        expect(cancel).toHaveBeenCalledOnce();
        expect(feedback).not.toHaveBeenCalled();
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        expect(toast.success).toHaveBeenCalledOnce();
    });

    it('publishes confirmed cancellation before a feedback failure and preserves success', async () => {
        const draft: CancellationFeedback = { reasonCode: 'too_expensive', comment: 'Price' };
        dialog.open.mockReturnValue({ afterClosed: () => of(draft) });
        feedback.mockReturnValue(throwError(() => new Error('Feedback unavailable')));
        const { component, changed } = setup();
        await component['cancelAsync']();
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        expect(feedback).toHaveBeenCalledWith(confirmed.cancellationId, draft);
        expect(cancel.mock.invocationCallOrder[0]).toBeLessThan(feedback.mock.invocationCallOrder[0] ?? Infinity);
        expect(toast.success).toHaveBeenCalledOnce();
        expect(toast.info).toHaveBeenCalledWith('PREMIUM_PAGE.CANCELLATION.FEEDBACK_ERROR');
        expect(toast.error).not.toHaveBeenCalled();
    });

    it('lets confirmation complete while the optional feedback request is still pending', async () => {
        const pendingFeedback = new Subject<unknown>();
        dialog.open.mockReturnValue({ afterClosed: () => of({ reasonCode: null, comment: 'Optional comment' }) });
        feedback.mockReturnValue(pendingFeedback);
        const { component, changed } = setup();
        await component['cancelAsync']();
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        expect(component['loading']()).toBe(false);
        pendingFeedback.complete();
    });

    it('does nothing when confirmation is dismissed', async () => {
        dialog.open.mockReturnValue({ afterClosed: () => of(undefined) });
        const { component } = setup();
        await component['cancelAsync']();
        expect(cancel).not.toHaveBeenCalled();
        expect(feedback).not.toHaveBeenCalled();
    });

    it('does not claim success or save feedback when cancellation fails', async () => {
        cancel.mockReturnValue(throwError(() => new Error('Paddle unavailable')));
        const { component, changed } = setup();
        await component['cancelAsync']();
        expect(changed).not.toHaveBeenCalled();
        expect(feedback).not.toHaveBeenCalled();
        expect(toast.success).not.toHaveBeenCalled();
        expect(toast.error).toHaveBeenCalledOnce();
    });
});
