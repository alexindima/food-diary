import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import { skip } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { ShoppingListFacade } from '../../lib/shopping-list.facade';
import type { ShoppingListItemFormModel } from '../../lib/shopping-list-form.types';
import { ShoppingListItemsPanelComponent } from '../shopping-list-items-panel/shopping-list-items-panel';
import { ShoppingListManageControlsComponent } from '../shopping-list-manage-controls/shopping-list-manage-controls';

@Component({
    selector: 'fd-shopping-list-page',
    templateUrl: './shopping-list-page.html',
    styleUrls: ['./shopping-list-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
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

    protected readonly list = this.facade.list;
    protected readonly items = this.facade.items;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly lists = this.facade.lists;
    protected readonly isMobileView = this.viewportService.isMobile;
    protected readonly isMobileManageVisible = computed(() => this.isMobileManageOpen());
    protected readonly canDeleteList = computed(
        () => this.lists().length > 1 && this.list() !== null && !this.isSaving() && !this.isLoading(),
    );
    protected readonly canClearList = computed(
        () => this.lists().length === 1 && this.items().length > 0 && this.list() !== null && !this.isSaving() && !this.isLoading(),
    );
    protected readonly listSelectModel = signal<{ id: string | null }>({ id: null });
    protected readonly listNameModel = signal({ name: '' });
    protected readonly itemFormModel = signal<ShoppingListItemFormModel>({
        name: '',
        amount: null,
        unit: null,
        category: null,
    });
    protected readonly listSelectForm = form(this.listSelectModel);
    protected readonly listNameForm = form(this.listNameModel, path => {
        required(path.name);
    });
    protected readonly itemForm = form(this.itemFormModel, path => {
        required(path.name);
    });

    private readonly isMobileManageOpen = signal(false);

    public constructor() {
        toObservable(computed(() => this.listNameModel().name))
            .pipe(skip(1), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.facade.setListName(value);
            });

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
            const name = this.facade.listName();
            if (this.listNameModel().name !== name) {
                this.listNameModel.set({ name });
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
        });

        this.itemFormModel.set({
            name: '',
            amount: null,
            unit: null,
            category: null,
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

        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('CONFIRM_DELETE.TITLE', {
                type: this.translateService.instant('SHOPPING_LIST.ENTITY_NAME'),
            }),
            message: this.translateService.instant('CONFIRM_DELETE.MESSAGE', {
                name: current.name,
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
                    this.facade.deleteCurrentList();
                }
            });
    }

    protected clearCurrentList(): void {
        const current = this.list();
        if (current === null || !this.canClearList()) {
            return;
        }

        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('SHOPPING_LIST.CLEAR_CONFIRM_TITLE'),
            message: this.translateService.instant('SHOPPING_LIST.CLEAR_CONFIRM_MESSAGE', {
                name: current.name,
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
                    this.facade.clearCurrentList();
                }
            });
    }

    protected createNewList(): void {
        this.facade.createNewList();
    }
}
