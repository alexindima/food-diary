import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { ProductListEmptyStateComponent } from './product-list-empty-state.component';

describe('ProductListEmptyStateComponent', () => {
    it('should render empty state and emit add product requests', async () => {
        const { component, fixture } = await setupComponentAsync('empty');
        const handler = vi.fn();
        component.addProduct.subscribe(handler);
        const element = fixture.nativeElement as HTMLElement;

        component.addProduct.emit();

        expect(element.textContent).toContain('PRODUCT_LIST.EMPTY_TITLE');
        expect(handler).toHaveBeenCalled();
    });

    it('should render no results state', async () => {
        const { fixture } = await setupComponentAsync('no-results');
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('PRODUCT_LIST.NO_RESULTS_TITLE');
    });
});

async function setupComponentAsync(
    state: 'empty' | 'no-results',
): Promise<{ component: ProductListEmptyStateComponent; fixture: ComponentFixture<ProductListEmptyStateComponent> }> {
    await TestBed.configureTestingModule({
        imports: [ProductListEmptyStateComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductListEmptyStateComponent);
    fixture.componentRef.setInput('state', state);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
