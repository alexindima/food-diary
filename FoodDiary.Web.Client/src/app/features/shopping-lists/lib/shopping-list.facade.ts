import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { createAutosaveQueue } from '../../../shared/lib/autosave-queue';
import { type MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListService } from '../api/shopping-list.service';
import { type ShoppingList, type ShoppingListItem, type ShoppingListItemDto, type ShoppingListSummary } from '../models/shopping-list.data';

export type ShoppingListDraftItem = {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
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

    public readonly list = signal<ShoppingList | null>(null);
    public readonly items = signal<ShoppingListItem[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly lists = signal<ShoppingListSummary[]>([]);
    public readonly selectedListId = signal<string | null>(null);
    public readonly listName = signal('');
    public readonly listOptions = computed(() =>
        this.lists().map(list => ({
            value: list.id,
            label: `${list.name} (${list.itemsCount})`,
        })),
    );

    public constructor() {}

    public initialize(): void {
        this.loadLists();
    }

    public selectList(id: string): void {
        if (!id || id === this.lastLoadedListId()) {
            return;
        }

        this.loadListById(id);
    }

    public setListName(name: string): void {
        this.listName.set(name);
        this.scheduleSave();
    }

    public createNewList(): void {
        const name = this.buildNewListName();
        this.isLoading.set(true);
        this.shoppingListService
            .create({ name })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isLoading.set(false);
                    this.applyList(list);
                    this.loadLists();
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.CREATE_ERROR'));
                },
            });
    }

    public addItem(draft: ShoppingListDraftItem): void {
        const name = draft.name.trim();
        if (!name) {
            return;
        }

        const nextItems = [
            ...this.items(),
            {
                id: this.createTempId(),
                shoppingListId: this.list()?.id ?? '',
                name,
                amount: this.normalizeAmount(draft.amount),
                unit: draft.unit ?? null,
                category: draft.category?.trim() || null,
                productId: null,
                isChecked: false,
                sortOrder: this.items().length + 1,
            },
        ];

        this.items.set(nextItems);
        this.scheduleSave();
    }

    public removeItem(itemId: string): void {
        const filtered = this.items().filter(item => item.id !== itemId);
        this.items.set(this.rebuildSortOrder(filtered));
        this.scheduleSave();
    }

    public toggleItemChecked(itemId: string, checked: boolean): void {
        const nextItems = this.items().map(entry => (entry.id === itemId ? { ...entry, isChecked: checked } : entry));
        this.items.set(nextItems);
        this.scheduleSave();
    }

    public clearCurrentList(): void {
        const current = this.list();
        if (!current) {
            return;
        }

        this.isSaving.set(true);
        this.shoppingListService
            .update(current.id, { name: current.name, items: [] })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    this.applyList(list);
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
        if (!current) {
            return;
        }

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
                    this.suppressAutosave = false;
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.DELETE_ERROR'));
                },
            });
    }

    private loadLists(): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: lists => {
                    this.isLoading.set(false);
                    if (lists.length === 0) {
                        this.createDefaultList();
                        return;
                    }

                    this.lists.set(lists);
                    const currentSelection = this.selectedListId();
                    const selectedId =
                        currentSelection && lists.some(list => list.id === currentSelection) ? currentSelection : lists[0].id;
                    this.selectedListId.set(selectedId);
                    this.loadListById(selectedId);
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
            });
    }

    private createDefaultList(): void {
        const name = this.translateService.instant('SHOPPING_LIST.DEFAULT_NAME');
        this.shoppingListService
            .create({ name })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isLoading.set(false);
                    this.applyList(list);
                    this.loadLists();
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.CREATE_ERROR'));
                },
            });
    }

    private loadListById(id: string): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isLoading.set(false);
                    if (list) {
                        this.applyList(list);
                        return;
                    }

                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
                error: () => {
                    this.isLoading.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'));
                },
            });
    }

    private applyList(list: ShoppingList): void {
        this.suppressAutosave = true;
        this.list.set(list);
        this.items.set(this.rebuildSortOrder(list.items));
        this.listName.set(list.name);
        this.selectedListId.set(list.id);
        this.lastLoadedListId.set(list.id);
        queueMicrotask(() => {
            this.suppressAutosave = false;
        });
    }

    private rebuildSortOrder(items: ShoppingListItem[]): ShoppingListItem[] {
        return items.map((item, index) => ({
            ...item,
            sortOrder: index + 1,
        }));
    }

    private normalizeAmount(value: number | null): number | null {
        if (value === null) {
            return null;
        }
        const parsed = Number(value);
        return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
    }

    private mapItemToDto(item: ShoppingListItem, index: number): ShoppingListItemDto {
        return {
            productId: item.productId ?? null,
            name: item.name,
            amount: item.amount ?? null,
            unit: item.unit ?? null,
            category: item.category ?? null,
            isChecked: item.isChecked,
            sortOrder: index + 1,
        };
    }

    private updateListSummary(list: ShoppingList): void {
        const next = this.lists().map(entry =>
            entry.id === list.id ? { ...entry, name: list.name, itemsCount: list.items.length } : entry,
        );
        this.lists.set(next);
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
            return;
        }

        this.saveQueue.schedule(undefined);
    }

    private persistList(): void {
        const current = this.list();
        if (!current || this.isSaving() || this.isLoading()) {
            return;
        }

        const name = this.listName().trim();
        if (!name) {
            return;
        }

        this.isSaving.set(true);
        const payload = {
            name,
            items: this.items().map((item, index) => this.mapItemToDto(item, index)),
        };

        this.shoppingListService
            .update(current.id, payload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    this.applyList(list);
                    this.updateListSummary(list);
                    if (this.pendingSave) {
                        this.pendingSave = false;
                        this.saveQueue.scheduleIfPending();
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                    this.toastService.error(this.translateService.instant('SHOPPING_LIST.SAVE_ERROR'));
                },
            });
    }

    private createTempId(): string {
        return `temp-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
    }
}
