import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ShoppingItemEditComponent, type ShoppingItemEditData } from './shopping-item-edit';

async function expectQuantityErrorAsync(fixture: ComponentFixture<ShoppingItemEditComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('SHOPPING_LIST.AMOUNT_ERROR');
    expect(element.querySelector('input[type="number"]')?.getAttribute('aria-invalid')).toBe('true');
}

describe('shopping item editing', () => {
    it.each([
        ['Ml', 'ML'],
        ['Pcs', 'PCS'],
        [' g ', 'G'],
    ])('displays and retains persisted unit %s when saving another field', async (persistedUnit, selectedUnit) => {
        const ref = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [ShoppingItemEditComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: ref },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        initial: { name: 'Oats', amount: 150, unit: persistedUnit, category: null, note: null },
                        productId: null,
                        saveBlocked: signal(false),
                    } satisfies ShoppingItemEditData,
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingItemEditComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('fd-ui-select button')?.textContent).toContain(`GENERAL.UNITS.${selectedUnit}`);
        fixture.componentInstance['draft'].update(value => ({ ...value, note: 'Keep the original unit' }));
        fixture.componentInstance['save'](new Event('submit'));
        expect(ref.close).toHaveBeenCalledWith(expect.objectContaining({ unit: selectedUnit, note: 'Keep the original unit' }));
    });

    it.each([null, 'product-1'])('keeps product identity fixed only for linked items (%s)', async productId => {
        const ref = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [ShoppingItemEditComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: ref },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        initial: { name: 'Oats', amount: 150, unit: 'G', category: null, note: null },
                        productId,
                        saveBlocked: signal(false),
                    } satisfies ShoppingItemEditData,
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
});

describe('Shopping item editor native validation', () => {
    it('guides correction of an invalid row and saves the new name and category', async () => {
        const ref = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [ShoppingItemEditComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: ref },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        initial: { name: 'Salt', amount: null, unit: null, category: 'Pantry', note: 'to taste' },
                        productId: null,
                        saveBlocked: signal(false),
                    } satisfies ShoppingItemEditData,
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingItemEditComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        const element = fixture.nativeElement as HTMLElement;
        const name = editorInput(element, 'SHOPPING_LIST.ADD_ITEM_NAME');
        const amount = editorInput(element, 'SHOPPING_LIST.ADD_ITEM_AMOUNT');
        const submit = element.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (submit === null) {
            throw new Error('Expected the shopping item save button');
        }

        enterValue(name, '  ');
        submit.click();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(ref.close).not.toHaveBeenCalled();
        expect(element.ownerDocument.activeElement).toBe(name);

        const correctedName = 'Sea salt flakes';
        enterValue(name, correctedName);
        enterValue(amount, '-1');
        submit.click();
        await expectQuantityErrorAsync(fixture);
        expect(ref.close).not.toHaveBeenCalled();
        expect(element.ownerDocument.activeElement).toBe(amount);

        const category = 'Seasonings';
        enterValue(amount, '');
        enterValue(editorInput(element, 'SHOPPING_LIST.ADD_ITEM_CATEGORY'), category);
        enterValue(editorInput(element, 'SHOPPING_LIST.ADD_ITEM_NOTE'), 'optional');
        submit.click();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(ref.close).toHaveBeenCalledExactlyOnceWith({ name: correctedName, amount: null, unit: null, category, note: 'optional' });
    });
});

function editorInput(element: HTMLElement, label: string): HTMLInputElement {
    const input = Array.from(element.querySelectorAll('input')).find(field => field.getAttribute('aria-label') === label);
    if (input === undefined) {
        throw new Error(`Expected the shopping item editor field ${label}`);
    }
    return input;
}

function enterValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}
