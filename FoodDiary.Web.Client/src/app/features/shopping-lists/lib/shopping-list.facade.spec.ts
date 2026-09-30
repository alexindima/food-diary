import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NEVER, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingList, ShoppingListOverview, ShoppingListSummary } from '../models/shopping-list.data';
import { ShoppingListFacade } from './shopping-list.facade';

const AUTOSAVE_DEBOUNCE_MS = 500;

type ShoppingListServiceMock = {
    create: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
    getOverview: ReturnType<typeof vi.fn>;
    getPage: ReturnType<typeof vi.fn>;
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

describe('ShoppingListFacade navigation and consolidation', () => {
    it('uses summary counts without loading every list', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getOverview.mockReturnValueOnce(
            of(
                makeOverview([
                    { id: 'list-1', name: 'Main', createdAt: '', itemsCount: 0, remainingCount: 0 },
                    { id: 'list-2', name: 'Other', createdAt: '', itemsCount: 1, remainingCount: 0 },
                ]),
            ),
        );
        facade.initialize();
        expect(facade.navigationLists()[1]).toMatchObject({ remainingCount: 0, completed: true });
        expect(shoppingListService.getById).not.toHaveBeenCalled();
    });
    it('distinguishes empty, pending and complete lists immediately', () => {
        const { facade } = setupShoppingListFacade();
        facade.initialize();
        expect(facade.navigationLists()[0]).toMatchObject({ remainingCount: 0, completed: false });
        addMilk(facade);
        expect(facade.navigationLists()[0]).toMatchObject({ remainingCount: 1, completed: false });
        facade.toggleItemChecked(facade.items()[0].id, true);
        expect(facade.navigationLists()[0]).toMatchObject({ remainingCount: 0, completed: true });
        facade.toggleItemChecked(facade.items()[0].id, false);
        expect(facade.navigationLists()[0].remainingCount).toBe(1);
    });
    it('rejects a stale merge preview without overwriting newer edits', () => {
        const { facade } = setupShoppingListFacade();
        facade.initialize();
        const expected = facade.items();
        addMilk(facade);
        expect(facade.applyConsolidation('list-1', expected, [])).toBe(false);
        expect(facade.items()).toHaveLength(1);
        expect(facade.applyConsolidation('list-1', facade.items(), [])).toBe(true);
        expect(facade.items()).toHaveLength(0);
    });
});

describe('ShoppingListFacade loading and selection', () => {
    it('does not duplicate an in-flight detail request', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.getById.mockReturnValue(new Subject<ShoppingList>());
        facade.selectList('list-2');
        facade.selectList('list-2');
        expect(shoppingListService.getById).toHaveBeenCalledExactlyOnceWith('list-2');
    });
    it('should load lists and current list on initialize', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();

        facade.initialize();

        expect(shoppingListService.getOverview).toHaveBeenCalledTimes(1);
        expect(shoppingListService.getPage).not.toHaveBeenCalled();
        facade.selectList('list-1');
        expect(shoppingListService.getById).not.toHaveBeenCalled();
        expect(facade.list()?.id).toBe('list-1');
        expect(facade.selectedListId()).toBe('list-1');
    });

    it('should expose empty state when no lists exist', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getOverview.mockReturnValueOnce(of(makeOverview([])));

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
    it('preserves edits made during a save and uses the assigned item id in the next request', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        facade.initialize();
        addMilk(facade);
        const draft = facade.items()[0];
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        facade.toggleItemChecked(draft.id, true);
        response.next({ ...list, items: [{ ...draft, id: 'saved-item' }] });
        expect(facade.items()[0]).toMatchObject({ id: 'saved-item', isChecked: true });
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            'list-1',
            expect.objectContaining({
                items: [expect.objectContaining({ id: 'saved-item', isChecked: true })],
            }),
        );
    });

    it('saves pending edits before switching lists', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        facade.initialize();
        addMilk(facade);
        facade.selectList('list-2');
        expect(shoppingListService.getById).not.toHaveBeenCalledWith('list-2');
        response.next({ ...list, items: facade.items() });
        expect(shoppingListService.getById).toHaveBeenLastCalledWith('list-2');
    });

    it('rolls back unsaved items and cancels pending navigation on save failure', () => {
        const { facade, shoppingListService, toastService } = setupShoppingListFacade();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.initialize();
        addMilk(facade);
        facade.selectList('list-2');
        expect(facade.items()).toEqual([]);
        expect(shoppingListService.getById).not.toHaveBeenCalledWith('list-2');
        expect(toastService.error).toHaveBeenCalled();
    });
});

