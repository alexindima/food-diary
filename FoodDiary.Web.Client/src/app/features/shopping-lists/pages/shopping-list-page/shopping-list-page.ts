import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ViewportService } from '../../../../services/viewport.service';
import type { MeasurementUnit } from '../../../products/models/product.data';
import { ShoppingListFacade } from '../../lib/shopping-list.facade';
import type { ShoppingListItemFormGroup } from '../../lib/shopping-list-form.types';
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
    protected readonly listSelectControl = new FormControl<string | null>(null);
    protected readonly listNameControl = new FormControl<string>('', { nonNullable: true, validators: Validators.required });
    protected readonly itemForm: FormGroup<ShoppingListItemFormGroup>;

    private readonly isMobileManageOpen = signal(false);

    public constructor() {
        this.itemForm = new FormGroup<ShoppingListItemFormGroup>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            amount: new FormControl<number | null>(null),
            unit: new FormControl<MeasurementUnit | null>(null),
            category: new FormControl<string | null>(null),
        });

        this.listNameControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
            this.facade.setListName(value);
        });

        this.listSelectControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(id => {
            if (id !== null && id.length > 0) {
                this.facade.selectList(id);
            }
        });

        effect(() => {
            const selectedId = this.facade.selectedListId();
            if (this.listSelectControl.value !== selectedId) {
                this.listSelectControl.setValue(selectedId, { emitEvent: false });
            }
        });

        effect(() => {
            const name = this.facade.listName();
            if (this.listNameControl.value !== name) {
                this.listNameControl.setValue(name, { emitEvent: false });
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
        this.itemForm.markAllAsTouched();
        if (this.itemForm.invalid) {
            return;
        }

        const name = this.itemForm.controls.name.value.trim();
        if (name.length === 0) {
            return;
        }

        this.facade.addItem({
            name,
            amount: this.itemForm.controls.amount.value,
            unit: this.itemForm.controls.unit.value ?? null,
            category: this.itemForm.controls.category.value?.trim() ?? null,
        });

        this.itemForm.reset({
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
