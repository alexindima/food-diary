import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminRecipeModerationFacade } from '../lib/admin-recipe-moderation.facade';
import type { AdminRecipeModerationItem } from '../models/admin-recipe-moderation.data';
import { AdminRecipeModerationEvidenceComponent } from './admin-recipe-moderation-evidence';

const cover = { assetId: adminId<'image-asset'>('cover-1'), field: 'cover' };
const step = { assetId: adminId<'image-asset'>('step-1'), field: 'steps[0]' };
const checkedImage = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a3ioAAAAASUVORK5CYII=';
const item: AdminRecipeModerationItem = {
    revisionId: adminId<'recipe-moderation-revision'>('revision-1'),
    recipeId: adminId<'recipe'>('recipe-1'),
    ownerId: adminId<'user'>('owner-1'),
    policyVersionId: adminId<'recipe-moderation-policy'>('policy-1'),
    status: 'NeedsReview',
    text: 'Checked recipe instructions',
    summary: 'Moderation evidence',
    createdAt: adminUtcInstant('2026-08-12T09:00:00Z'),
    updatedAt: null,
    reason: null,
    primaryResult: '{"requiresReview":true}',
    shadowResult: 'Provider response remains plain text.',
    attempts: 1,
    images: [cover, step],
};

describe('Admin recipe moderation evidence content', () => {
    it('renders checked text and opens formatted and plain-text provider results', async () => {
        const { host } = await setupEvidenceAsync();
        expect(host.querySelector('pre')?.textContent).toBe(item.text);
        const disclosure = Array.from(host.querySelectorAll('details'));
        expect(disclosure.map(panel => panel.querySelector('summary')?.textContent.trim())).toEqual([
            'ADMIN_RECIPE_MODERATION.OPENAI_RESULT',
            'ADMIN_RECIPE_MODERATION.MISTRAL_RESULT',
        ]);

        for (const panel of disclosure) {
            const summary = panel.querySelector('summary');
            if (summary === null) {
                throw new Error('Expected the provider result disclosure');
            }
            summary.click();
        }
        expect(disclosure.every(panel => panel.open)).toBe(true);
        expect(disclosure.map(panel => panel.querySelector('pre')?.textContent)).toEqual([
            '{\n  "requiresReview": true\n}',
            'Provider response remains plain text.',
        ]);
    });

    it('keeps absent provider results and the image action section out of the rendered evidence', async () => {
        const { host } = await setupEvidenceAsync({ ...item, primaryResult: null, shadowResult: '', images: [] });
        expect(host.querySelector('pre')?.textContent).toBe(item.text);
        expect(host.querySelector('details, button')).toBeNull();
    });
});

describe('Admin recipe moderation evidence image ownership', () => {
    it('releases the previous request when choosing another image and removes the selected preview on Close', async () => {
        const { fixture, host, getImage } = await setupEvidenceAsync();
        const first = new Subject<string>();
        const second = new Subject<string>();
        getImage.mockReturnValueOnce(first).mockReturnValueOnce(second);
        imageButton(host, cover.field).click();
        fixture.detectChanges();
        expect(getImage).toHaveBeenCalledExactlyOnceWith(item.revisionId, cover.assetId);
        expect(first.observed).toBe(true);
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe('ADMIN_COMMON.LOADING');

        imageButton(host, step.field).click();
        fixture.detectChanges();
        expect(first.observed).toBe(false);
        expect(getImage).toHaveBeenLastCalledWith(item.revisionId, step.assetId);
        first.next(checkedImage);
        fixture.detectChanges();
        expect(host.querySelector('img')).toBeNull();

        second.next(checkedImage);
        fixture.detectChanges();
        expect(host.querySelector('[role="status"]')).toBeNull();
        expect(host.querySelector('img')?.getAttribute('src')).toBe(checkedImage);
        expect(host.querySelector('img')?.alt).toBe('ADMIN_RECIPE_MODERATION.CHECKED_IMAGE');
        requireButton(host, 'ADMIN_RECIPE_MODERATION.CLOSE_IMAGE').click();
        fixture.detectChanges();
        expect(second.observed).toBe(false);
        expect(host.querySelector('img')).toBeNull();
    });

    it('shows a failed image request, clears the error on retry and cancels the pending read on destroy', async () => {
        const { fixture, host, getImage } = await setupEvidenceAsync();
        const failed = new Subject<string>();
        const retry = new Subject<string>();
        getImage.mockReturnValueOnce(failed).mockReturnValueOnce(retry);
        imageButton(host, cover.field).click();
        failed.error(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }));
        fixture.detectChanges();
        expect(host.querySelector('fd-ui-form-error')?.textContent).toContain('ADMIN_RECIPE_MODERATION.IMAGE_UNAVAILABLE');
        expect(host.querySelector('img')).toBeNull();

        imageButton(host, cover.field).click();
        fixture.detectChanges();
        expect(host.querySelector('fd-ui-form-error')).toBeNull();
        expect(host.querySelector('[role="status"]')?.textContent.trim()).toBe('ADMIN_COMMON.LOADING');
        expect(getImage.mock.calls).toEqual([
            [item.revisionId, cover.assetId],
            [item.revisionId, cover.assetId],
        ]);
        expect(retry.observed).toBe(true);
        fixture.destroy();
        expect(retry.observed).toBe(false);
    });
});

async function setupEvidenceAsync(evidence = item): Promise<{
    fixture: ComponentFixture<AdminRecipeModerationEvidenceComponent>;
    host: HTMLElement;
    getImage: Mock<AdminRecipeModerationFacade['getImage']>;
}> {
    const getImage = vi.fn<AdminRecipeModerationFacade['getImage']>();
    await TestBed.configureTestingModule({
        imports: [AdminRecipeModerationEvidenceComponent],
        providers: [...provideTranslateTesting(), { provide: AdminRecipeModerationFacade, useValue: { getImage } }],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminRecipeModerationEvidenceComponent);
    fixture.componentRef.setInput('item', evidence);
    fixture.detectChanges();
    return { fixture, host: fixture.nativeElement as HTMLElement, getImage };
}

function imageButton(host: HTMLElement, field: string): HTMLButtonElement {
    return requireButton(host, `ADMIN_RECIPE_MODERATION.VIEW_IMAGE · ${field}`);
}

function requireButton(host: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(control => control.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the evidence button ${label}`);
    }
    return button;
}
