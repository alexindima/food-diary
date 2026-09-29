import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ShoppingItemEditComponent } from './shopping-item-edit';

describe('shopping item editing', () => {
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
