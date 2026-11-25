import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { ShoppingListSummary } from '../../../../shared/models/shopping-list.data';
import { ShoppingListService } from '../../api/shopping-list.service';
import { ShoppingListBrowserDialogComponent } from '../../dialogs/shopping-list-browser-dialog/shopping-list-browser-dialog';
import { ShoppingListManageControlsComponent } from './shopping-list-manage-controls';

const LISTS: ShoppingListSummary[] = [
    { id: entityId<'shopping-list'>('list-1'), name: 'Groceries', createdAt: utcInstant('2026-05-17T00:00:00Z'), itemsCount: 1 },
    { id: entityId<'shopping-list'>('list-2'), name: 'Weekend', createdAt: utcInstant('2026-05-18T00:00:00Z'), itemsCount: 0 },
];

async function setupManageControlsAsync(lists: ShoppingListSummary[] = LISTS): Promise<{
    component: ShoppingListManageControlsComponent;
    fixture: ComponentFixture<ShoppingListManageControlsComponent>;
    openDialog: ReturnType<typeof vi.fn>;
    getPage: Mock<ShoppingListService['getPage']>;
}> {
    const openDialog = vi.fn();
    const getPage = vi.fn<ShoppingListService['getPage']>().mockReturnValue(of([]));
    await TestBed.configureTestingModule({
        imports: [ShoppingListManageControlsComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([]),
            { provide: FdUiDialogService, useValue: { open: openDialog } },
            { provide: ShoppingListService, useValue: { getPage } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ShoppingListManageControlsComponent);
    fixture.componentRef.setInput('selectedListId', LISTS[0].id);
    fixture.componentRef.setInput('lists', lists);
    fixture.componentRef.setInput('isLoading', false);
    fixture.componentRef.setInput('canDeleteList', true);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture, openDialog, getPage };
}

describe('ShoppingListManageControlsComponent rendered actions', () => {
    it('keeps the original name after Escape and trims the next confirmed rename', async () => {
        const { component, fixture } = await setupManageControlsAsync();
        const rename = vi.fn();
        component.renameListById.subscribe(rename);
        const host = fixture.nativeElement as HTMLElement;
        const input = openRename(fixture);
        expect(input.value).toBe(LISTS[0].name);
        input.value = '  ';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(host.querySelector<HTMLButtonElement>('button[aria-label="SHOPPING_LIST.SAVE_NAME_BUTTON"]')?.disabled).toBe(true);
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        fixture.detectChanges();
        expect(host.querySelector('h2')?.textContent).toBe(LISTS[0].name);
        expect(rename).not.toHaveBeenCalled();

        const newName = openRename(fixture);
        newName.value = '  Next week  ';
        newName.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        host.querySelector<HTMLButtonElement>('button[aria-label="SHOPPING_LIST.SAVE_NAME_BUTTON"]')?.click();
        fixture.detectChanges();
        expect(host.querySelector('input')).toBeNull();
        expect(rename).toHaveBeenCalledExactlyOnceWith({ listId: LISTS[0].id, name: 'Next week' });
    });

    it('submits a rename once when clicking Save also blurs the name input', async () => {
        const { component, fixture } = await setupManageControlsAsync();
        const renamed = vi.fn<(event: { listId: ShoppingListSummary['id']; name: string }) => void>();
        component.renameListById.subscribe(renamed);
        const host = fixture.nativeElement as HTMLElement;
        const input = openRename(fixture);
        input.focus();
        input.value = '  Next week  ';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const save = host.querySelector<HTMLButtonElement>('button[aria-label="SHOPPING_LIST.SAVE_NAME_BUTTON"]');
        if (save === null) {
            throw new Error('Expected the inline shopping list save button');
        }
        expect(save.disabled).toBe(false);
        expect(document.activeElement).toBe(input);
        expect(renamed).not.toHaveBeenCalled();

        save.focus();
        expect(document.activeElement).toBe(save);
        expect(renamed).toHaveBeenCalledExactlyOnceWith({ listId: LISTS[0].id, name: 'Next week' });
        save.click();
        fixture.detectChanges();

        expect(renamed).toHaveBeenCalledOnce();
        expect(input.isConnected).toBe(false);
    });

    it('applies selection and creation from the mobile list dialog and ignores dismissal', async () => {
        const extra = ['Party', 'Vacation'].map(name => ({ ...LISTS[1], id: entityId<'shopping-list'>(name), name }));
        const { component, fixture, openDialog } = await setupManageControlsAsync([...LISTS, ...extra]);
        const create = vi.fn();
        component.createList.subscribe(create);
        component.listSelected.subscribe(id => {
            fixture.componentRef.setInput('selectedListId', id);
        });
        fixture.componentRef.setInput('isMobile', true);
        fixture.detectChanges();
        openDialog
            .mockReturnValueOnce({ afterClosed: () => of(undefined) })
            .mockReturnValueOnce({ afterClosed: () => of({ id: extra[1].id }) })
            .mockReturnValueOnce({ afterClosed: () => of({ create: true, name: 'Market' }) });

        const host = fixture.nativeElement as HTMLElement;
        const allLists = host.querySelector<HTMLButtonElement>('.shopping-list__all-lists-button button');
        allLists?.click();
        expect(host.querySelector('button[aria-current="true"]')?.textContent).toContain(LISTS[0].name);
        expect(create).not.toHaveBeenCalled();
        allLists?.click();
        fixture.detectChanges();
        expect(host.querySelector('button[aria-current="true"]')?.textContent).toContain(extra[1].name);
        allLists?.click();

        expect(openDialog).toHaveBeenCalledWith(ShoppingListBrowserDialogComponent, expect.objectContaining({ preset: 'form' }));
        expect(create).toHaveBeenCalledExactlyOnceWith('Market');
    });
});

function openRename(fixture: ComponentFixture<ShoppingListManageControlsComponent>): HTMLInputElement {
    const host = fixture.nativeElement as HTMLElement;
    host.querySelector<HTMLButtonElement>('button[aria-label="COMMON.MORE_ACTIONS"]')?.click();
    fixture.detectChanges();
    const menuItems = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]'));
    menuItems.find(item => item.textContent.trim().endsWith('SHOPPING_LIST.RENAME_LIST_BUTTON'))?.click();
    fixture.detectChanges();
    const input = host.querySelector<HTMLInputElement>('input[aria-label="SHOPPING_LIST.RENAME_LIST_BUTTON"]');
    if (input === null) {
        throw new Error('Expected the inline shopping list name editor');
    }
    return input;
}

describe('ShoppingListManageControlsComponent', () => {
    it('keeps tab order stable and replaces the least recently used slot', async () => {
        const extra = ['3', '4', '5'].map(number => ({
            ...LISTS[0],
            id: entityId<'shopping-list'>(`list-${number}`),
            name: `Extra ${number}`,
        }));
        const { component } = await setupManageControlsAsync([...LISTS, ...extra]);
        component['selectList'](entityId<'shopping-list'>('list-5'));
        expect(component['quickLists']().map(list => list.id)).toEqual(['list-1', 'list-2', 'list-5']);
        expect(component['quickLists']().map(list => list.id)).toContain('list-5');
        component['selectList'](entityId<'shopping-list'>('list-2'));
        expect(component['quickLists']().map(list => list.id)).toEqual(['list-1', 'list-2', 'list-5']);
        component['selectList'](entityId<'shopping-list'>('list-4'));
        expect(component['quickLists']().map(list => list.id)).toEqual(['list-4', 'list-2', 'list-5']);
    });

    it('tracks list count and selected list card state', async () => {
        const { component } = await setupManageControlsAsync();

        expect(component['listsCount']()).toBe(2);
        expect(component['selectedListId']()).toBe('list-1');
    });

    it('requests a selection and keeps the current card until the input changes', async () => {
        const { component, fixture } = await setupManageControlsAsync();
        const selected = vi.fn();
        component.listSelected.subscribe(selected);

        component['selectList'](LISTS[1].id);

        expect(selected).toHaveBeenCalledExactlyOnceWith(LISTS[1].id);
        expect(component.selectedListId()).toBe(LISTS[0].id);
        fixture.componentRef.setInput('selectedListId', LISTS[1].id);
        expect(component.selectedListId()).toBe(LISTS[1].id);
    });

    it('starts renaming the requested list after it appears in summaries', async () => {
        const { component, fixture } = await setupManageControlsAsync();
        const newList: ShoppingListSummary = {
            id: entityId<'shopping-list'>('list-3'),
            name: 'New shopping list',
            createdAt: utcInstant('2026-06-06T00:00:00Z'),
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

    it('supports single-list delete state', async () => {
        const { component, fixture } = await setupManageControlsAsync([LISTS[0]]);
        fixture.componentRef.setInput('canDeleteList', false);
        fixture.detectChanges();

        expect(component['listsCount']()).toBe(1);
        expect(component.canDeleteList()).toBe(false);
    });
});

describe('ShoppingListManageControlsComponent desktop list creation', () => {
    it('closes the desktop browser and requests a list with the trimmed unmatched name', async () => {
        const extras = ['Party', 'Vacation'].map(name => ({ ...LISTS[1], id: entityId<'shopping-list'>(name), name }));
        const lists = [...LISTS, ...extras];
        const { component, fixture, getPage, openDialog } = await setupManageControlsAsync(lists);
        fixture.componentRef.setInput('initialPage', { items: lists, hasMore: false, nextPage: null });
        fixture.detectChanges();
        const results = new Subject<ShoppingListSummary[]>();
        getPage.mockReturnValueOnce(results);
        const create = vi.fn<(name: string | void) => void>();
        component.createList.subscribe(create);

        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelector<HTMLButtonElement>('.shopping-list__all-lists-button button');
        expect(trigger).not.toBeNull();
        trigger?.click();
        fixture.detectChanges();
        const browser = document.querySelector<HTMLElement>('.cdk-overlay-container fd-shopping-list-browser');
        const search = browser?.querySelector<HTMLInputElement>('input');
        const action = browser?.querySelector<HTMLButtonElement>('.list-browser__create button');
        if (search === null || search === undefined || action === null || action === undefined) {
            throw new Error('Expected the desktop list search and create action');
        }

        search.value = '  Market trip  ';
        search.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(getPage).toHaveBeenCalledExactlyOnceWith(1, 'market trip');
        });
        expect(action.textContent).toContain('SHOPPING_LIST.CREATE_LIST');
        expect(create).not.toHaveBeenCalled();

        results.next([]);
        results.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(browser?.textContent).toContain('SHOPPING_LIST.NO_MATCHES');
        expect(action.textContent).toContain('SHOPPING_LIST.CREATE_NAMED_LIST');
        action.click();
        fixture.detectChanges();

        expect(create).toHaveBeenCalledExactlyOnceWith('Market trip');
        expect(document.querySelector('.cdk-overlay-container fd-shopping-list-browser')).toBeNull();
        expect(openDialog).not.toHaveBeenCalled();
    });
});
