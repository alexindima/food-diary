import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { ProductListPaginationComponent } from './product-list-pagination.component';

const PAGE_INDEX = 2;
const TOTAL_ITEMS = 30;
const PAGE_SIZE = 10;

describe('ProductListPaginationComponent', () => {
    it('should hide pagination when there is only one page', async () => {
        const { fixture } = await setupComponentAsync({ totalPages: 1 });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('fd-ui-pagination')).toBeNull();
    });

    it('should emit page changes', async () => {
        const { component } = await setupComponentAsync({ totalPages: 3 });
        const handler = vi.fn();
        component.pageChange.subscribe(handler);

        component.pageChange.emit(PAGE_INDEX);

        expect(handler).toHaveBeenCalledWith(PAGE_INDEX);
    });
});

type ProductListPaginationSetupOptions = {
    totalPages: number;
};

async function setupComponentAsync(
    options: ProductListPaginationSetupOptions,
): Promise<{ component: ProductListPaginationComponent; fixture: ComponentFixture<ProductListPaginationComponent> }> {
    await TestBed.configureTestingModule({
        imports: [ProductListPaginationComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductListPaginationComponent);
    fixture.componentRef.setInput('totalPages', options.totalPages);
    fixture.componentRef.setInput('length', TOTAL_ITEMS);
    fixture.componentRef.setInput('pageSize', PAGE_SIZE);
    fixture.componentRef.setInput('pageIndex', 0);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
