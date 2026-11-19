import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import type { BillingOverview } from '../../../../shared/models/billing.models';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { PremiumBillingFacade } from '../../lib/premium-billing.facade';
import type { CancellationConfirmation, CancellationFeedback } from '../../models/cancellation.models';
import { PremiumCancellationActionsComponent } from './premium-cancellation-actions';

const cancel = vi.fn<PremiumBillingFacade['cancelSubscription']>();
const feedback = vi.fn<PremiumBillingFacade['submitCancellationFeedback']>();
const toast = { success: vi.fn(), info: vi.fn(), error: vi.fn() };
const scrollIntoViewDescriptor = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');
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
    if (scrollIntoViewDescriptor === undefined) {
        Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    }
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            { provide: PremiumBillingFacade, useValue: { cancelSubscription: cancel, submitCancellationFeedback: feedback } },
            { provide: FdUiToastService, useValue: toast },
            { provide: AuthService, useValue: { refreshSession: (): Observable<boolean> => of(true) } },
        ],
    });
    cancel.mockReturnValue(of(confirmed));
    feedback.mockReturnValue(of(undefined));
});

afterEach(() => {
    vi.unstubAllGlobals();
    if (scrollIntoViewDescriptor === undefined) {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    }
});

async function setupAsync(currentOverview = overview): Promise<{
    fixture: ComponentFixture<PremiumCancellationActionsComponent>;
    changed: ReturnType<typeof vi.fn>;
    action: HTMLButtonElement;
}> {
    const fixture = TestBed.createComponent(PremiumCancellationActionsComponent);
    fixture.componentRef.setInput('overview', currentOverview);
    const changed = vi.fn();
    fixture.componentInstance.overviewChanged.subscribe(changed);
    await fixture.whenStable();
    return { fixture, changed, action: findButton('ACTION', fixture.nativeElement as HTMLElement) };
}

async function openDialogAsync(
    action: HTMLButtonElement,
    fixture: ComponentFixture<PremiumCancellationActionsComponent>,
): Promise<HTMLElement> {
    await waitForAvailableActionAsync(action, fixture);
    action.click();
    await stabilizeAsync(fixture);
    const dialog = document.body.querySelector<HTMLElement>('fd-premium-cancel-dialog');
    if (dialog === null) {
        throw new Error('The cancellation dialog is missing.');
    }
    return dialog;
}

async function waitForAvailableActionAsync(
    action: HTMLButtonElement,
    fixture: ComponentFixture<PremiumCancellationActionsComponent>,
): Promise<void> {
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(action.disabled).toBe(false);
    });
}

async function stabilizeAsync(fixture: ComponentFixture<PremiumCancellationActionsComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
}

function findButton(key: string, region: HTMLElement): HTMLButtonElement {
    const found = [...region.querySelectorAll('button')].find(button =>
        button.textContent.trim().endsWith(`PREMIUM_PAGE.CANCELLATION.${key}`),
    );
    if (found === undefined) {
        throw new Error(`Missing the cancellation button ${key}.`);
    }
    return found;
}

function enterComment(dialog: HTMLElement, comment: string): void {
    const textarea = dialog.querySelector('textarea');
    if (textarea === null) {
        throw new Error('The optional comments field is missing.');
    }
    textarea.value = comment;
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
}

async function selectReasonAsync(
    dialog: HTMLElement,
    fixture: ComponentFixture<PremiumCancellationActionsComponent>,
    reasonCode: string,
): Promise<void> {
    const reason = dialog.querySelector<HTMLButtonElement>('button[aria-haspopup="listbox"]');
    if (reason === null) {
        throw new Error('The optional cancellation reason control is missing.');
    }
    reason.click();
    await stabilizeAsync(fixture);
    findButton(`REASONS.${reasonCode}`, document.body).click();
}

