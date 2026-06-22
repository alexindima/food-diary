import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import { skip } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { ShoppingListFacade } from '../../lib/shopping-list.facade';
import type { ShoppingListItemFormModel } from '../../lib/shopping-list-form.types';
import { ShoppingListItemsPanelComponent } from '../shopping-list-items-panel/shopping-list-items-panel';
import { ShoppingListManageControlsComponent } from '../shopping-list-manage-controls/shopping-list-manage-controls';
import { SHOPPING_LIST_TOUR } from './shopping-list-tour';

@Component({
    selector: 'fd-shopping-list-page',
    templateUrl: './shopping-list-page.html',
    styleUrls: ['./shopping-list-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ShoppingListItemsPanelComponent,
        ShoppingListManageControlsComponent,
    ],
    providers: [ShoppingListFacade],
})
export class ShoppingListPageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly viewportService = inject(ViewportService);
    private readonly facade = inject(ShoppingListFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly list = this.facade.list;
    protected readonly items = this.facade.items;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly lists = this.facade.lists;
    protected readonly renameRequestedListId = this.facade.renameRequestedListId;
    protected readonly isMobileView = this.viewportService.isMobile;
    protected readonly isMobileManageVisible = computed(() => this.isMobileManageOpen());
    protected readonly checkedItemsCount = computed(() => this.items().filter(item => item.isChecked).length);
    protected readonly totalItemsCount = computed(() => this.items().length);
    protected readonly remainingItemsCount = computed(() => this.totalItemsCount() - this.checkedItemsCount());
    protected readonly hasLists = computed(() => this.lists().length > 0);
    protected readonly canDeleteList = computed(() => this.list() !== null && !this.isSaving() && !this.isLoading());
    protected readonly canClearList = computed(
        () => this.items().length > 0 && this.list() !== null && !this.isSaving() && !this.isLoading(),
    );
    protected readonly listSelectModel = signal<{ id: string | null }>({ id: null });
    protected readonly itemFormModel = signal<ShoppingListItemFormModel>({
        name: '',
        amount: null,
        unit: null,
        category: null,
        note: null,
    });
    protected readonly listSelectForm = form(this.listSelectModel);
    protected readonly itemForm = form(this.itemFormModel, path => {
        required(path.name);
    });

    private readonly isMobileManageOpen = signal(false);

    public constructor() {
        toObservable(computed(() => this.listSelectModel().id))
            .pipe(skip(1), takeUntilDestroyed(this.destroyRef))
            .subscribe(id => {
                if (id !== null && id.length > 0) {
                    this.facade.selectList(id);
                }
            });

        effect(() => {
            const selectedId = this.facade.selectedListId();
            if (this.listSelectModel().id !== selectedId) {
                this.listSelectModel.set({ id: selectedId });
            }
        });

        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileManageOpen.set(false);
            }
        });

        this.facade.initialize();
    }

    protected addItem(): void {
        if (this.list() === null) {
            return;
        }

        this.itemForm().markAsTouched();
        if (this.itemForm().invalid()) {
            return;
        }

        const value = this.itemFormModel();
        const name = value.name.trim();
        if (name.length === 0) {
            return;
        }

        this.facade.addItem({
            name,
            amount: value.amount,
            unit: value.unit ?? null,
            category: value.category?.trim() ?? null,
            note: value.note?.trim() ?? null,
        });

        this.itemFormModel.set({
            name: '',
            amount: null,
            unit: null,
            category: null,
            note: null,
        });
    }

    protected removeItem(itemId: string): void {
        this.facade.removeItem(itemId);
    }

    protected toggleItemChecked(itemId: string, checked: boolean): void {
        this.facade.toggleItemChecked(itemId, checked);
    }

    protected toggleMobileManage(): void {
        this.isMobileManageOpen.update(value => !value);
    }

    protected deleteCurrentList(): void {
        const current = this.list();
        if (current === null || !this.canDeleteList()) {
            return;
        }

        this.confirmDeleteList(current.id, current.name);
    }

    protected deleteListById(listId: string): void {
        if (!this.canDeleteList()) {
            return;
        }

        const list = this.lists().find(entry => entry.id === listId);
        if (list === undefined) {
            return;
        }

        this.confirmDeleteList(list.id, list.name);
    }

    protected renameListById(listId: string, name: string): void {
        this.facade.renameListById(listId, name);
    }

    protected clearRenameRequest(listId: string): void {
        this.facade.clearRenameRequest(listId);
    }

    private confirmDeleteList(listId: string, listName: string): void {
        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('CONFIRM_DELETE.TITLE', {
                type: this.translateService.instant('SHOPPING_LIST.ENTITY_NAME'),
            }),
            message: this.translateService.instant('CONFIRM_DELETE.MESSAGE', {
                name: listName,
            }),
        };

        this.dialogService
            .open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
                preset: 'confirm',
                data,
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed === true) {
                    this.facade.deleteListById(listId);
                }
            });
    }

    protected clearCurrentList(): void {
        const current = this.list();
        if (current === null || !this.canClearList()) {
            return;
        }

        this.confirmClearList(current.id, current.name);
    }

    protected clearListById(listId: string): void {
        const list = this.lists().find(entry => entry.id === listId);
        if (list === undefined || list.itemsCount === 0 || this.isSaving() || this.isLoading()) {
            return;
        }

        this.confirmClearList(list.id, list.name);
    }

    private confirmClearList(listId: string, listName: string): void {
        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('SHOPPING_LIST.CLEAR_CONFIRM_TITLE'),
            message: this.translateService.instant('SHOPPING_LIST.CLEAR_CONFIRM_MESSAGE', {
                name: listName,
            }),
            confirmLabel: this.translateService.instant('SHOPPING_LIST.CLEAR_LIST_BUTTON'),
            cancelLabel: this.translateService.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.dialogService
            .open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
                preset: 'confirm',
                data,
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed === true) {
                    this.facade.clearListById(listId);
                }
            });
    }

    protected createNewList(): void {
        this.facade.createNewList();
    }

    protected startShoppingListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(SHOPPING_LIST_TOUR), { force });
    }
}
