import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form, required } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { ShoppingListItemFormModel } from '../../lib/shopping-list-form.types';
import type { ShoppingListItem } from '../../models/shopping-list.data';
import { ShoppingListItemsPanelComponent } from './shopping-list-items-panel';

const CHECKED_ITEM: ShoppingListItem = {
    id: 'item-1',
    shoppingListId: 'list-1',
    name: 'Milk',
    amount: 2,
    unit: 'l',
    category: 'Dairy',
    isChecked: true,
    sortOrder: 1,
};

function createItemForm(): FieldTree<ShoppingListItemFormModel> {
    const model = signal<ShoppingListItemFormModel>({
        name: '',
        amount: null,
        unit: null,
        category: null,
    });

    return TestBed.runInInjectionContext(() =>
        form(model, path => {
            required(path.name);
        }),
    );
}

async function setupItemsPanelAsync(items: ShoppingListItem[] = [CHECKED_ITEM]): Promise<{
    component: ShoppingListItemsPanelComponent;
    fixture: ComponentFixture<ShoppingListItemsPanelComponent>;
}> {
    await TestBed.configureTestingModule({
        imports: [ShoppingListItemsPanelComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ShoppingListItemsPanelComponent);
    fixture.componentRef.setInput('itemForm', createItemForm());
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('ShoppingListItemsPanelComponent', () => {
    it('builds localized unit options and item view models', async () => {
        const { component } = await setupItemsPanelAsync();

        expect(component['unitOptions']().length).toBeGreaterThan(0);
        expect(component['itemViewModels']()[0]).toMatchObject({
            id: CHECKED_ITEM.id,
            name: CHECKED_ITEM.name,
            isChecked: true,
        });
        expect(component['itemViewModels']()[0].meta).toContain('2');
    });

    it('emits add, remove, and checked change events', async () => {
        const { component } = await setupItemsPanelAsync();
        const addSpy = vi.fn();
        const removeSpy = vi.fn();
        const checkedSpy = vi.fn();
        component.itemAdd.subscribe(addSpy);
        component.itemRemove.subscribe(removeSpy);
        component.itemCheckedChange.subscribe(checkedSpy);

        component.itemAdd.emit();
        component.itemRemove.emit(CHECKED_ITEM.id);
        component.itemCheckedChange.emit({ itemId: CHECKED_ITEM.id, checked: false });

        expect(addSpy).toHaveBeenCalledOnce();
        expect(removeSpy).toHaveBeenCalledWith(CHECKED_ITEM.id);
        expect(checkedSpy).toHaveBeenCalledWith({ itemId: CHECKED_ITEM.id, checked: false });
    });

    it('renders empty state view model for empty items', async () => {
        const { component } = await setupItemsPanelAsync([]);

        expect(component['itemViewModels']()).toEqual([]);
    });
});
