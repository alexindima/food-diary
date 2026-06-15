import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
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
        imports: [ShoppingListManageControlsComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    }).compileComponents();

    const listSelectModel = signal({ id: 'list-1' });
    const listSelectForm = TestBed.runInInjectionContext(() => form(listSelectModel));
    const fixture = TestBed.createComponent(ShoppingListManageControlsComponent);
    fixture.componentRef.setInput('listSelectField', listSelectForm.id);
    fixture.componentRef.setInput('lists', lists);
    fixture.componentRef.setInput('isLoading', false);
    fixture.componentRef.setInput('canDeleteList', true);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('ShoppingListManageControlsComponent', () => {
    it('tracks list count and selected list card state', async () => {
        const { component } = await setupManageControlsAsync();

        expect(component['listsCount']()).toBe(2);
        expect(component['selectedListId']()).toBe('list-1');
    });

    it('updates selected list when a card is selected', async () => {
        const { component } = await setupManageControlsAsync();

        component['selectList']('list-2');

        expect(component['selectedListId']()).toBe('list-2');
    });

    it('starts renaming the requested list after it appears in summaries', async () => {
        const { component, fixture } = await setupManageControlsAsync();
        const newList: ShoppingListSummary = {
            id: 'list-3',
            name: 'New shopping list',
            createdAt: '2026-06-06T00:00:00Z',
            itemsCount: 0,
        };
        const handledSpy = vi.fn();
        component.renameRequestHandled.subscribe(handledSpy);

        fixture.componentRef.setInput('renameRequestedListId', newList.id);
        fixture.detectChanges();

        expect(component['editingListId']()).toBeNull();

        fixture.componentRef.setInput('lists', [...LISTS, newList]);
        fixture.detectChanges();

        expect(component['editingListId']()).toBe(newList.id);
        expect(component['renameDraft']()).toBe(newList.name);
        expect(handledSpy).toHaveBeenCalledWith(newList.id);
    });

    it('emits create, rename, delete by id, and clear actions', async () => {
        const { component } = await setupManageControlsAsync();
        const createSpy = vi.fn();
        const renameSpy = vi.fn();
        const deleteByIdSpy = vi.fn();
        const clearSpy = vi.fn();
        component.createList.subscribe(createSpy);
        component.renameListById.subscribe(renameSpy);
        component.deleteListById.subscribe(deleteByIdSpy);
        component.clearListById.subscribe(clearSpy);

        component.createList.emit();
        component.renameListById.emit({ listId: 'list-2', name: 'Weekend groceries' });
        component.deleteListById.emit('list-2');
        component.clearListById.emit('list-1');

        expect(createSpy).toHaveBeenCalledOnce();
        expect(renameSpy).toHaveBeenCalledWith({ listId: 'list-2', name: 'Weekend groceries' });
        expect(deleteByIdSpy).toHaveBeenCalledWith('list-2');
        expect(clearSpy).toHaveBeenCalledWith('list-1');
    });

    it('supports single-list delete state', async () => {
        const { component, fixture } = await setupManageControlsAsync([LISTS[0]]);
        fixture.componentRef.setInput('canDeleteList', false);
        fixture.detectChanges();

        expect(component['listsCount']()).toBe(1);
        expect(component.canDeleteList()).toBe(false);
    });
});
