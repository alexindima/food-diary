import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NEVER, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { MeasurementUnit } from '../../../shared/models/product.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList, ShoppingListOverview, ShoppingListSummary } from '../../../shared/models/shopping-list.data';
import { ShoppingListService } from '../api/shopping-list.service';
import { ShoppingListFacade } from './shopping-list.facade';

const AUTOSAVE_DEBOUNCE_MS = 500;
const REVISION_LENGTH = 64;

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
                    { id: entityId<'shopping-list'>('list-1'), name: 'Main', createdAt: utcInstant(''), itemsCount: 0, remainingCount: 0 },
                    { id: entityId<'shopping-list'>('list-2'), name: 'Other', createdAt: utcInstant(''), itemsCount: 1, remainingCount: 0 },
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
        expect(facade.applyConsolidation(entityId<'shopping-list'>('list-1'), expected, [])).toBe(false);
        expect(facade.items()).toHaveLength(1);
        expect(facade.applyConsolidation(entityId<'shopping-list'>('list-1'), facade.items(), [])).toBe(true);
        expect(facade.items()).toHaveLength(0);
    });
});

describe('ShoppingListFacade loading and selection', () => {
    it('does not duplicate an in-flight detail request', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.getById.mockReturnValue(new Subject<ShoppingList>());
        facade.selectList(entityId<'shopping-list'>('list-2'));
        facade.selectList(entityId<'shopping-list'>('list-2'));
        expect(shoppingListService.getById).toHaveBeenCalledExactlyOnceWith('list-2');
    });
    it('should load lists and current list on initialize', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();

        facade.initialize();

        expect(shoppingListService.getOverview).toHaveBeenCalledTimes(1);
        expect(shoppingListService.getPage).not.toHaveBeenCalled();
        facade.selectList(entityId<'shopping-list'>('list-1'));
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

        facade.selectList(entityId<'shopping-list'>(''));
        facade.selectList(entityId<'shopping-list'>('list-1'));

        expect(shoppingListService.getById).not.toHaveBeenCalled();
    });
});

describe('ShoppingListFacade competing selections', () => {
    it('ignores an old success after the newest selected list has loaded', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const older = new Subject<ShoppingList>();
        const newer = new Subject<ShoppingList>();
        shoppingListService.getById.mockReturnValueOnce(older).mockReturnValueOnce(newer);
        facade.initialize();

        facade.selectList(entityId<'shopping-list'>('list-2'));
        facade.selectList(entityId<'shopping-list'>('list-3'));
        newer.next({ ...list, id: entityId<'shopping-list'>('list-3'), name: 'Newest' });
        older.next({ ...list, id: entityId<'shopping-list'>('list-2'), name: 'Older' });

        expect(facade.selectedListId()).toBe('list-3');
        expect(facade.listName()).toBe('Newest');
        expect(facade.isLoading()).toBe(false);
    });

    it('does not end the newest loading state or clear its deduplication after an old error', () => {
        const { facade, list, shoppingListService, toastService } = setupShoppingListFacade();
        const older = new Subject<ShoppingList>();
        const newer = new Subject<ShoppingList>();
        shoppingListService.getById.mockReturnValueOnce(older).mockReturnValueOnce(newer);
        facade.initialize();

        facade.selectList(entityId<'shopping-list'>('list-2'));
        facade.selectList(entityId<'shopping-list'>('list-3'));
        older.error(new Error('old request offline'));
        facade.selectList(entityId<'shopping-list'>('list-3'));

        expect(facade.isLoading()).toBe(true);
        expect(shoppingListService.getById).toHaveBeenCalledTimes(2);
        expect(toastService.error).not.toHaveBeenCalled();
        newer.next({ ...list, id: entityId<'shopping-list'>('list-3') });
        expect(facade.isLoading()).toBe(false);
        expect(facade.selectedListId()).toBe('list-3');
    });

    it('does not show an old error after the newest selected list has loaded', () => {
        const { facade, list, shoppingListService, toastService } = setupShoppingListFacade();
        const older = new Subject<ShoppingList>();
        const newer = new Subject<ShoppingList>();
        shoppingListService.getById.mockReturnValueOnce(older).mockReturnValueOnce(newer);
        facade.initialize();

        facade.selectList(entityId<'shopping-list'>('list-2'));
        facade.selectList(entityId<'shopping-list'>('list-3'));
        newer.next({ ...list, id: entityId<'shopping-list'>('list-3') });
        older.error(new Error('old request offline'));

        expect(facade.selectedListId()).toBe('list-3');
        expect(toastService.error).not.toHaveBeenCalled();
        expect(facade.isLoading()).toBe(false);
    });
});

