import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    ElementRef,
    inject,
    input,
    output,
    signal,
    untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiMenuComponent } from 'fd-ui-kit/menu/fd-ui-menu';
import { FdUiMenuDividerComponent } from 'fd-ui-kit/menu/fd-ui-menu-divider';
import { FdUiMenuItemComponent } from 'fd-ui-kit/menu/fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from 'fd-ui-kit/menu/fd-ui-menu-trigger.directive';

import { ShoppingListBrowserComponent } from '../../components/shopping-list-browser/shopping-list-browser';
import {
    type ShoppingListBrowserData,
    ShoppingListBrowserDialogComponent,
    type ShoppingListBrowserResult,
} from '../../dialogs/shopping-list-browser-dialog/shopping-list-browser-dialog';
import type { ShoppingListPage, ShoppingListSummary } from '../../models/shopping-list.data';

const RENAME_FOCUS_DELAY_MS = 0;
const QUICK_LIST_LIMIT = 3;

@Component({
    selector: 'fd-shopping-list-manage-controls',
    imports: [
        TranslatePipe,
        ShoppingListBrowserComponent,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiMenuComponent,
        FdUiMenuDividerComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    templateUrl: './shopping-list-manage-controls.html',
    styleUrl: '../shopping-list-page/shopping-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListManageControlsComponent {
    private readonly dialogs = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly slots = signal<string[]>([]);
    private readonly recency = signal<string[]>([]);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    public readonly listSelectField = input.required<FieldTree<string | null>>();
    public readonly initialPage = input<ShoppingListPage | null>(null);
    public readonly lists = input.required<readonly ShoppingListSummary[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly canDeleteList = input.required<boolean>();
    public readonly isMobile = input(false);
    public readonly renameRequestedListId = input<string | null>(null);
    protected readonly selectedListId = computed(() => this.listSelectField()().value());
    protected readonly selectedList = computed(() => this.lists().find(list => list.id === this.selectedListId()));
    protected readonly quickLists = computed(() => {
        const available = this.lists();
        const ids = [...this.slots(), ...available.map(list => list.id)];
        return [...new Set(ids)]
            .map(id => available.find(list => list.id === id))
            .filter((list): list is ShoppingListSummary => list !== undefined)
            .slice(0, QUICK_LIST_LIMIT);
    });
    protected readonly listsCount = computed(() => this.lists().length);
    protected readonly editingListId = signal<string | null>(null);
    protected readonly renameDraft = signal('');

    public readonly mergeDuplicates = output();
    public readonly createList = output<string | void>();
    public readonly canClearPurchased = input(false);
    public readonly clearPurchased = output();
    public readonly clearListById = output<string>();
    public readonly deleteListById = output<string>();
    public readonly renameListById = output<{ listId: string; name: string }>();
    public readonly renameRequestHandled = output<string>();

    public constructor() {
        effect(() => {
            const id = this.selectedListId();
            const available = this.lists();
            if (id !== null && available.some(list => list.id === id)) {
                untracked(() => {
                    this.recordUse(id);
                });
            }
        });
        effect(() => {
            const requestedListId = this.renameRequestedListId();
            if (requestedListId === null || this.editingListId() === requestedListId) {
                return;
            }

            const list = this.lists().find(entry => entry.id === requestedListId);
            if (list === undefined) {
                return;
            }

            this.startRename(list);
            this.renameRequestHandled.emit(requestedListId);
        });
    }

    protected selectList(listId: string): void {
        if (this.isLoading() || listId === this.selectedListId()) {
            return;
        }

        this.recordUse(listId);
        this.listSelectField()().value.set(listId);
    }

    private recordUse(id: string): void {
        const slots = this.quickLists().map(list => list.id);
        if (!slots.includes(id)) {
            const oldest = [...this.recency()].reverse().find(entry => slots.includes(entry));
            const unused = [...slots].reverse().find(entry => !this.recency().includes(entry));
            const index = slots.indexOf(unused ?? oldest ?? '');
            slots.splice(Math.max(0, index), 1, id);
        }
        this.slots.set(slots);
        this.recency.update(ids => [id, ...ids.filter(entry => entry !== id)]);
    }

    protected openMobileLists(): void {
        if (!this.isMobile()) {
            return;
        }
        this.dialogs
            .open<ShoppingListBrowserDialogComponent, ShoppingListBrowserData, ShoppingListBrowserResult>(
                ShoppingListBrowserDialogComponent,
                {
                    preset: 'form',
                    autoFocus: 'dialog',
                    data: { lists: this.lists, initialPage: this.initialPage, selectedId: this.selectedListId() },
                },
            )
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result !== undefined) {
                    if ('create' in result) {
                        this.createList.emit(result.name);
                    } else {
                        this.selectList(result.id);
                    }
                }
            });
    }

    protected createAndRenameList(): void {
        this.createList.emit();
    }

    protected renameList(listId: string): void {
        const list = this.lists().find(entry => entry.id === listId);
        if (list === undefined) {
            return;
        }

        this.startRename(list);
    }

    protected canClearListCard(list: ShoppingListSummary): boolean {
        return list.itemsCount > 0 && !this.isLoading();
    }

    protected updateRenameDraft(event: Event): void {
        const target = event.target;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }

        this.renameDraft.set(target.value);
    }

    protected saveRename(listId: string): void {
        if (this.editingListId() !== listId) {
            return;
        }

        const name = this.renameDraft().trim();
        if (name.length === 0) {
            return;
        }

        this.renameListById.emit({ listId, name });
        this.editingListId.set(null);
    }

    protected cancelRename(): void {
        this.editingListId.set(null);
    }

    private startRename(list: ShoppingListSummary): void {
        this.editingListId.set(list.id);
        this.renameDraft.set(list.name);
        this.focusNameInput(RENAME_FOCUS_DELAY_MS);
    }

    private focusNameInput(delayMs: number): void {
        setTimeout(() => {
            setTimeout(() => {
                const nameInput = this.host.nativeElement.querySelector<HTMLInputElement>('.shopping-list__list-card-rename-input');
                nameInput?.focus();
                nameInput?.select();
            }, 0);
        }, delayMs);
    }
}