describe('ShoppingListFacade persistence operations', () => {
    it('should add item and persist after debounce', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        await waitForAsyncTasksAsync();

        addMilk(facade);

        expect(facade.items()).toHaveLength(1);

        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });

    it('should restore current list when delete fails', async () => {
        const { facade, list, shoppingListService, toastService } = setupShoppingListFacade();
        shoppingListService.deleteById.mockReturnValueOnce(throwError(() => new Error('delete failed')));
        facade.initialize();
        await waitForAsyncTasksAsync();

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
        shoppingListService.getOverview.mockReturnValueOnce(
            of(
                makeOverview([
                    { id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 },
                    { id: 'list-2', name: 'Weekend', createdAt: '', itemsCount: 0 },
                ]),
            ),
        );
        facade.initialize();
        await waitForAsyncTasksAsync();

        facade.deleteListById('list-2');

        expect(shoppingListService.deleteById).toHaveBeenCalledWith('list-2');
        expect(facade.list()).toEqual(list);
        expect(facade.selectedListId()).toBe('list-1');
    });

    it('should delete the last selected list and keep empty state', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        shoppingListService.getPage.mockReturnValueOnce(of([]));
        facade.initialize();
        await waitForAsyncTasksAsync();

        facade.deleteCurrentList();
        await waitForAsyncTasksAsync();

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
        await waitForAsyncTasksAsync();

        facade.setListName('Renamed list');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', expect.objectContaining({ name: 'Renamed list' }));
    });

    it('should rename list by id immediately', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        shoppingListService.update.mockReturnValueOnce(of({ ...list, name: 'Inline rename' }));
        facade.initialize();
        await waitForAsyncTasksAsync();

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
        shoppingListService.getPage.mockReturnValueOnce(NEVER);

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

describe('ShoppingListFacade localized names', () => {
    it.each([
        ['ru', '30.09.2026'],
        ['en', '9/30/2026'],
    ])('uses the %s app language for the default date', (language, dateLabel) => {
        const { facade, shoppingListService } = setupShoppingListFacade(language);
        vi.setSystemTime(new Date('2026-09-30T12:00:00Z'));

        facade.createNewList();

        expect(shoppingListService.create).toHaveBeenCalledWith({ name: `SHOPPING_LIST.NEW_LIST ${dateLabel}` });
    });
});

function setupShoppingListFacade(language = 'ru'): ShoppingListFacadeContext {
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
            { provide: TranslateService, useValue: { instant: (key: string): string => key, getCurrentLang: (): string => language } },
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
        getOverview: vi.fn().mockReturnValue(
            of({
                selectedList: list,
                lists: {
                    items: [{ id: list.id, name: list.name, createdAt: list.createdAt, itemsCount: list.items.length, remainingCount: 0 }],
                    hasMore: false,
                    nextPage: null,
                },
            }),
        ),
        getPage: vi.fn().mockReturnValue(of([{ id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 }])),
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
        note: null,
    });
}

describe('Shopping list creation during navigation', () => {
    it('keeps creation available while loading and queues a single request', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        facade.isLoading.set(true);
        expect(facade.isCreating()).toBe(false);
        facade.createNewList();
        facade.createNewList();
        expect(facade.isCreating()).toBe(true);
        expect(shoppingListService.create).not.toHaveBeenCalled();
        facade.isLoading.set(false);
        TestBed.tick();
        expect(shoppingListService.create).toHaveBeenCalledTimes(1);
        expect(facade.isCreating()).toBe(false);
    });
    it('unblocks creation after a failed request', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.create.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.createNewList();
        expect(facade.isCreating()).toBe(false);
        expect(facade.isLoading()).toBe(false);
    });
});

describe('Named creation and purchased cleanup', () => {
    it('preserves a queued custom name and does not request renaming', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        facade.isLoading.set(true);
        facade.createNewList('  Weekend  ');
        facade.createNewList('Ignored');
        facade.isLoading.set(false);
        TestBed.tick();
        expect(shoppingListService.create).toHaveBeenCalledWith({ name: 'Weekend' });
        expect(facade.renameRequestedListId()).toBeNull();
    });
    it('removes only confirmed items still checked in the same list', () => {
        const { facade } = setupShoppingListFacade();
        facade.initialize();
        const row = { shoppingListId: 'list-1', name: 'Milk', sortOrder: 0 };
        facade.items.set([
            { ...row, id: 'confirmed', isChecked: true },
            { ...row, id: 'unchecked', isChecked: false },
            { ...row, id: 'newly-checked', isChecked: true },
        ]);
        facade.removePurchased('other-list', ['confirmed']);
        expect(facade.items().map(item => item.id)).toEqual(['confirmed', 'unchecked', 'newly-checked']);
        facade.removePurchased('list-1', ['confirmed', 'unchecked']);
        expect(facade.items().map(item => item.id)).toEqual(['unchecked', 'newly-checked']);
    });
});

function makeOverview(items: ShoppingListSummary[]): ShoppingListOverview {
    const first = items.at(0);
    return {
        selectedList: first === undefined ? null : { id: first.id, name: first.name, createdAt: first.createdAt, items: [] },
        lists: { items, hasMore: false, nextPage: null },
    };
}
