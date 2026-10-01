import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ShoppingItemEditComponent } from './shopping-item-edit';

describe('shopping item editing', () => {
    it.each([null, 'product-1'])('keeps product identity fixed only for linked items (%s)', async productId => {
        const ref = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [ShoppingItemEditComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: ref },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: { productId, name: 'Oats', amount: 150, unit: 'G', category: null, note: null },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingItemEditComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector<HTMLInputElement>('fd-ui-input input')?.readOnly).toBe(productId !== null);
        expect(element.querySelector<HTMLButtonElement>('fd-ui-select button')?.disabled).toBe(productId !== null);
        expect(element.textContent.includes('SHOPPING_LIST.PRODUCT_IDENTITY_HINT')).toBe(productId !== null);
        expect(element.querySelector<HTMLInputElement>('input[type="number"]')?.disabled).toBe(false);
        fixture.componentInstance['draft'].update(value => ({ ...value, amount: 200, note: 'For breakfast' }));
        fixture.componentInstance['save'](new Event('submit'));
        expect(ref.close).toHaveBeenCalledWith(expect.objectContaining({ name: 'Oats', unit: 'G', amount: 200, note: 'For breakfast' }));
    });

    it('returns valid edits and rejects blank names and invalid amounts', async () => {
        const ref = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [ShoppingItemEditComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: ref },
                { provide: FD_UI_DIALOG_DATA, useValue: { name: 'Salt', amount: null, unit: null, category: null, note: 'to taste' } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingItemEditComponent);
        const component = fixture.componentInstance;
        fixture.detectChanges();
        component['draft'].update(value => ({ ...value, name: ' ' }));
        component['save'](new Event('submit'));
        expect(ref.close).not.toHaveBeenCalled();
        component['draft'].update(value => ({ ...value, name: 'Salt', amount: -1 }));
        expect(component['valid']()).toBe(false);
        component['draft'].update(value => ({ ...value, amount: null, note: 'optional' }));
        component['save'](new Event('submit'));
        expect(ref.close).toHaveBeenCalledWith(expect.objectContaining({ name: 'Salt', amount: null, note: 'optional' }));
    });
});