describe('ShoppingListFacade saves before leaving', () => {
    it('reports unsaved changes during debounce and request, then clears after success', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        facade.initialize();
        expect(facade.hasUnsavedChanges()).toBe(false);
        addMilk(facade);
        expect(facade.hasUnsavedChanges()).toBe(true);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(facade.hasUnsavedChanges()).toBe(true);
        response.next({ ...list, items: facade.items() });
        expect(facade.hasUnsavedChanges()).toBe(false);
    });

    it('flushes a pending edit and waits for its response before leaving', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        facade.initialize();
        addMilk(facade);
        let finished = false;
        const navigation = facade.saveBeforeLeaveAsync().then(result => {
            finished = true;
            return result;
        });
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
        await waitForAsyncTasksAsync();
        expect(finished).toBe(false);
        response.next({ ...list, items: facade.items() });
        expect(await navigation).toBe(true);
    });

    it('prevents leaving when the flushed save fails synchronously', async () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        addMilk(facade);
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        expect(await facade.saveBeforeLeaveAsync()).toBe(false);
    });

    it('saves edits made during an in-flight request before leaving', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const first = new Subject<ShoppingList>();
        const second = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(first).mockReturnValueOnce(second);
        facade.initialize();
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        facade.toggleItemChecked(facade.items()[0].id, true);
        const navigation = facade.saveBeforeLeaveAsync();
        first.next({ ...list, items: [{ ...facade.items()[0], id: entityId<'shopping-list-item'>('saved-item'), isChecked: false }] });
        await waitForAsyncTasksAsync();
        expect(shoppingListService.update).toHaveBeenCalledTimes(2);
        second.next({ ...list, items: facade.items() });
        expect(await navigation).toBe(true);
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
        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', expect.objectContaining({ expectedRevision: list.revision }));
        facade.toggleItemChecked(draft.id, true);
        response.next({
            ...list,
            revision: 'b'.repeat(REVISION_LENGTH),
            items: [{ ...draft, id: entityId<'shopping-list-item'>('saved-item') }],
        });
        expect(facade.items()[0]).toMatchObject({ id: 'saved-item', isChecked: true });
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            'list-1',
            expect.objectContaining({
                expectedRevision: 'b'.repeat(REVISION_LENGTH),
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
        facade.selectList(entityId<'shopping-list'>('list-2'));
        expect(shoppingListService.getById).not.toHaveBeenCalledWith('list-2');
        response.next({ ...list, items: facade.items() });
        expect(shoppingListService.getById).toHaveBeenLastCalledWith('list-2');
    });

    it('keeps unsaved items and cancels pending navigation on save failure', () => {
        const { facade, shoppingListService, toastService } = setupShoppingListFacade();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.initialize();
        addMilk(facade);
        facade.selectList(entityId<'shopping-list'>('list-2'));
        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        expect(facade.hasUnsavedChanges()).toBe(true);
        expect(facade.saveError()).toBe(true);
        expect(shoppingListService.getById).not.toHaveBeenCalledWith('list-2');
        expect(toastService.error).toHaveBeenCalled();
    });
});

