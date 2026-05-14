import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { ProductDetailActionsComponent } from './product-detail-actions.component';

describe('ProductDetailActionsComponent', () => {
    it('renders edit and delete actions for owned products', () => {
        const { fixture, component } = setupComponent(true);
        let editCount = 0;
        let deleteCount = 0;
        component.edit.subscribe(() => {
            editCount += 1;
        });
        component.delete.subscribe(() => {
            deleteCount += 1;
        });

        const buttons = fixture.debugElement.queryAll(By.css('fd-ui-button'));
        buttons[0].triggerEventHandler('click');
        buttons[1].triggerEventHandler('click');

        expect(getText(fixture)).toContain('PRODUCT_DETAIL.EDIT_BUTTON');
        expect(getText(fixture)).toContain('PRODUCT_DETAIL.DELETE_BUTTON');
        expect(editCount).toBe(1);
        expect(deleteCount).toBe(1);
    });

    it('renders duplicate action for read-only products', () => {
        const { fixture, component } = setupComponent(false);
        let duplicateCount = 0;
        component.duplicate.subscribe(() => {
            duplicateCount += 1;
        });

        fixture.debugElement.query(By.css('fd-ui-button')).triggerEventHandler('click');

        expect(getText(fixture)).toContain('PRODUCT_DETAIL.DUPLICATE_BUTTON');
        expect(duplicateCount).toBe(1);
    });

    it('renders warning message when provided', () => {
        const { fixture } = setupComponent(true, 'PRODUCT_DETAIL.WARNING');

        expect(getText(fixture)).toContain('PRODUCT_DETAIL.WARNING');
    });
});

function setupComponent(
    canModify: boolean,
    warningMessage: string | null = null,
): { fixture: ComponentFixture<ProductDetailActionsComponent>; component: ProductDetailActionsComponent } {
    TestBed.configureTestingModule({
        imports: [ProductDetailActionsComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(ProductDetailActionsComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('canModify', canModify);
    fixture.componentRef.setInput('warningMessage', warningMessage);
    fixture.componentRef.setInput('isDuplicateInProgress', false);
    fixture.detectChanges();

    return { fixture, component };
}

function getText(fixture: ComponentFixture<ProductDetailActionsComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
