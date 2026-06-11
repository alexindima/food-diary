import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ViewportService } from '../../../../shared/platform/viewport.service';
import { MeasurementUnit } from '../../../products/models/product.data';
import { ShoppingListFacade } from '../../lib/shopping-list.facade';
import type { ShoppingList, ShoppingListItem, ShoppingListSummary } from '../../models/shopping-list.data';
import { ShoppingListPageComponent } from './shopping-list-page';

const FIRST_LIST_ID = 'list-1';
const SECOND_LIST_ID = 'list-2';

const SHOPPING_LIST_ITEM: ShoppingListItem = {
    id: 'item-1',
    shoppingListId: FIRST_LIST_ID,
    name: 'Eggs',
    amount: 12,
    unit: 'pcs',
    category: 'Dairy',
    aisle: 'Dairy',
    note: null,
    isChecked: false,
    checkedOnUtc: null,
    sources: [],
    sortOrder: 1,
};

const SHOPPING_LIST: ShoppingList = {
    id: FIRST_LIST_ID,
    name: 'Groceries',
    createdAt: '2026-05-17T00:00:00Z',
    items: [SHOPPING_LIST_ITEM],
};

const SHOPPING_LISTS: ShoppingListSummary[] = [
    { id: FIRST_LIST_ID, name: 'Groceries', createdAt: '2026-05-17T00:00:00Z', itemsCount: 1 },
    { id: SECOND_LIST_ID, name: 'Party', createdAt: '2026-05-18T00:00:00Z', itemsCount: 0 },
];

type ShoppingListFacadeMock = {
    addItem: ReturnType<typeof vi.fn>;
    clearCurrentList: ReturnType<typeof vi.fn>;
    clearListById: ReturnType<typeof vi.fn>;
    clearRenameRequest: ReturnType<typeof vi.fn>;
    createNewList: ReturnType<typeof vi.fn>;
    deleteCurrentList: ReturnType<typeof vi.fn>;
    deleteListById: ReturnType<typeof vi.fn>;
    initialize: ReturnType<typeof vi.fn>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    items: ReturnType<typeof signal<ShoppingListItem[]>>;
    list: ReturnType<typeof signal<ShoppingList | null>>;
    listName: ReturnType<typeof signal<string>>;
    lists: ReturnType<typeof signal<ShoppingListSummary[]>>;
    removeItem: ReturnType<typeof vi.fn>;
    renameListById: ReturnType<typeof vi.fn>;
    renameRequestedListId: ReturnType<typeof signal<string | null>>;
    selectList: ReturnType<typeof vi.fn>;
    selectedListId: ReturnType<typeof signal<string | null>>;
    toggleItemChecked: ReturnType<typeof vi.fn>;
};

type ShoppingListPageTestContext = {
    component: ShoppingListPageComponent;
    dialogService: { open: ReturnType<typeof vi.fn> };
    facade: ShoppingListFacadeMock;
    fixture: ComponentFixture<ShoppingListPageComponent>;
    isMobile: ReturnType<typeof signal<boolean>>;
};