describe('ShoppingListFacade failed save recovery', () => {
    it('retries the retained draft and clears the error only after saving it', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        const draft = facade.items();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);

        facade.retrySave();

        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            'list-1',
            expect.objectContaining({ items: [expect.objectContaining({ name: 'Milk' })] }),
        );
        expect(facade.hasUnsavedChanges()).toBe(true);
        response.next({ ...list, items: draft });
        expect(facade.hasUnsavedChanges()).toBe(false);
        expect(facade.saveError()).toBe(false);
        expect(facade.items()).toEqual(draft);
    });

    it('preserves newer edits after failure and does not automatically repeat failed requests or create a new list', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        facade.toggleItemChecked(facade.items()[0].id, true);
        facade.createNewList('Next list');
        response.error(new Error('offline'));
        TestBed.tick();
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk', isChecked: true })]);
        expect(facade.hasUnsavedChanges()).toBe(true);
        expect(facade.isCreating()).toBe(false);
        expect(shoppingListService.create).not.toHaveBeenCalled();
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });
});

describe('ShoppingListFacade immediate failed save recovery', () => {
    it('retains a failed navigation draft without retrying when its original debounce expires', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        addMilk(facade);

        facade.selectList(entityId<'shopping-list'>('list-2'));
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
        expect(shoppingListService.getById).not.toHaveBeenCalledWith('list-2');
        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        expect(facade.saveError()).toBe(true);
        expect(facade.hasUnsavedChanges()).toBe(true);
    });

    it('cancels a pending creation when its immediate prerequisite save fails synchronously', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new Error('offline')));
        addMilk(facade);

        facade.createNewList('Next list');
        TestBed.tick();
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        expect(shoppingListService.create).not.toHaveBeenCalled();
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
        expect(facade.isCreating()).toBe(false);
        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        expect(facade.saveError()).toBe(true);
        expect(facade.hasUnsavedChanges()).toBe(true);
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
                    { id: entityId<'shopping-list'>('list-1'), name: 'Main list', createdAt: utcInstant(''), itemsCount: 0 },
                    { id: entityId<'shopping-list'>('list-2'), name: 'Weekend', createdAt: utcInstant(''), itemsCount: 0 },
                ]),
            ),
        );
        facade.initialize();
        await waitForAsyncTasksAsync();

        facade.deleteListById(entityId<'shopping-list'>('list-2'));

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

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', {
            expectedRevision: 'a'.repeat(REVISION_LENGTH),
            name: 'Main list',
            items: [],
        });
        expect(facade.items()).toHaveLength(1);
        expect(facade.isSaving()).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith('SHOPPING_LIST.CLEAR_ERROR');
    });
});

describe('ShoppingListFacade pending clear', () => {
    it.each(['success', 'failure'])('locks item editing until a pending clear finishes with %s', outcome => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const cleared = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(cleared);
        facade.initialize();
        addMilk(facade);
        const items = facade.items();

        facade.clearListById(list.id);
        expect(facade.isClearing()).toBe(true);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        if (outcome === 'success') {
            cleared.next(list);
            expect(facade.items()).toEqual([]);
            expect(facade.hasUnsavedChanges()).toBe(false);
        } else {
            cleared.error(new Error('offline'));
            expect(facade.items()).toEqual(items);
            vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
            expect(shoppingListService.update).toHaveBeenLastCalledWith(
                list.id,
                expect.objectContaining({ items: [expect.objectContaining({ name: 'Milk' })] }),
            );
        }
        expect(facade.isClearing()).toBe(false);
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

        facade.renameListById(entityId<'shopping-list'>('list-1'), ' Inline rename ');

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', { expectedRevision: list.revision, name: 'Inline rename' });
        expect(facade.listName()).toBe('Inline rename');
    });

    it.each(['before', 'during'])('preserves item edits made %s an inline rename and saves them afterward', timing => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        const renamed = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(renamed);
        facade.initialize();
        if (timing === 'before') {
            addMilk(facade);
        }

        facade.renameListById(list.id, 'Renamed');
        if (timing === 'during') {
            addMilk(facade);
        }
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        renamed.next({ ...list, name: 'Renamed' });

        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        expect(facade.listName()).toBe('Renamed');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            list.id,
            expect.objectContaining({ name: 'Renamed', items: [expect.objectContaining({ name: 'Milk' })] }),
        );
    });

    it('keeps newer item edits and resumes their save when an inline rename fails', () => {
        const { facade, list, shoppingListService, toastService } = setupShoppingListFacade();
        const renamed = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(renamed);
        facade.initialize();
        facade.renameListById(list.id, 'Renamed');
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);

        renamed.error(new Error('offline'));

        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        expect(facade.listName()).toBe(list.name);
        expect(toastService.error).toHaveBeenCalledWith('SHOPPING_LIST.SAVE_ERROR');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            list.id,
            expect.objectContaining({ name: list.name, items: [expect.objectContaining({ name: 'Milk' })] }),
        );
    });
});

