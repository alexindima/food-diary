import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS } from '../meal-detail-lib/meal-detail.config';
import type { MealDetailItemPreview } from '../meal-detail-lib/meal-detail.types';
import { MealDetailItemPreviewComponent } from './meal-detail-item-preview.component';

const PREVIEW_LIMIT = MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS;

describe('MealDetailItemPreviewComponent', () => {
    it('should show only preview limit while collapsed', async () => {
        const { component, fixture } = await setupComponentAsync({ items: createItems(PREVIEW_LIMIT + 1) });

        fixture.detectChanges();

        expect(component.visibleItems().length).toBe(PREVIEW_LIMIT);
        expect(component.hiddenItemPreviewCount()).toBe(1);
        expect(getFixtureText(fixture)).toContain('CONSUMPTION_DETAIL.SUMMARY.ITEMS_MORE');
    });

    it('should show all items while expanded', async () => {
        const items = createItems(PREVIEW_LIMIT + 1);
        const { component, fixture } = await setupComponentAsync({ items, isItemPreviewExpanded: true });

        fixture.detectChanges();

        expect(component.visibleItems()).toEqual(items);
        expect(getFixtureText(fixture)).toContain('CONSUMPTION_DETAIL.SUMMARY.ITEMS_HIDE');
    });

    it('should emit expand toggle from more button', async () => {
        const { component, fixture } = await setupComponentAsync({ items: createItems(PREVIEW_LIMIT + 1) });
        const toggleSpy = vi.fn();
        component.itemPreviewExpandedToggle.subscribe(toggleSpy);

        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector('button');
        button?.click();

        expect(toggleSpy).toHaveBeenCalledOnce();
    });

    it('should render translated unit key or raw unit text', async () => {
        const { fixture } = await setupComponentAsync({
            items: [
                { name: 'Product grams', amount: 100, unitKey: 'PRODUCT_AMOUNT_UNITS.G', unitText: null },
                { name: 'AI spoon', amount: 2, unitKey: null, unitText: 'spoon' },
            ],
        });

        fixture.detectChanges();

        expect(getFixtureText(fixture)).toContain('PRODUCT_AMOUNT_UNITS.G');
        expect(getFixtureText(fixture)).toContain('spoon');
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        isItemPreviewExpanded: boolean;
        items: readonly MealDetailItemPreview[];
    }> = {},
): Promise<{
    component: MealDetailItemPreviewComponent;
    fixture: ComponentFixture<MealDetailItemPreviewComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealDetailItemPreviewComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealDetailItemPreviewComponent);
    fixture.componentRef.setInput('items', overrides.items ?? createItems(1));
    fixture.componentRef.setInput('isItemPreviewExpanded', overrides.isItemPreviewExpanded ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getFixtureText(fixture: ComponentFixture<MealDetailItemPreviewComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createItems(count: number): MealDetailItemPreview[] {
    return Array.from({ length: count }, (_, index) => ({
        name: `Item ${index + 1}`,
        amount: index + 1,
        unitKey: 'PRODUCT_AMOUNT_UNITS.G',
        unitText: null,
    }));
}
