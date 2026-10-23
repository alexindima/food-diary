import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdUiPreventUnloadDirective } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { ShoppingList, ShoppingListItem, ShoppingListSummary } from '../../../../shared/models/shopping-list.data';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { ShoppingListFacade } from '../../lib/shopping-list.facade';
import { ShoppingListPageComponent } from './shopping-list-page';

const FIRST_LIST_ID = 'list-1';
const REVISION_LENGTH = 64;
const SECOND_LIST_ID = 'list-2';
const INVALID_SHOPPING_AMOUNT_ABOVE_MAXIMUM = 1_000_001;
const SHOPPING_ITEM_API_LIMITS = { name: 256, category: 128, note: 512 };

const SHOPPING_LIST_ITEM: ShoppingListItem = {
    id: entityId<'shopping-list-item'>('item-1'),
    shoppingListId: entityId<'shopping-list'>(FIRST_LIST_ID),
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
    id: entityId<'shopping-list'>(FIRST_LIST_ID),
    revision: 'a'.repeat(REVISION_LENGTH),
    name: 'Groceries',
    createdAt: utcInstant('2026-05-17T00:00:00Z'),
    items: [SHOPPING_LIST_ITEM],
};

const SHOPPING_LISTS: ShoppingListSummary[] = [
    { id: entityId<'shopping-list'>(FIRST_LIST_ID), name: 'Groceries', createdAt: utcInstant('2026-05-17T00:00:00Z'), itemsCount: 1 },
    { id: entityId<'shopping-list'>(SECOND_LIST_ID), name: 'Party', createdAt: utcInstant('2026-05-18T00:00:00Z'), itemsCount: 0 },
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
    hasUnsavedChanges: ReturnType<typeof vi.fn>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isCreating: ReturnType<typeof signal<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    isClearing: ReturnType<typeof signal<boolean>>;
    saveError: ReturnType<typeof signal<boolean>>;
    conflictListId: ReturnType<typeof signal<ShoppingList['id'] | null>>;
    reloadConflict: ReturnType<typeof vi.fn>;
    retrySave: ReturnType<typeof vi.fn>;
    items: ReturnType<typeof signal<ShoppingListItem[]>>;
    list: ReturnType<typeof signal<ShoppingList | null>>;
    listName: ReturnType<typeof signal<string>>;
    initialPage: ReturnType<typeof signal<null>>;
    navigationLists: ReturnType<typeof signal<ShoppingListSummary[]>>;
    applyConsolidation: ReturnType<typeof vi.fn>;
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

async function setupShoppingListPageAsync(initialUnsaved = false): Promise<ShoppingListPageTestContext> {
    const facade: ShoppingListFacadeMock = {
        list: signal(SHOPPING_LIST),
        items: signal([SHOPPING_LIST_ITEM]),
        isLoading: signal(false),
        isSaving: signal(false),
        isClearing: signal(false),
        saveError: signal(false),
        conflictListId: signal(null),
        reloadConflict: vi.fn(),
        retrySave: vi.fn(),
        isCreating: signal(false),
        lists: signal(SHOPPING_LISTS),
        initialPage: signal(null),
        navigationLists: signal(SHOPPING_LISTS),
        applyConsolidation: vi.fn(),
        selectedListId: signal(FIRST_LIST_ID),
        listName: signal(SHOPPING_LIST.name),
        renameRequestedListId: signal(null),
        initialize: vi.fn(),
        hasUnsavedChanges: vi.fn().mockReturnValue(initialUnsaved),
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
        imports: [ShoppingListPageComponent],
        providers: [
            provideTranslateTesting(),
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

describe('ShoppingListPageComponent pending clear', () => {
    it('blocks item controls during clearing while allowing edits during ordinary autosave', async () => {
        const { facade, fixture } = await setupShoppingListPageAsync();
        const host = fixture.nativeElement as HTMLElement;
        const fieldset = host.querySelector('fieldset');
        if (fieldset === null) {
            throw new Error('Expected item controls fieldset.');
        }
        facade.isSaving.set(true);
        fixture.detectChanges();
        expect(fieldset.disabled).toBe(false);

        facade.isClearing.set(true);
        fixture.detectChanges();
        expect(fieldset.disabled).toBe(true);

        facade.isClearing.set(false);
        fixture.detectChanges();
        expect(fieldset.disabled).toBe(false);
    });
});

describe('ShoppingListPageComponent form and item actions', () => {
    it('passes the product link to the item editor', async () => {
        const { component, facade, dialogService } = await setupShoppingListPageAsync();
        facade.items.set([{ ...SHOPPING_LIST_ITEM, productId: entityId<'product'>('product-1') }]);
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(undefined) });

        component['editItem'](SHOPPING_LIST_ITEM.id);

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(dialogService.open.mock.calls[0]?.[1]).toMatchObject({ data: { productId: 'product-1' } });
    });

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

describe('ShoppingListPageComponent API text limits', () => {
    it.each([
        { field: 'name' as const, limit: 256 },
        { field: 'category' as const, limit: 128 },
        { field: 'note' as const, limit: 512 },
    ])('keeps oversized $field out of the autosaved list', async ({ field, limit }) => {
        const { component, facade, fixture } = await setupShoppingListPageAsync();
        component['itemFormModel'].set({
            name: 'Milk',
            amount: null,
            unit: null,
            category: null,
            note: null,
            [field]: 'A'.repeat(limit + 1),
        });
        fixture.detectChanges();

        component['addItem']();

        expect(facade.addItem).not.toHaveBeenCalled();
        expect(component['itemForm'][field]().invalid()).toBe(true);
        expect(component['itemFormModel']()[field]).toHaveLength(limit + 1);
    });

    it('accepts text fields at the API limits', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        const item = {
            name: 'A'.repeat(SHOPPING_ITEM_API_LIMITS.name),
            amount: null,
            unit: null,
            category: 'B'.repeat(SHOPPING_ITEM_API_LIMITS.category),
            note: 'C'.repeat(SHOPPING_ITEM_API_LIMITS.note),
        };
        component['itemFormModel'].set(item);

        component['addItem']();

        expect(facade.addItem).toHaveBeenCalledWith(item);
    });
});

describe('ShoppingListPageComponent failed autosave', () => {
    it('keeps the list visible and exposes the retry action', async () => {
        const { facade, fixture } = await setupShoppingListPageAsync();
        facade.saveError.set(true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const alert = host.querySelector('fd-ui-inline-alert[role="alert"]');
        expect(alert?.textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
        expect(host.querySelector('.shopping-list__items')).not.toBeNull();

        alert?.querySelector('button')?.click();

        expect(facade.retrySave).toHaveBeenCalledOnce();
    });
});

describe('ShoppingListPageComponent invalid quantity', () => {
    it.each([0, -1, INVALID_SHOPPING_AMOUNT_ABOVE_MAXIMUM])(
        'explains native invalid quantity %s and clears the error after correction',
        async amount => {
            const { component, facade, fixture } = await setupShoppingListPageAsync();
            component['itemFormModel'].update(value => ({ ...value, name: 'Oats' }));
            fixture.detectChanges();
            const element = fixture.nativeElement as HTMLElement;
            const details = element.querySelector<HTMLButtonElement>('button[aria-label="SHOPPING_LIST.ADD_ITEM_DETAILS"]');
            if (details === null) {
                throw new Error('Missing shopping item details button');
            }
            details.click();
            fixture.detectChanges();
            const input = element.querySelector<HTMLInputElement>('input[type="number"]');
            if (input === null) {
                throw new Error('Missing shopping quantity input');
            }
            input.value = String(amount);
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('blur'));
            fixture.detectChanges();
            await fixture.whenStable();
            expect(element.querySelector('[role="alert"]')?.textContent).toContain('SHOPPING_LIST.AMOUNT_ERROR');
            expect(input.getAttribute('aria-invalid')).toBe('true');
            element.querySelector('form.shopping-list__add-form')?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
            expect(facade.addItem).not.toHaveBeenCalled();
            input.value = '1.5';
            input.dispatchEvent(new Event('input', { bubbles: true }));
            fixture.detectChanges();
            await fixture.whenStable();
            expect(element.querySelector('[role="alert"]')).toBeNull();
            expect(input.getAttribute('aria-invalid')).not.toBe('true');
        },
    );

    it.each([0, -1, Number.NaN, Number.POSITIVE_INFINITY])('preserves invalid amount %s and does not submit it', async amount => {
        const { component, facade } = await setupShoppingListPageAsync();
        component['itemFormModel'].set({ name: 'Oats', amount, unit: null, category: null, note: null });
        component['addItem']();
        expect(facade.addItem).not.toHaveBeenCalled();
        expect(component['itemForm'].amount().invalid()).toBe(true);
        expect(component['itemFormModel']().amount).toBe(amount);
    });
});

describe('ShoppingListPageComponent native submit behavior', () => {
    it('prevents native quick add form submit when adding an item', async () => {
        const { component, facade, fixture } = await setupShoppingListPageAsync();
        component['itemFormModel'].update(value => ({ ...value, name: 'Bread' }));
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('.shopping-list__add-form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(facade.addItem).toHaveBeenCalledWith({
            name: 'Bread',
            amount: null,
            unit: null,
            category: null,
            note: null,
        });
    });
});

describe('ShoppingListPageComponent browser navigation', () => {
    it.each([true, false])('requests unload confirmation only while unsaved=%s', async unsaved => {
        const { fixture } = await setupShoppingListPageAsync(unsaved);
        await fixture.whenStable();
        const unload = fixture.debugElement.query(By.directive(FdUiPreventUnloadDirective)).injector.get(FdUiPreventUnloadDirective);
        expect(unload.fdUiPreventUnload()).toBe(unsaved);
        const event = new Event('beforeunload', { cancelable: true });
        window.dispatchEvent(event);
        expect(event.defaultPrevented).toBe(unsaved);
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

        component['clearListById'](entityId<'shopping-list'>(FIRST_LIST_ID));

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

        component['renameListById'](entityId<'shopping-list'>(SECOND_LIST_ID), 'Weekend groceries');

        expect(facade.renameListById).toHaveBeenCalledWith(SECOND_LIST_ID, 'Weekend groceries');
    });
});

async function flushSignalEffectsAsync(): Promise<void> {
    await new Promise(resolve => {
        setTimeout(resolve, 0);
    });
}

describe('Shopping list merge preview', () => {
    it('applies the preview only after confirmation', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        const original = [SHOPPING_LIST_ITEM, { ...SHOPPING_LIST_ITEM, id: entityId<'shopping-list-item'>('duplicate') }];
        facade.items.set(original);
        component['mergeDuplicates']();
        expect(facade.applyConsolidation).toHaveBeenCalledWith(FIRST_LIST_ID, original, [expect.objectContaining({ amount: 24 })]);
    });
    it('leaves items unchanged when cancelled', async () => {
        const { component, facade, dialogService } = await setupShoppingListPageAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        component['mergeDuplicates']();
        expect(facade.applyConsolidation).not.toHaveBeenCalled();
    });
});

describe('Shopping list conflict confirmation', () => {
    it('shows a reload action and preserves the draft when confirmation is cancelled', async () => {
        const { fixture, facade, dialogService } = await setupShoppingListPageAsync();
        facade.conflictListId.set(SHOPPING_LIST.id);
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        fixture.detectChanges();
        const alert = fixture.debugElement.query(By.css('fd-ui-inline-alert[role="alert"]'));
        expect(alert).not.toBeNull();
        alert.triggerEventHandler('primaryAction');
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(facade.reloadConflict).not.toHaveBeenCalled();
        expect(facade.items()).toEqual([SHOPPING_LIST_ITEM]);
    });

    it('reloads the captured conflict target only after confirmation', async () => {
        const { component, facade } = await setupShoppingListPageAsync();
        facade.conflictListId.set(SHOPPING_LIST.id);
        component['reloadConflict']();
        expect(facade.reloadConflict).toHaveBeenCalledExactlyOnceWith(SHOPPING_LIST.id);
    });
});
