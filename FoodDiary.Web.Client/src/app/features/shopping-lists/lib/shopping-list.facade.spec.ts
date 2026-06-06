import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NEVER, of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingList } from '../models/shopping-list.data';
import { ShoppingListFacade } from './shopping-list.facade';

const AUTOSAVE_DEBOUNCE_MS = 500;

type ShoppingListServiceMock = {
    create: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};

type ShoppingListFacadeContext = {
    facade: ShoppingListFacade;
    list: ShoppingList;
    shoppingListService: ShoppingListServiceMock;
    toastService: { error: ReturnType<typeof vi.fn>; open: ReturnType<typeof vi.fn> };
};

afterEach(() => {
    vi.useRealTimers();
});

describe('ShoppingListFacade loading and selection', () => {
    it('should load lists and current list on initialize', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();

        facade.initialize();

        expect(shoppingListService.getAll).toHaveBeenCalledTimes(1);
        expect(shoppingListService.getById).toHaveBeenCalledWith('list-1');
        expect(facade.list()?.id).toBe('list-1');
        expect(facade.selectedListId()).toBe('list-1');
    });

    it('should expose empty state when no lists exist', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getAll.mockReturnValueOnce(of([]));

        facade.initialize();

        expect(shoppingListService.create).not.toHaveBeenCalled();
        expect(facade.lists()).toEqual([]);
        expect(facade.list()).toBeNull();
        expect(facade.items()).toEqual([]);
        expect(facade.selectedListId()).toBeNull();
        expect(facade.listName()).toBe('');
    });

    it('should ignore empty and already selected list selections', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.getById.mockClear();

        facade.selectList('');
        facade.selectList('list-1');

        expect(shoppingListService.getById).not.toHaveBeenCalled();
    });
});

describe('ShoppingListFacade item persistence and errors', () => {
    it('should add item and persist after debounce', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        await Promise.resolve();

        addMilk(facade);

        expect(facade.items()).toHaveLength(1);

        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });

    it('should restore current list when delete fails', async () => {
        const { facade, list, shoppingListService, toastService } = setupShoppingListFacade();
        shoppingListService.deleteById.mockReturnValueOnce(throwError(() => new Error('delete failed')));
        facade.initialize();
        await Promise.resolve();

        facade.deleteCurrentList();

        expect(shoppingListService.deleteById).toHaveBeenCalledWith('list-1');
        expect(facade.list()).toEqual(list);
        expect(facade.items()).toEqual([]);
        expect(facade.selectedListId()).toBe('list-1');
        expect(facade.listName()).toBe('Main list');
        expect(toastService.error).toHaveBeenCalledWith('SHOPPING_LIST.DELETE_ERROR');
    });

    it('should delete inactive list without replacing the current list', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getAll.mockReturnValueOnce(
            of([
                { id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 },
                { id: 'list-2', name: 'Weekend', createdAt: '', itemsCount: 0 },
            ]),
        );
        facade.initialize();
        await Promise.resolve();

        facade.deleteListById('list-2');

        expect(shoppingListService.deleteById).toHaveBeenCalledWith('list-2');
        expect(facade.list()).toEqual(list);
        expect(facade.selectedListId()).toBe('list-1');
    });

    it('should delete the last selected list and keep empty state', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getAll.mockReturnValueOnce(of([{ id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 }]));
        shoppingListService.getAll.mockReturnValueOnce(of([]));
        facade.initialize();
        await Promise.resolve();

        facade.deleteCurrentList();
        await Promise.resolve();

        expect(shoppingListService.deleteById).toHaveBeenCalledWith('list-1');
        expect(facade.lists()).toEqual([]);
        expect(facade.list()).toBeNull();
        expect(facade.selectedListId()).toBeNull();
    });

    it('should keep current items and show error when clearing list fails', () => {
        const { facade, shoppingListService, toastService } = setupShoppingListFacade();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('clear failed')));
        facade.initialize();

        addMilk(facade);
        facade.clearCurrentList();

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', { name: 'Main list', items: [] });
        expect(facade.items()).toHaveLength(1);
        expect(facade.isSaving()).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith('SHOPPING_LIST.CLEAR_ERROR');
    });
});

describe('ShoppingListFacade autosave', () => {
    it('should persist renamed list after debounce', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        await Promise.resolve();

        facade.setListName('Renamed list');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', expect.objectContaining({ name: 'Renamed list' }));
    });

    it('should rename list by id immediately', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        shoppingListService.update.mockReturnValueOnce(of({ ...list, name: 'Inline rename' }));
        facade.initialize();
        await Promise.resolve();

        facade.renameListById('list-1', ' Inline rename ');

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', { name: 'Inline rename' });
        expect(facade.listName()).toBe('Inline rename');
    });

    it('should expose a newly created list summary before the list reload finishes', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        const createdList: ShoppingList = {
            id: 'list-2',
            name: 'New list',
            createdAt: '2026-06-06T00:00:00Z',
            items: [],
        };
        shoppingListService.create.mockReturnValueOnce(of(createdList));
        shoppingListService.getAll.mockReturnValueOnce(NEVER);

        facade.createNewList();

        expect(facade.selectedListId()).toBe(createdList.id);
        expect(facade.renameRequestedListId()).toBe(createdList.id);
        expect(facade.lists()[0]).toEqual({
            id: createdList.id,
            name: createdList.name,
            createdAt: createdList.createdAt,
            itemsCount: 0,
        });

        facade.clearRenameRequest(createdList.id);

        expect(facade.renameRequestedListId()).toBeNull();
    });
});

function setupShoppingListFacade(): ShoppingListFacadeContext {
    vi.useFakeTimers();

    const list: ShoppingList = {
        id: 'list-1',
        name: 'Main list',
        createdAt: '2026-01-01T00:00:00Z',
        items: [],
    };
    const shoppingListService = createShoppingListServiceMock(list);
    const toastService = { open: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
        providers: [
            ShoppingListFacade,
            { provide: ShoppingListService, useValue: shoppingListService },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            { provide: FdUiToastService, useValue: toastService },
        ],
    });

    return {
        facade: TestBed.inject(ShoppingListFacade),
        list,
        shoppingListService,
        toastService,
    };
}

function createShoppingListServiceMock(list: ShoppingList): ShoppingListServiceMock {
    return {
        getAll: vi.fn().mockReturnValue(of([{ id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 }])),
        getById: vi.fn().mockReturnValue(of(list)),
        create: vi.fn().mockReturnValue(of(list)),
        update: vi.fn().mockReturnValue(of(list)),
        deleteById: vi.fn().mockReturnValue(of(void 0)),
    };
}

function addMilk(facade: ShoppingListFacade): void {
    facade.addItem({
        name: 'Milk',
        amount: 1,
        unit: MeasurementUnit.ML,
        category: 'Dairy',
    });
}
