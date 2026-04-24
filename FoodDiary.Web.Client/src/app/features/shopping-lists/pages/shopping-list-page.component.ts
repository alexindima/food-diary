import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FormGroupControls } from '../../../shared/lib/common.data';
import { ViewportService } from '../../../services/viewport.service';
import { MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListItem } from '../models/shopping-list.data';
import { ShoppingListFacade } from '../lib/shopping-list.facade';

@Component({
    selector: 'fd-shopping-list-page',
    templateUrl: './shopping-list-page.component.html',
    styleUrls: ['./shopping-list-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiCheckboxComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    providers: [ShoppingListFacade],
})
export class ShoppingListPageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly viewportService = inject(ViewportService);
    private readonly facade = inject(ShoppingListFacade);

    public readonly list = this.facade.list;
    public readonly items = this.facade.items;
    public readonly isLoading = this.facade.isLoading;
    public readonly isSaving = this.facade.isSaving;
    public readonly lists = this.facade.lists;
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly isMobileManageVisible = computed(() => this.isMobileManageOpen());
    public readonly canDeleteList = computed(() => this.lists().length > 1 && !!this.list() && !this.isSaving() && !this.isLoading());
    public readonly canClearList = computed(
        () => this.lists().length === 1 && this.items().length > 0 && !!this.list() && !this.isSaving() && !this.isLoading(),
    );
    public readonly listOptions = computed(() => this.facade.listOptions());
    public readonly isEmptyState = computed(() => this.items().length === 0);
    public readonly listSelectControl = new FormControl<string | null>(null);
    public readonly listNameControl = new FormControl<string>('', { nonNullable: true, validators: Validators.required });
    public readonly itemForm: FormGroup<ShoppingListItemFormGroup>;
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];

    private readonly unitValues = Object.values(MeasurementUnit) as MeasurementUnit[];
    private readonly isMobileManageOpen = signal(false);

    public constructor() {
        this.itemForm = new FormGroup<ShoppingListItemFormGroup>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            amount: new FormControl<number | null>(null),
            unit: new FormControl<MeasurementUnit | null>(null),
            category: new FormControl<string | null>(null),
        });

        this.buildUnitOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUnitOptions();
        });

        this.listNameControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => this.facade.setListName(value));

        this.listSelectControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(id => {
            if (id) {
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

    public addItem(): void {
        this.itemForm.markAllAsTouched();
        if (this.itemForm.invalid) {
            return;
        }

        const name = this.itemForm.controls.name.value.trim();
        if (!name) {
            return;
        }

        this.facade.addItem({
            name,
            amount: this.itemForm.controls.amount.value,
            unit: this.itemForm.controls.unit.value ?? null,
            category: this.itemForm.controls.category.value?.trim() || null,
        });

        this.itemForm.reset({
            name: '',
            amount: null,
            unit: null,
            category: null,
        });
    }

    public removeItem(itemId: string): void {
        this.facade.removeItem(itemId);
    }

    public toggleItemChecked(item: ShoppingListItem, checked: boolean): void {
        this.facade.toggleItemChecked(item.id, checked);
    }

    public formatItemMeta(item: ShoppingListItem): string {
        const parts: string[] = [];
        if (item.amount) {
            const unitLabel = this.getUnitLabel(item.unit);
            parts.push(unitLabel ? `${item.amount} ${unitLabel}` : `${item.amount}`);
        }
        if (item.category) {
            parts.push(item.category);
        }
        return parts.join(' - ');
    }

    public toggleMobileManage(): void {
        this.isMobileManageOpen.update(value => !value);
    }

    public deleteCurrentList(): void {
        const current = this.list();
        if (!current || !this.canDeleteList()) {
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
                if (confirmed) {
                    this.facade.deleteCurrentList();
                }
            });
    }

    public clearCurrentList(): void {
        const current = this.list();
        if (!current || !this.canClearList()) {
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
                if (confirmed) {
                    this.facade.clearCurrentList();
                }
            });
    }

    public createNewList(): void {
        this.facade.createNewList();
    }

    private buildUnitOptions(): void {
        this.unitOptions = this.unitValues.map(unit => ({
            value: unit,
            label: this.translateService.instant(`GENERAL.UNITS.${unit}`),
        }));
    }

    private getUnitLabel(unit?: MeasurementUnit | string | null): string | null {
        if (!unit) {
            return null;
        }

        if (typeof unit === 'string') {
            const key = `GENERAL.UNITS.${unit}`;
            const translated = this.translateService.instant(key);
            return translated === key ? unit : translated;
        }

        return this.translateService.instant(`GENERAL.UNITS.${unit}`);
    }
}

interface ShoppingListItemFormValues {
    name: string;
    amount: number | null;
    unit: MeasurementUnit | null;
    category: string | null;
}

type ShoppingListItemFormGroup = FormGroupControls<ShoppingListItemFormValues>;