describe('ShoppingListFacade new list summaries', () => {
    it('should expose a newly created list summary before the list reload finishes', () => {
        const { facade, shoppingListService } = setupShoppingListFacade();
        const createdList: ShoppingList = {
            id: entityId<'shopping-list'>('list-2'),
            revision: 'a'.repeat(REVISION_LENGTH),
            name: 'New list',
            createdAt: utcInstant('2026-06-06T00:00:00Z'),
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
        id: entityId<'shopping-list'>('list-1'),
        revision: 'a'.repeat(REVISION_LENGTH),
        name: 'Main list',
        createdAt: utcInstant('2026-01-01T00:00:00Z'),
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
        getPage: vi
            .fn()
            .mockReturnValue(
                of([{ id: entityId<'shopping-list'>('list-1'), name: 'Main list', createdAt: utcInstant(''), itemsCount: 0 }]),
            ),
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
            {
                ...row,
                id: entityId<'shopping-list-item'>('confirmed'),
                isChecked: true,
                shoppingListId: entityId<'shopping-list'>(row.shoppingListId),
            },
            {
                ...row,
                id: entityId<'shopping-list-item'>('unchecked'),
                isChecked: false,
                shoppingListId: entityId<'shopping-list'>(row.shoppingListId),
            },
            {
                ...row,
                id: entityId<'shopping-list-item'>('newly-checked'),
                isChecked: true,
                shoppingListId: entityId<'shopping-list'>(row.shoppingListId),
            },
        ]);
        facade.removePurchased(entityId<'shopping-list'>('other-list'), [entityId<'shopping-list-item'>('confirmed')]);
        expect(facade.items().map(item => item.id)).toEqual(['confirmed', 'unchecked', 'newly-checked']);
        facade.removePurchased(entityId<'shopping-list'>('list-1'), [
            entityId<'shopping-list-item'>('confirmed'),
            entityId<'shopping-list-item'>('unchecked'),
        ]);
        expect(facade.items().map(item => item.id)).toEqual(['unchecked', 'newly-checked']);
    });
});

function makeOverview(items: ShoppingListSummary[]): ShoppingListOverview {
    const first = items.at(0);
    return {
        selectedList:
            first === undefined
                ? null
                : { id: first.id, revision: 'a'.repeat(REVISION_LENGTH), name: first.name, createdAt: first.createdAt, items: [] },
        lists: { items, hasMore: false, nextPage: null },
    };
}