async function setupShoppingListPageAsync(): Promise<ShoppingListPageTestContext> {
    const facade: ShoppingListFacadeMock = {
        list: signal(SHOPPING_LIST),
        items: signal([SHOPPING_LIST_ITEM]),
        isLoading: signal(false),
        isSaving: signal(false),
        lists: signal(SHOPPING_LISTS),
        selectedListId: signal(FIRST_LIST_ID),
        listName: signal(SHOPPING_LIST.name),
        renameRequestedListId: signal(null),
        initialize: vi.fn(),
        selectList: vi.fn(),
        createNewList: vi.fn(),
        clearRenameRequest: vi.fn(),
        addItem: vi.fn(),
        removeItem: vi.fn(),
        toggleItemChecked: vi.fn(),
        clearCurrentList: vi.fn(),
        clearListById: vi.fn(),
        deleteCurrentList: vi.fn(),
        deleteListById: vi.fn(),
        renameListById: vi.fn(),
    };
    const isMobile = signal(false);
    const dialogService = { open: vi.fn().mockReturnValue({ afterClosed: () => of(true) }) };

    TestBed.overrideComponent(ShoppingListPageComponent, {
        set: {
            providers: [{ provide: ShoppingListFacade, useValue: facade }],
        },
    });

    await TestBed.configureTestingModule({
        imports: [ShoppingListPageComponent, TranslateModule.forRoot()],
        providers: [
            provideRouter([]),
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: ViewportService, useValue: { isMobile } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ShoppingListPageComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, dialogService, facade, fixture, isMobile };
}

describe('ShoppingListPageComponent form and item actions', () => {
    it('initializes facade and mirrors selected list/name into controls', async () => {
        const { component, facade } = await setupShoppingListPageAsync();

        expect(facade.initialize).toHaveBeenCalledOnce();
        expect(component['listSelectModel']().id).toBe(FIRST_LIST_ID);
    });

    it('delegates list select changes to facade', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        await flushSignalEffectsAsync();
        facade.selectList.mockClear();

        component['listSelectForm'].id().value.set(SECOND_LIST_ID);
        await flushSignalEffectsAsync();

        expect(facade.selectList).toHaveBeenCalledWith(SECOND_LIST_ID);
    });

    it('adds trimmed item draft and resets form', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        component['itemFormModel'].set({
            name: ' Milk ',
            amount: 2,
            unit: MeasurementUnit.ML,
            category: ' Dairy ',
            note: ' Carton ',
        });

        component['addItem']();

        expect(facade.addItem).toHaveBeenCalledWith({
            name: 'Milk',
            amount: 2,
            unit: MeasurementUnit.ML,
            category: 'Dairy',
            note: 'Carton',
        });
        expect(component['itemFormModel']().name).toBe('');
        expect(component['itemFormModel']().amount).toBeNull();
    });

    it('adds an item from quick add input submit', async () => {
        const { facade, fixture } = await setupShoppingListPageAsync();
        const host = fixture.nativeElement as HTMLElement;
        const nameInput = host.querySelector<HTMLInputElement>('.shopping-list__quick-add-input input');
        const submitButton = host.querySelector<HTMLButtonElement>('.shopping-list__quick-add-button button');

        if (nameInput === null || submitButton === null) {
            throw new Error('Expected quick add controls to exist.');
        }

        nameInput.value = 'Wine';
        nameInput.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        submitButton.click();

        expect(facade.addItem).toHaveBeenCalledWith({
            name: 'Wine',
            amount: null,
            unit: null,
            category: null,
            note: null,
        });
    });

    it('does not add invalid or blank items', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        component['itemFormModel'].set({ name: '   ', amount: null, unit: null, category: null, note: null });

        component['addItem']();

        expect(facade.addItem).not.toHaveBeenCalled();
    });

    it('delegates item remove and checked changes', async () => {
        const { component, facade } = await setupShoppingListPageAsync();

        component['removeItem'](SHOPPING_LIST_ITEM.id);
        component['toggleItemChecked'](SHOPPING_LIST_ITEM.id, true);

        expect(facade.removeItem).toHaveBeenCalledWith(SHOPPING_LIST_ITEM.id);
        expect(facade.toggleItemChecked).toHaveBeenCalledWith(SHOPPING_LIST_ITEM.id, true);
    });
});

describe('ShoppingListPageComponent list management', () => {
    it('computes delete and clear availability from list state', async () => {
        const { component, facade } = await setupShoppingListPageAsync();

        expect(component['canDeleteList']()).toBe(true);
        expect(component['canClearList']()).toBe(true);

        facade.lists.set([SHOPPING_LISTS[0]]);
        expect(component['canDeleteList']()).toBe(true);
        expect(component['canClearList']()).toBe(true);
    });

    it('confirms before deleting current list', async () => {
        const { component, dialogService, facade } = await setupShoppingListPageAsync();

        component['deleteCurrentList']();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(facade.deleteListById).toHaveBeenCalledWith(FIRST_LIST_ID);
    });

    it('does not delete when confirmation is cancelled', async () => {
        const { component, dialogService, facade } = await setupShoppingListPageAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });

        component['deleteCurrentList']();

        expect(facade.deleteListById).not.toHaveBeenCalled();
    });

    it('confirms before clearing the only current list', async () => {
        const { component, dialogService, facade } = await setupShoppingListPageAsync();
        facade.lists.set([SHOPPING_LISTS[0]]);

        component['clearCurrentList']();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(facade.clearListById).toHaveBeenCalledWith(FIRST_LIST_ID);
    });

    it('confirms before clearing a list by id', async () => {
        const { component, dialogService, facade } = await setupShoppingListPageAsync();

        component['clearListById'](FIRST_LIST_ID);

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(facade.clearListById).toHaveBeenCalledWith(FIRST_LIST_ID);
    });

    it('closes mobile manage panel when viewport switches to desktop', async () => {
        const { component, fixture, isMobile } = await setupShoppingListPageAsync();
        isMobile.set(true);

        component['toggleMobileManage']();
        expect(component['isMobileManageVisible']()).toBe(true);

        isMobile.set(false);
        fixture.detectChanges();

        expect(component['isMobileManageVisible']()).toBe(false);
    });

    it('creates new list through facade', async () => {
        const { component, facade } = await setupShoppingListPageAsync();

        component['createNewList']();

        expect(facade.createNewList).toHaveBeenCalledOnce();
    });

    it('renames list through facade', async () => {
        const { component, facade } = await setupShoppingListPageAsync();

        component['renameListById'](SECOND_LIST_ID, 'Weekend groceries');

        expect(facade.renameListById).toHaveBeenCalledWith(SECOND_LIST_ID, 'Weekend groceries');
    });
});

async function flushSignalEffectsAsync(): Promise<void> {
    await new Promise(resolve => {
        setTimeout(resolve, 0);
    });
}
