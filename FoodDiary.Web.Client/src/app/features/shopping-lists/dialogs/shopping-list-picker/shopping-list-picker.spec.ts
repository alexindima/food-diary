import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { ShoppingListSummary } from '../../../../shared/models/shopping-list.data';
import { ShoppingListPickerComponent, type ShoppingListPickerData } from './shopping-list-picker';

const WEEKEND: ShoppingListSummary = {
    id: entityId<'shopping-list'>('weekend'),
    name: 'Weekend groceries',
    createdAt: utcInstant('2026-05-17T10:00:00Z'),
    itemsCount: 1,
};
const INGREDIENT_AMOUNT = 1.5;
const INGREDIENT_COUNT = 4;

describe('ShoppingListPickerComponent', () => {
    it('requires a non-blank name when choosing a new list and returns the trimmed name', () => {
        const { close, fixture, host } = openPicker();
        expect(host.querySelector('h2')?.textContent).toBe('PUBLIC_RECIPES.SHOPPING_SELECT_TITLE');
        const name = host.querySelector<HTMLInputElement>('input');
        if (name === null) {
            throw new Error('Expected a new shopping list name field');
        }
        name.value = '  ';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        getButton(host, 'PUBLIC_RECIPES.SHOPPING_CHOOSE').click();
        expect(close).not.toHaveBeenCalled();

        name.value = '  Weekly groceries  ';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        getButton(host, 'PUBLIC_RECIPES.SHOPPING_CHOOSE').click();

        expect(close).toHaveBeenCalledExactlyOnceWith({ id: null, name: 'Weekly groceries' });
    });

    it('switches to an existing list from the dropdown and returns the selected list', () => {
        const item = { name: 'Oats', amount: INGREDIENT_AMOUNT, unit: 'cup', note: 'Rolled' };
        const { close, fixture, host } = openPicker({ item });
        expect(host.querySelector('h2')?.textContent).toBe('PUBLIC_RECIPES.SHOPPING_PICK');
        expect(host.textContent).toContain('Oats');
        expect(host.textContent).toMatch(/1\.5\s+GENERAL\.UNITS\.cup/u);
        expect(host.textContent).toContain('Rolled');
        expect(getButton(host, 'PUBLIC_RECIPES.SHOPPING_CREATE_ADD')).toBeTruthy();
        host.querySelector<HTMLButtonElement>('button[aria-haspopup="listbox"]')?.click();
        fixture.detectChanges();

        const listbox = document.querySelector<HTMLElement>('[role="listbox"]');
        if (listbox === null) {
            throw new Error('Expected the shopping list dropdown');
        }
        getButton(listbox, WEEKEND.name).click();
        fixture.detectChanges();
        expect(host.querySelector('input')).toBeNull();
        getButton(host, 'PUBLIC_RECIPES.SHOPPING_ADD_CONFIRM').click();

        expect(close).toHaveBeenCalledExactlyOnceWith({ id: WEEKEND.id, name: WEEKEND.name });
    });

    it('preselects the last list in bulk mode and leaves it untouched on cancel', async () => {
        const { close, fixture, host } = openPicker({ count: INGREDIENT_COUNT, selected: WEEKEND });
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { PUBLIC_RECIPES: { SHOPPING_COUNT: '{{count}} ingredients' } });
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();

        expect(host.querySelector('h2')?.textContent).toBe('PUBLIC_RECIPES.SHOPPING_BULK_TITLE');
        expect(host.textContent).toContain('4 ingredients');
        expect(host.querySelector('button[aria-haspopup="listbox"]')?.textContent.trim()).toBe(WEEKEND.name);
        expect(host.querySelector('input')).toBeNull();
        getButton(host, 'PUBLIC_RECIPES.SHOPPING_CANCEL').click();

        expect(close).toHaveBeenCalledExactlyOnceWith();
    });
});

function openPicker(options: Partial<ShoppingListPickerData> = {}): {
    close: ReturnType<typeof vi.fn>;
    fixture: ComponentFixture<ShoppingListPickerComponent>;
    host: HTMLElement;
} {
    const data: ShoppingListPickerData = { lists: [WEEKEND], name: 'Groceries', selected: null, ...options };
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [ShoppingListPickerComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(ShoppingListPickerComponent);
    fixture.detectChanges();
    return { close, fixture, host: fixture.nativeElement as HTMLElement };
}

function getButton(container: HTMLElement, text: string): HTMLButtonElement {
    const button = Array.from(container.querySelectorAll('button')).find(candidate => candidate.textContent.trim() === text);
    if (button === undefined) {
        throw new Error(`Button "${text}" was not found`);
    }
    return button;
}