describe('ShoppingListFacade conditional updates', () => {
    it('resumes the selected draft autosave after a nonselected clear finishes past the debounce', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const other = { ...list, id: entityId<'shopping-list'>('list-2'), revision: 'b'.repeat(REVISION_LENGTH) };
        facade.lists.update(lists => [...lists, { id: other.id, name: other.name, createdAt: other.createdAt, itemsCount: 1 }]);
        shoppingListService.getById.mockReturnValueOnce(of(other));
        const cleared = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(cleared);
        addMilk(facade);
        facade.clearListById(other.id);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
        cleared.next({ ...other, revision: 'c'.repeat(REVISION_LENGTH) });
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            list.id,
            expect.objectContaining({ expectedRevision: list.revision, items: [expect.objectContaining({ name: 'Milk' })] }),
        );
        expect(shoppingListService.update).toHaveBeenCalledTimes(2);
    });
    it.each(['rename', 'clear'] as const)('loads the target detail revision before a nonselected %s', operation => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const target = {
            ...list,
            id: entityId<'shopping-list'>('list-2'),
            name: 'Other saved list',
            revision: 'b'.repeat(REVISION_LENGTH),
        };
        facade.lists.update(lists => [...lists, { id: target.id, name: 'Old summary name', createdAt: target.createdAt, itemsCount: 1 }]);
        const baseline = new Subject<ShoppingList>();
        shoppingListService.getById.mockReturnValueOnce(baseline);
        shoppingListService.update.mockReturnValueOnce(of({ ...target, revision: 'c'.repeat(REVISION_LENGTH) }));

        if (operation === 'rename') {
            facade.renameListById(target.id, 'Renamed target');
        } else {
            facade.clearListById(target.id);
        }
        expect(shoppingListService.getById).toHaveBeenCalledExactlyOnceWith(target.id);
        expect(shoppingListService.update).not.toHaveBeenCalled();
        baseline.next(target);
        expect(shoppingListService.update).toHaveBeenCalledExactlyOnceWith(
            target.id,
            operation === 'rename'
                ? { expectedRevision: target.revision, name: 'Renamed target' }
                : { expectedRevision: target.revision, name: target.name, items: [] },
        );
        expect(facade.list()).toEqual(list);
    });

    it('adopts a rename response revision while retaining newer local name and items', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        facade.renameListById(list.id, 'First rename');
        addMilk(facade);
        facade.setListName('Newer draft name');
        response.next({ ...list, name: 'First rename', revision: 'b'.repeat(REVISION_LENGTH) });
        expect(facade.listName()).toBe('Newer draft name');
        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk' })]);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(
            list.id,
            expect.objectContaining({
                expectedRevision: 'b'.repeat(REVISION_LENGTH),
                name: 'Newer draft name',
                items: [expect.objectContaining({ name: 'Milk' })],
            }),
        );
    });
});

describe('ShoppingListFacade unavailable update baseline', () => {
    it.each(['rename', 'clear'] as const)(
        'does not borrow the selected revision for a nonselected %s when detail loading fails',
        operation => {
            const { facade, list, shoppingListService } = setupShoppingListFacade();
            facade.initialize();
            const otherId = entityId<'shopping-list'>('list-2');
            facade.lists.update(lists => [...lists, { id: otherId, name: 'Other', createdAt: list.createdAt, itemsCount: 1 }]);
            shoppingListService.getById.mockReturnValueOnce(of(null));
            if (operation === 'rename') {
                facade.renameListById(otherId, 'Draft rename');
            } else {
                facade.clearListById(otherId);
            }
            expect(shoppingListService.update).not.toHaveBeenCalled();
            expect(facade.list()).toEqual(list);
            expect(facade.isSaving()).toBe(false);
            expect(facade.isClearing()).toBe(false);
        },
    );
});

