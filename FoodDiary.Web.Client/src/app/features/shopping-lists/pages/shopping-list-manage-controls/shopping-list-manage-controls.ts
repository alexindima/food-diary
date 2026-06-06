import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, input, output, signal } from '@angular/core';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiMenuComponent } from 'fd-ui-kit/menu/fd-ui-menu';
import { FdUiMenuDividerComponent } from 'fd-ui-kit/menu/fd-ui-menu-divider';
import { FdUiMenuItemComponent } from 'fd-ui-kit/menu/fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from 'fd-ui-kit/menu/fd-ui-menu-trigger.directive';

import type { ShoppingListSummary } from '../../models/shopping-list.data';

const RENAME_FOCUS_DELAY_MS = 0;

@Component({
    selector: 'fd-shopping-list-manage-controls',
    imports: [
        TranslatePipe,
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
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    public readonly listSelectField = input.required<FieldTree<string | null>>();
    public readonly lists = input.required<readonly ShoppingListSummary[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly canDeleteList = input.required<boolean>();
    public readonly isMobile = input(false);
    public readonly renameRequestedListId = input<string | null>(null);
    protected readonly selectedListId = computed(() => this.listSelectField()().value());
    protected readonly listsCount = computed(() => this.lists().length);
    protected readonly editingListId = signal<string | null>(null);
    protected readonly renameDraft = signal('');

    public readonly createList = output();
    public readonly clearListById = output<string>();
    public readonly deleteListById = output<string>();
    public readonly renameListById = output<{ listId: string; name: string }>();
    public readonly renameRequestHandled = output<string>();

    public constructor() {
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
        if (listId === this.selectedListId()) {
            return;
        }

        this.listSelectField()().value.set(listId);
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
