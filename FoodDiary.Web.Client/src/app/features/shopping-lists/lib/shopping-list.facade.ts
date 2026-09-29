import { computed, DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { createAutosaveQueue } from '../../../shared/lib/autosave-queue';
import { createClientId } from '../../../shared/lib/client-id.utils';
import type { MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingList, ShoppingListItem, ShoppingListPage, ShoppingListSummary } from '../models/shopping-list.data';
import { mapShoppingListItemToDto, normalizeShoppingListAmount, rebuildShoppingListSortOrder } from './shopping-list-item.mapper';

export type ShoppingListDraftItem = {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | string | null;
    category: string | null;
    note: string | null;
};

@Injectable()
export class ShoppingListFacade {
    private readonly shoppingListService = inject(ShoppingListService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly saveQueue = createAutosaveQueue<void>({
        debounceMs: 500,
        isBusy: () => this.isSaving() || this.isLoading(),
        persist: () => {
            this.persistList();
        },
    });

    private readonly lastLoadedListId = signal<string | null>(null);
    private suppressAutosave = false;
    private pendingSave = false;
    private pendingSelection: string | null = null;

    public readonly list = signal<ShoppingList | null>(null);
    public readonly items = signal<ShoppingListItem[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly initialPage = signal<ShoppingListPage | null>(null);
    private loadingListId: string | null = null;
    public readonly lists = signal<ShoppingListSummary[]>([]);
    private readonly listCounts = signal<Partial<Record<string, { remaining: number; total: number }>>>({});
    public readonly navigationLists = computed(() =>
        this.lists().map(summary => {
            const counts =
                summary.id === this.list()?.id
                    ? { remaining: this.items().filter(item => !item.isChecked).length, total: this.items().length }
                    : this.listCounts()[summary.id];
            return {
                ...summary,
                remainingCount: counts?.remaining ?? summary.remainingCount,
                completed: (counts?.total ?? summary.itemsCount) > 0 && (counts?.remaining ?? summary.remainingCount) === 0,
            };
        }),
    );

    public applyConsolidation(listId: string, expected: readonly ShoppingListItem[], items: ShoppingListItem[]): boolean {
        if (this.list()?.id !== listId || this.items() !== expected || this.isLoading() || this.isSaving()) {
            this.toastService.error(this.translateService.instant('SHOPPING_LIST.MERGE_STALE'));
            return false;
        }
        this.items.set(items);
        this.scheduleSave();
        return true;
    }

    private storeListCounts(list: ShoppingList): void {
        this.listCounts.update(counts => ({
            ...counts,
            [list.id]: {
                remaining: list.items.filter(item => !item.isChecked).length,
                total: list.items.length,
            },
        }));
    }
    public readonly selectedListId = signal<string | null>(null);
    public readonly listName = signal('');
    public readonly renameRequestedListId = signal<string | null>(null);

    private readonly pendingCreate = signal(false);
    private readonly creating = signal(false);
    public readonly isCreating = computed(() => this.pendingCreate() || this.creating());

    public constructor() {
        effect(() => {
            if (this.pendingCreate() && !this.isLoading() && !this.isSaving()) {
                untracked(() => {
                    if (this.saveQueue.hasPending()) {
                        this.saveQueue.flushNow();
                    }
                    if (!this.isSaving()) {
                        this.pendingCreate.set(false);
                        this.performCreate();
                    }
                });
            }
        });
    }

    public initialize(): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getOverview()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: overview => {
                    this.initialPage.set(overview.lists);
                    this.lists.set(overview.lists.items);
                    if (overview.selectedList !== null) {
                        this.applyList(overview.selectedList);
                    } else {
                        this.applyEmptyListState();
                    }
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
            });
    }

    public selectList(id: string): void {
        if (id.length === 0 || id === this.lastLoadedListId() || id === this.loadingListId) {
            return;
        }

        if (this.isSaving() || this.saveQueue.hasPending()) {
            this.pendingSelection = id;
            if (!this.isSaving()) {
                this.saveQueue.flushNow();
            }
            return;
        }
        this.loadListById(id);
    }

    public setListName(name: string): void {
        this.listName.set(name);
        this.scheduleSave();
    }

    public renameListById(listId: string, name: string): void {
        const trimmedName = name.trim();
        if (listId.length === 0 || trimmedName.length === 0) {
            return;
        }

        const current = this.list();
        const previousList = current;
        const previousItems = this.items();
        const previousLists = this.lists();
        const previousListName = this.listName();
        this.lists.set(previousLists.map(entry => (entry.id === listId ? { ...entry, name: trimmedName } : entry)));
        if (current?.id === listId) {
            this.list.set({ ...current, name: trimmedName });
            this.listName.set(trimmedName);
        }

        this.isSaving.set(true);
        this.shoppingListService
            .update(listId, { name: trimmedName })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    if (this.selectedListId() === list.id) {
                        this.applyList(list);
                    }
                    this.updateListSummary(list);
                },
                error: () => {
                    this.isSaving.set(false);
                    this.lists.set(previousLists);
                    this.list.set(previousList);
                    this.items.set(previousItems);
                    this.listName.set(previousListName);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.SAVE_ERROR'));
                },
            });
    }

    private requestedCreateName: string | undefined;

    public createNewList(name?: string | void): void {
        if (this.isCreating()) {
            return;
        }
        const trimmedName = name?.trim() ?? '';
        this.requestedCreateName = trimmedName.length === 0 ? undefined : trimmedName;
        if (this.isLoading() || this.isSaving() || this.saveQueue.hasPending()) {
            this.pendingCreate.set(true);
            return;
        }
        this.performCreate();
    }

    private performCreate(): void {
        this.creating.set(true);
        const explicitName = this.requestedCreateName;
        this.requestedCreateName = undefined;
        const name = explicitName ?? this.buildNewListName();
        this.isLoading.set(true);
        this.shoppingListService
            .create({ name })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.creating.set(false);
                    this.isLoading.set(false);
                    this.renameRequestedListId.set(explicitName === undefined ? list.id : null);
                    this.upsertListSummary(list);
                    this.applyList(list);
                    this.loadLists();
                },
                error: () => {
                    this.creating.set(false);
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.CREATE_ERROR'));
                },
            });
    }

    public addItem(draft: ShoppingListDraftItem): void {
        const current = this.list();
        if (current === null) {
            return;
        }

        const name = draft.name.trim();
        if (name.length === 0) {
            return;
        }

        const nextItems = [
            ...this.items(),
            {
                id: this.createTempId(),
                shoppingListId: current.id,
                name,
                amount: normalizeShoppingListAmount(draft.amount),
                unit: draft.unit ?? null,
                category: draft.category?.trim() ?? null,
                aisle: draft.category?.trim() ?? null,
                note: draft.note?.trim() ?? null,
                productId: null,
                isChecked: false,
                checkedOnUtc: null,
                sources: [],
                sortOrder: this.items().length + 1,
            },
        ];

        this.items.set(nextItems);
        this.scheduleSave();
    }

    public clearRenameRequest(listId: string): void {
        if (this.renameRequestedListId() === listId) {
            this.renameRequestedListId.set(null);
        }
    }

    public editItem(itemId: string, draft: ShoppingListDraftItem): void {
        const name = draft.name.trim();
        if (name.length === 0 || !this.items().some(item => item.id === itemId)) {
            return;
        }
        this.items.update(items =>
            items.map(item =>
                item.id === itemId
                    ? {
                          ...item,
                          name,
                          amount: normalizeShoppingListAmount(draft.amount),
                          unit: draft.unit,
                          category: draft.category?.trim() ?? null,
                          aisle: draft.category === item.category ? item.aisle : (draft.category?.trim() ?? null),
                          note: draft.note?.trim() ?? null,
                      }
                    : item,
            ),
        );
        this.scheduleSave();
    }

    public removePurchased(listId: string, itemIds: readonly string[]): void {
        if (this.list()?.id !== listId || this.isLoading() || this.isSaving()) {
            return;
        }
        const ids = new Set(itemIds);
        const remaining = this.items().filter(item => !item.isChecked || !ids.has(item.id));
        if (remaining.length === this.items().length) {
            return;
        }
        this.items.set(rebuildShoppingListSortOrder(remaining));
        this.scheduleSave();
    }

    public removeItem(itemId: string): void {
        const filtered = this.items().filter(item => item.id !== itemId);
        this.items.set(rebuildShoppingListSortOrder(filtered));
        this.scheduleSave();
    }

    public toggleItemChecked(itemId: string, checked: boolean): void {
        const nextItems = this.items().map(entry =>
            entry.id === itemId
                ? {
                      ...entry,
                      isChecked: checked,
                      checkedOnUtc: checked ? new Date().toISOString() : null,
                  }
                : entry,
        );
        this.items.set(nextItems);
        this.scheduleSave();
    }

    public clearCurrentList(): void {
        const current = this.list();
        if (current === null) {
            return;
        }

        this.clearListById(current.id);
    }

    public clearListById(listId: string): void {
        const current = this.list();
        const summary = this.lists().find(entry => entry.id === listId);
        const listName = current?.id === listId ? current.name : summary?.name;
        if (listName === undefined) {
            return;
        }

        this.isSaving.set(true);
        this.shoppingListService
            .update(listId, { name: listName, items: [] })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    if (this.selectedListId() === list.id) {
                        this.applyList(list);
                    }
                    this.updateListSummary(list);
                },
                error: () => {
                    this.isSaving.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.CLEAR_ERROR'));
                },
            });
    }

    public deleteCurrentList(): void {
        const current = this.list();
        if (current === null) {
            return;
        }

        this.deleteSelectedList(current);
    }

    public deleteListById(listId: string): void {
        const current = this.list();
        if (current?.id === listId) {
            this.deleteSelectedList(current);
            return;
        }

        const previousLists = this.lists();
        if (!previousLists.some(list => list.id === listId)) {
            return;
        }

        this.lists.set(previousLists.filter(list => list.id !== listId));
        this.isSaving.set(true);
        this.shoppingListService
            .deleteById(listId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.loadLists();
                },
                error: () => {
                    this.isSaving.set(false);
                    this.lists.set(previousLists);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.DELETE_ERROR'));
                },
            });
    }

    private deleteSelectedList(current: ShoppingList): void {
        const previousItems = this.items();
        const previousSelectedListId = this.selectedListId();
        const previousListName = this.listName();
        this.suppressAutosave = true;
        this.pendingSave = false;
        this.list.set(null);
        this.items.set([]);
        this.lastLoadedListId.set(null);
        this.selectedListId.set(null);
        this.listName.set('');
        this.isSaving.set(true);

        this.shoppingListService
            .deleteById(current.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.suppressAutosave = false;
                    this.loadLists();
                },
                error: () => {
                    this.isSaving.set(false);
                    this.list.set(current);
                    this.items.set(previousItems);
                    this.lastLoadedListId.set(current.id);
                    this.selectedListId.set(previousSelectedListId);
                    this.listName.set(previousListName);
                    this.suppressAutosave = false;
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.DELETE_ERROR'));
                },
            });
    }

    private loadLists(): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getPage()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: lists => {
                    this.isLoading.set(false);
                    if (lists.length === 0) {
                        this.applyEmptyListState();
                        return;
                    }

                    const pageSize = 20;
                    this.initialPage.set({
                        items: lists,
                        hasMore: lists.length === pageSize,
                        nextPage: lists.length === pageSize ? 2 : null,
                    });
                    const selected = this.list();
                    this.lists.set(lists);
                    if (selected !== null && !lists.some(entry => entry.id === selected.id)) {
                        this.upsertListSummary(selected);
                    }
                    const currentSelection = this.selectedListId();
                    const selectedId =
                        currentSelection !== null && this.lists().some(list => list.id === currentSelection)
                            ? currentSelection
                            : lists[0].id;
                    this.selectedListId.set(selectedId);

                    this.loadListById(selectedId);
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
            });
    }

    private loadListById(id: string): void {
        this.loadingListId = id;
        this.isLoading.set(true);
        this.shoppingListService
            .getById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.loadingListId = null;
                    this.isLoading.set(false);
                    if (list !== null) {
                        this.applyList(list);
                        return;
                    }

                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
                error: () => {
                    this.loadingListId = null;
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
            });
    }

    private applyEmptyListState(): void {
        this.runWithAutosaveSuppressed(() => {
            this.lists.set([]);
            this.list.set(null);
            this.items.set([]);
            this.listName.set('');
            this.selectedListId.set(null);
            this.lastLoadedListId.set(null);
        });
    }

    private applyList(list: ShoppingList): void {
        if (!this.lists().some(entry => entry.id === list.id)) {
            this.upsertListSummary(list);
        }
        this.storeListCounts(list);
        this.runWithAutosaveSuppressed(() => {
            this.list.set(list);
            this.items.set(rebuildShoppingListSortOrder(list.items));
            this.listName.set(list.name);
            this.selectedListId.set(list.id);
            this.lastLoadedListId.set(list.id);
        });
    }

    private runWithAutosaveSuppressed(action: () => void): void {
        this.suppressAutosave = true;
        try {
            action();
        } finally {
            this.suppressAutosave = false;
        }
    }

    private updateListSummary(list: ShoppingList): void {
        this.storeListCounts(list);
        const next = this.lists().map(entry =>
            entry.id === list.id ? { ...entry, name: list.name, itemsCount: list.items.length } : entry,
        );
        this.lists.set(next);
    }

    private upsertListSummary(list: ShoppingList): void {
        const nextSummary: ShoppingListSummary = {
            id: list.id,
            name: list.name,
            createdAt: list.createdAt,
            itemsCount: list.items.length,
        };
        const previousLists = this.lists().filter(entry => entry.id !== list.id);
        this.lists.set([nextSummary, ...previousLists]);
    }

    private buildNewListName(): string {
        const base = this.translateService.instant('SHOPPING_LIST.NEW_LIST');
        const dateLabel = new Date().toLocaleDateString();
        return `${base} ${dateLabel}`;
    }

    private scheduleSave(): void {
        if (this.suppressAutosave) {
            return;
        }

        if (this.isSaving()) {
            this.pendingSave = true;
            this.saveQueue.schedule();
            return;
        }

        this.saveQueue.schedule();
    }

    private persistList(): void {
        const current = this.list();
        if (current === null || this.isSaving() || this.isLoading()) {
            return;
        }

        const name = this.listName().trim();
        if (name.length === 0) {
            return;
        }

        this.isSaving.set(true);
        const submittedItems = this.items();
        const payload = {
            name,
            items: submittedItems.map((item, index) => mapShoppingListItemToDto(item, index)),
        };

        this.shoppingListService
            .update(current.id, payload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    this.updateListSummary(list);
                    if (this.pendingSave) {
                        // Keep newer local edits, but adopt IDs assigned to newly persisted items.
                        const ids = new Map(submittedItems.map((item, index) => [item.id, list.items[index]?.id ?? item.id]));
                        this.list.set(list);
                        this.items.update(items => items.map(item => ({ ...item, id: ids.get(item.id) ?? item.id })));
                        this.pendingSave = false;
                        this.saveQueue.scheduleIfPending();
                    } else {
                        this.applyList(list);
                        const selection = this.pendingSelection;
                        this.pendingSelection = null;
                        if (selection !== null) {
                            this.loadListById(selection);
                        }
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                    this.pendingSave = false;
                    this.pendingSelection = null;
                    this.saveQueue.clearPending();
                    this.applyList(current);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.SAVE_ERROR'));
                },
            });
    }

    private createTempId(): string {
        return createClientId('temp');
    }
}