describe('ShoppingListFacade revision conflict recovery', () => {
    it('keeps the nonselected conflict target distinct and reloads that target after confirmation', () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const other = { ...list, id: entityId<'shopping-list'>('list-2'), name: 'Other', revision: 'b'.repeat(REVISION_LENGTH) };
        facade.lists.update(lists => [...lists, { id: other.id, name: other.name, createdAt: other.createdAt, itemsCount: 0 }]);
        shoppingListService.getById.mockReturnValueOnce(of(other));
        shoppingListService.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
        facade.renameListById(other.id, 'Draft other name');
        expect(facade.conflictListId()).toBe(other.id);
        expect(facade.list()).toEqual(list);
        expect(facade.lists().find(summary => summary.id === other.id)?.name).toBe('Draft other name');
        facade.reloadConflict(list.id);
        expect(shoppingListService.getById).toHaveBeenCalledTimes(1);
        const fresh = { ...other, name: 'Other client name', revision: 'c'.repeat(REVISION_LENGTH) };
        shoppingListService.getById.mockReturnValueOnce(of(fresh));
        facade.reloadConflict(other.id);
        expect(facade.list()).toEqual(fresh);
        expect(facade.selectedListId()).toBe(other.id);
        expect(facade.hasUnsavedChanges()).toBe(false);
        expect(facade.conflictListId()).toBeNull();
    });
    it('retains a conflicted draft and stops autosave, retries, navigation and queued creation', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        const response = new Subject<ShoppingList>();
        shoppingListService.update.mockReturnValueOnce(response);
        addMilk(facade);
        facade.setListName('Draft name');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        facade.toggleItemChecked(facade.items()[0].id, true);
        facade.createNewList('Next');
        response.error(new HttpErrorResponse({ status: HttpStatusCode.Conflict }));
        TestBed.tick();
        facade.retrySave();
        facade.selectList(entityId<'shopping-list'>('list-2'));
        facade.createNewList('Blocked');
        expect(await facade.saveBeforeLeaveAsync()).toBe(false);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS * 2);

        expect(facade.conflictListId()).toBe(list.id);
        expect(facade.listName()).toBe('Draft name');
        expect(facade.items()).toEqual([expect.objectContaining({ name: 'Milk', isChecked: true })]);
        expect(facade.hasUnsavedChanges()).toBe(true);
        expect(facade.saveError()).toBe(false);
        expect(facade.isCreating()).toBe(false);
        expect(shoppingListService.create).not.toHaveBeenCalled();
        expect(shoppingListService.getById).not.toHaveBeenCalled();
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });
});

describe('ShoppingListFacade conflicted mutations', () => {
    it.each(['rename', 'clear'] as const)('retains selected draft after a %s conflict', operation => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        addMilk(facade);
        shoppingListService.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
        if (operation === 'rename') {
            facade.renameListById(list.id, 'Draft rename');
        } else {
            facade.clearListById(list.id);
        }
        const draft = facade.items();
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(facade.conflictListId()).toBe(list.id);
        expect(facade.items()).toEqual(draft);
        expect(facade.items()).toHaveLength(1);
        expect(facade.listName()).toBe(operation === 'rename' ? 'Draft rename' : list.name);
        expect(facade.hasUnsavedChanges()).toBe(true);
        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });

    it.each([null, 'error'] as const)('keeps the draft and conflict when confirmed reload returns %s', result => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        const draft = facade.items();
        shoppingListService.getById.mockReturnValueOnce(result === null ? of(null) : throwError(() => new Error('offline')));
        facade.reloadConflict(list.id);
        expect(facade.items()).toEqual(draft);
        expect(facade.conflictListId()).toBe(list.id);
        expect(facade.hasUnsavedChanges()).toBe(true);
        expect(facade.isLoading()).toBe(false);
    });

    it('replaces a draft only after a successful confirmed reload and uses the returned revision', async () => {
        const { facade, list, shoppingListService } = setupShoppingListFacade();
        facade.initialize();
        shoppingListService.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
        addMilk(facade);
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        const fresh = { ...list, name: 'Other client saved', revision: 'b'.repeat(REVISION_LENGTH) };
        shoppingListService.getById.mockReturnValueOnce(of(fresh));
        facade.reloadConflict(list.id);
        expect(facade.items()).toEqual([]);
        expect(facade.listName()).toBe(fresh.name);
        expect(facade.conflictListId()).toBeNull();
        expect(facade.hasUnsavedChanges()).toBe(false);
        expect(await facade.saveBeforeLeaveAsync()).toBe(true);
        facade.setListName('Next edit');
        vi.advanceTimersByTime(AUTOSAVE_DEBOUNCE_MS);
        expect(shoppingListService.update).toHaveBeenLastCalledWith(list.id, {
            expectedRevision: fresh.revision,
            name: 'Next edit',
            items: [],
        });
    });
});
