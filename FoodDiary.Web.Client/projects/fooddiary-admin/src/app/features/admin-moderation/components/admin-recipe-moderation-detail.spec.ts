import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { type AdminRecipeModerationAction, AdminRecipeModerationFacade } from '../lib/admin-recipe-moderation.facade';
import type { AdminRecipeModerationItem } from '../models/admin-recipe-moderation.data';
import { AdminRecipeModerationDetailComponent } from './admin-recipe-moderation-detail';

const item: AdminRecipeModerationItem = {
    revisionId: adminId<'recipe-moderation-revision'>('revision-1'),
    recipeId: adminId<'recipe'>('recipe-1'),
    ownerId: adminId<'user'>('owner-1'),
    policyVersionId: adminId<'recipe-moderation-policy'>('policy-1'),
    status: 'NeedsReview',
    text: 'Submitted recipe',
    summary: 'Review needed',
    createdAt: adminUtcInstant('2026-08-12T09:00:00Z'),
    updatedAt: null,
    reason: 'Check the ingredient list.',
    primaryResult: null,
    shadowResult: null,
    attempts: 1,
    images: [],
};

const approveLabel = 'ADMIN_RECIPE_MODERATION.APPROVE';
const changesLabel = 'ADMIN_RECIPE_MODERATION.REQUEST_CHANGES';
const retryLabel = 'ADMIN_RECIPE_MODERATION.RETRY';

describe('Admin recipe moderation detail decisions', () => {
    it('accepts approval without a note and requests changes only for a nonblank reason', async () => {
        const { fixture, host, submit } = await setupDetailAsync();
        fixture.componentRef.setInput('item', item);
        fixture.detectChanges();
        const approve = requireButton(host, approveLabel);
        const requestChanges = requireButton(host, changesLabel);
        const reason = requireReason(host);

        expect(host.textContent).toContain(item.revisionId);
        expect(host.textContent).toContain(item.policyVersionId);
        expect(host.textContent).toContain(item.ownerId);
        expect(host.textContent).toContain('ADMIN_RECIPE_MODERATION.NEEDS_REVIEW');
        expect(host.textContent).toContain(item.reason);
        expect(approve.disabled).toBe(false);
        expect(requestChanges.disabled).toBe(true);
        approve.click();
        requestChanges.click();
        expect(submit).toHaveBeenCalledExactlyOnceWith('approve');

        typeReason(reason, '   ');
        fixture.detectChanges();
        expect(requestChanges.disabled).toBe(true);

        const note = 'Clarify the missing ingredient.';
        typeReason(reason, note);
        fixture.detectChanges();
        expect(fixture.componentInstance.reason()).toBe(note);
        expect(requestChanges.disabled).toBe(false);
        requestChanges.click();
        expect(submit.mock.calls).toEqual([['approve'], ['requestChanges']]);
    });

    it('disables both decisions and the reason while saving and renders the server error after failure', async () => {
        const { fixture, host, submit } = await setupDetailAsync();
        fixture.componentRef.setInput('item', item);
        fixture.componentRef.setInput('reason', 'Keep the submitted reason.');
        fixture.componentRef.setInput('actionInProgress', true);
        fixture.detectChanges();
        const reason = requireReason(host);
        const approve = requireButton(host, approveLabel);
        const requestChanges = requireButton(host, changesLabel);

        expect(reason.disabled).toBe(true);
        expect(approve.disabled).toBe(true);
        expect(requestChanges.disabled).toBe(true);
        approve.click();
        requestChanges.click();
        expect(submit).not.toHaveBeenCalled();

        fixture.componentRef.setInput('actionInProgress', false);
        fixture.componentRef.setInput('actionError', 'ADMIN_RECIPE_MODERATION.DECISION_FAILED');
        fixture.detectChanges();
        expect(host.querySelector('fd-ui-form-error')?.textContent).toContain('ADMIN_RECIPE_MODERATION.DECISION_FAILED');
        expect(reason.value).toBe('Keep the submitted reason.');
        expect(requestChanges.disabled).toBe(false);
    });
});

describe('Admin recipe moderation detail loading and status', () => {
    it('shows loading first and lets the administrator retry a failed detail load', async () => {
        const { fixture, host } = await setupDetailAsync();
        const retryDetails = vi.fn<() => void>();
        fixture.componentInstance.retryDetails.subscribe(retryDetails);
        fixture.componentRef.setInput('loading', true);
        fixture.componentRef.setInput('failed', true);
        fixture.detectChanges();
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe('ADMIN_COMMON.LOADING');
        expect(host.querySelector('textarea')).toBeNull();
        expect(host.querySelector('fd-admin-load-error')).toBeNull();

        fixture.componentRef.setInput('loading', false);
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('ADMIN_COMMON.LOAD_ERROR');
        requireButton(host, 'ADMIN_COMMON.RETRY').click();
        expect(retryDetails).toHaveBeenCalledTimes(1);
    });

    it('offers Retry only to a delayed check and hides decisions for a published revision', async () => {
        const { fixture, host, submit } = await setupDetailAsync();
        fixture.componentRef.setInput('item', { ...item, status: 'CheckDelayed', reason: null });
        fixture.detectChanges();
        expect(host.querySelector('textarea')).toBeNull();
        expect(host.textContent).not.toContain(approveLabel);
        const retry = requireButton(host, retryLabel);
        retry.click();
        expect(submit).toHaveBeenCalledExactlyOnceWith('retry');

        fixture.componentRef.setInput('actionInProgress', true);
        fixture.detectChanges();
        expect(retry.disabled).toBe(true);
        retry.click();
        expect(submit).toHaveBeenCalledTimes(1);

        fixture.componentRef.setInput('item', { ...item, status: 'Published', reason: null });
        fixture.detectChanges();
        expect(host.textContent).toContain('ADMIN_RECIPE_MODERATION.PUBLISHED');
        expect(host.querySelector('textarea, button')).toBeNull();
    });
});

async function setupDetailAsync(): Promise<{
    fixture: ComponentFixture<AdminRecipeModerationDetailComponent>;
    host: HTMLElement;
    submit: Mock<(action: AdminRecipeModerationAction) => void>;
}> {
    await TestBed.configureTestingModule({
        imports: [AdminRecipeModerationDetailComponent],
        providers: [...provideTranslateTesting(), { provide: AdminRecipeModerationFacade, useValue: {} }],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminRecipeModerationDetailComponent);
    const submit = vi.fn<(action: AdminRecipeModerationAction) => void>();
    fixture.componentInstance.submitAction.subscribe(submit);
    return { fixture, host: fixture.nativeElement as HTMLElement, submit };
}

function requireButton(host: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(control => control.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the moderation button ${label}`);
    }
    return button;
}

function requireReason(host: HTMLElement): HTMLTextAreaElement {
    const reason = host.querySelector('textarea');
    if (reason === null) {
        throw new Error('Expected the moderation reason textarea');
    }
    return reason;
}

function typeReason(control: HTMLTextAreaElement, value: string): void {
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}