describe('Premium cancellation', () => {
    it('cancels with the survey skipped and sends no feedback request', async () => {
        const { fixture, changed, action } = await setupAsync();
        const dialog = await openDialogAsync(action, fixture);
        expect(dialog.textContent).toContain('PREMIUM_PAGE.CANCELLATION.PAID_MESSAGE');
        const confirm = findButton('CONFIRM', dialog);
        confirm.click();
        confirm.click();
        await stabilizeAsync(fixture);
        expect(cancel).toHaveBeenCalledOnce();
        expect(feedback).not.toHaveBeenCalled();
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        expect(toast.success).toHaveBeenCalledOnce();
    });

    it('publishes confirmed cancellation before a feedback failure and preserves success', async () => {
        const draft = { reasonCode: 'too_expensive', comment: 'Price' } satisfies CancellationFeedback;
        feedback.mockReturnValue(throwError(() => new Error('Feedback unavailable')));
        const { fixture, changed, action } = await setupAsync();
        const dialog = await openDialogAsync(action, fixture);
        await selectReasonAsync(dialog, fixture, draft.reasonCode);
        enterComment(dialog, '  Price  ');
        await stabilizeAsync(fixture);
        findButton('CONFIRM', dialog).click();
        await stabilizeAsync(fixture);
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        expect(feedback).toHaveBeenCalledWith(confirmed.cancellationId, draft);
        expect(changed.mock.invocationCallOrder[0]).toBeLessThan(feedback.mock.invocationCallOrder[0] ?? Infinity);
        expect(toast.success).toHaveBeenCalledOnce();
        expect(toast.info).toHaveBeenCalledWith('PREMIUM_PAGE.CANCELLATION.FEEDBACK_ERROR');
        expect(toast.error).not.toHaveBeenCalled();
    });

    it('lets confirmation complete while the optional feedback request is still pending', async () => {
        const pendingFeedback = new Subject<unknown>();
        feedback.mockReturnValue(pendingFeedback);
        const { fixture, changed, action } = await setupAsync();
        const dialog = await openDialogAsync(action, fixture);
        enterComment(dialog, 'Optional comment');
        await stabilizeAsync(fixture);
        findButton('CONFIRM', dialog).click();
        await stabilizeAsync(fixture);
        expect(changed).toHaveBeenCalledWith(confirmed.overview);
        await waitForAvailableActionAsync(action, fixture);
        expect(pendingFeedback.observed).toBe(true);
        pendingFeedback.complete();
    });

    it('does nothing when confirmation is dismissed', async () => {
        const { fixture, action } = await setupAsync();
        const dialog = await openDialogAsync(action, fixture);
        findButton('KEEP', dialog).click();
        await stabilizeAsync(fixture);
        expect(dialog.isConnected).toBe(false);
        expect(cancel).not.toHaveBeenCalled();
        expect(feedback).not.toHaveBeenCalled();
    });

    it('does not claim success or save feedback on failure and lets the user retry', async () => {
        cancel.mockReturnValueOnce(throwError(() => new Error('Paddle unavailable')));
        const { fixture, changed, action } = await setupAsync();
        const dialog = await openDialogAsync(action, fixture);
        enterComment(dialog, 'Reason for leaving');
        await stabilizeAsync(fixture);
        findButton('CONFIRM', dialog).click();
        await stabilizeAsync(fixture);
        expect(changed).not.toHaveBeenCalled();
        expect(feedback).not.toHaveBeenCalled();
        expect(toast.success).not.toHaveBeenCalled();
        expect(toast.error).toHaveBeenCalledOnce();

        const retry = await openDialogAsync(action, fixture);
        findButton('CONFIRM', retry).click();
        await stabilizeAsync(fixture);
        expect(cancel).toHaveBeenCalledTimes(2);
        expect(changed).toHaveBeenCalledExactlyOnceWith(confirmed.overview);
        fixture.componentRef.setInput('overview', confirmed.overview);
        await stabilizeAsync(fixture);
        expect(action.isConnected).toBe(false);
    });
});

describe('Premium cancellation terms', () => {
    it.each([
        { provider: 'Paddle', currentPeriodEndUtc: overview.currentPeriodEndUtc, message: 'IMMEDIATE_MESSAGE' },
        { provider: 'TelegramStars', currentPeriodEndUtc: null, message: 'PERIOD_MESSAGE' },
    ] as const)('shows the provider-specific cancellation terms for $provider', async ({ provider, currentPeriodEndUtc, message }) => {
        const { fixture, action } = await setupAsync({
            ...overview,
            subscriptionProvider: provider,
            subscriptionStatus: 'past_due',
            currentPeriodEndUtc,
        });
        const dialog = await openDialogAsync(action, fixture);
        expect(dialog.textContent).toContain(`PREMIUM_PAGE.CANCELLATION.${message}`);
        findButton('KEEP', dialog).click();
        await stabilizeAsync(fixture);
        expect(cancel).not.toHaveBeenCalled();
    });
});
