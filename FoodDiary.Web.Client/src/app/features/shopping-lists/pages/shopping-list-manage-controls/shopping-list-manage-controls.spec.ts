import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { ShoppingListSummary } from '../../models/shopping-list.data';
import { ShoppingListManageControlsComponent } from './shopping-list-manage-controls';

const LISTS: ShoppingListSummary[] = [
    { id: 'list-1', name: 'Groceries', createdAt: '2026-05-17T00:00:00Z', itemsCount: 1 },
    { id: 'list-2', name: 'Weekend', createdAt: '2026-05-18T00:00:00Z', itemsCount: 0 },
];

async function setupManageControlsAsync(lists: ShoppingListSummary[] = LISTS): Promise<{
    component: ShoppingListManageControlsComponent;
    fixture: ComponentFixture<ShoppingListManageControlsComponent>;
}> {
    await TestBed.configureTestingModule({
        imports: [ShoppingListManageControlsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const listSelectModel = signal({ id: 'list-1' });
    const listNameModel = signal({ name: 'Groceries' });
    const listSelectForm = TestBed.runInInjectionContext(() => form(listSelectModel));
    const listNameForm = TestBed.runInInjectionContext(() => form(listNameModel));
    const fixture = TestBed.createComponent(ShoppingListManageControlsComponent);
    fixture.componentRef.setInput('listSelectField', listSelectForm.id);
    fixture.componentRef.setInput('listNameField', listNameForm.name);
    fixture.componentRef.setInput('lists', lists);
    fixture.componentRef.setInput('isLoading', false);
    fixture.componentRef.setInput('canDeleteList', true);
    fixture.componentRef.setInput('canClearList', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('ShoppingListManageControlsComponent', () => {
    it('maps list summaries into select options and count', async () => {
        const { component } = await setupManageControlsAsync();

        expect(component['listsCount']()).toBe(2);
        expect(component['listOptions']()).toEqual([
            { label: 'Groceries (1)', value: 'list-1' },
            { label: 'Weekend (0)', value: 'list-2' },
        ]);
    });

    it('emits create, delete, and clear actions', async () => {
        const { component } = await setupManageControlsAsync();
        const createSpy = vi.fn();
        const deleteSpy = vi.fn();
        const clearSpy = vi.fn();
        component.createList.subscribe(createSpy);
        component.deleteList.subscribe(deleteSpy);
        component.clearList.subscribe(clearSpy);

        component.createList.emit();
        component.deleteList.emit();
        component.clearList.emit();

        expect(createSpy).toHaveBeenCalledOnce();
        expect(deleteSpy).toHaveBeenCalledOnce();
        expect(clearSpy).toHaveBeenCalledOnce();
    });

    it('supports single-list clear state', async () => {
        const { component, fixture } = await setupManageControlsAsync([LISTS[0]]);
        fixture.componentRef.setInput('canDeleteList', false);
        fixture.componentRef.setInput('canClearList', true);
        fixture.detectChanges();

        expect(component['listsCount']()).toBe(1);
        expect(component.canDeleteList()).toBe(false);
        expect(component.canClearList()).toBe(true);
    });
});
