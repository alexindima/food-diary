import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminRecipeModerationPolicyVersion, RecipeModerationPolicyId } from '../models/admin-recipe-moderation.data';
import { AdminRecipeModerationRuleHistoryComponent } from './admin-recipe-moderation-rule-history';

const ACTIVE: AdminRecipeModerationPolicyVersion = {
    id: adminId<'recipe-moderation-policy'>('active-rules'),
    publishedAt: adminUtcInstant('2026-08-01T12:00:00Z'),
    publishedBy: adminId<'user'>('moderation-admin'),
    rules: { textReviewCategories: ['hate'], imageReviewCategories: ['violence'], mistralSamplePercent: 25 },
};
const DEFAULTS: AdminRecipeModerationPolicyVersion = {
    id: adminId<'recipe-moderation-policy'>('initial-rules'),
    publishedAt: adminUtcInstant('2026-06-01T12:00:00Z'),
    publishedBy: null,
    rules: {
        textReviewCategories: ['hate', 'legacy-review'],
        imageReviewCategories: ['violence', 'legacy-image'],
        mistralSamplePercent: 5,
    },
};
const CATEGORY_LABELS = { hate: 'Hate', violence: 'Violence' };
type HistoryFixture = ComponentFixture<AdminRecipeModerationRuleHistoryComponent>;

beforeEach(() => {
    TestBed.configureTestingModule({ imports: [AdminRecipeModerationRuleHistoryComponent], providers: provideTranslateTesting() });
});

it('shows the current built-in policy without presenting a restore action', () => {
    const fixture = mountHistory([DEFAULTS], DEFAULTS.id);
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.history-row span:not([hidden])')?.textContent).toContain('ADMIN_RECIPE_MODERATION.DEFAULT_RULES');
    expect(root.querySelector('button')).toBeNull();
});

it('shows publisher or default metadata and opens the readable rules of an older version', () => {
    const fixture = mountHistory([ACTIVE, DEFAULTS]);
    const active = historyRow(fixture, ACTIVE.id);
    const defaults = historyRow(fixture, DEFAULTS.id);
    const publisher = active.querySelector<HTMLDivElement>(':scope > div > div:nth-child(2)');
    expect(publisher?.hidden).toBe(false);
    expect(publisher?.textContent).toContain(ACTIVE.publishedBy);
    expect(active.querySelector('button')).toBeNull();
    expect(defaults.querySelector('span:not([hidden])')?.textContent).toContain('ADMIN_RECIPE_MODERATION.DEFAULT_RULES');
    const details = defaults.querySelector('details');
    if (details === null) {
        throw new Error('Expected the older policy to provide expandable rules.');
    }
    expect(details.open).toBe(false);
    details.querySelector('summary')?.click();
    expect(details.open).toBe(true);
    expect(Array.from(details.querySelectorAll('dd'), field => field.textContent.replaceAll(/\s+/gu, ' ').trim())).toEqual([
        'Hate, legacy-review',
        'Violence, legacy-image',
        '5%',
    ]);
});

it('blocks restore while a save is pending and emits exactly the chosen inactive version when released', () => {
    const fixture = mountHistory([ACTIVE, DEFAULTS]);
    const restored = vi.fn<(id: RecipeModerationPolicyId) => void>();
    fixture.componentInstance.restore.subscribe(restored);
    const control = historyRow(fixture, DEFAULTS.id).querySelector('button');
    if (control === null) {
        throw new Error('Expected a restore action for the inactive default policy.');
    }

    fixture.componentRef.setInput('saving', true);
    fixture.detectChanges();
    expect(control.disabled).toBe(true);
    control.click();
    expect(restored).not.toHaveBeenCalled();

    fixture.componentRef.setInput('saving', false);
    fixture.detectChanges();
    expect(control.disabled).toBe(false);
    control.click();
    expect(restored).toHaveBeenCalledExactlyOnceWith(DEFAULTS.id);
});

function mountHistory(history: readonly AdminRecipeModerationPolicyVersion[], activeId = ACTIVE.id): HistoryFixture {
    const fixture = TestBed.createComponent(AdminRecipeModerationRuleHistoryComponent);
    fixture.componentRef.setInput('history', history);
    fixture.componentRef.setInput('activeId', activeId);
    fixture.componentRef.setInput('categoryLabels', CATEGORY_LABELS);
    fixture.detectChanges();
    return fixture;
}

function historyRow(fixture: HistoryFixture, id: RecipeModerationPolicyId): HTMLElement {
    const root = fixture.nativeElement as HTMLElement;
    const row = Array.from(root.querySelectorAll<HTMLElement>('.history-row')).find(element => element.textContent.includes(id));
    if (row === undefined) {
        throw new Error(`Expected a history row for ${id}.`);
    }
    return row;
}
