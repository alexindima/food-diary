import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { MeasurementUnit } from '../../types/product.data';
import { ShoppingList, ShoppingListItem, ShoppingListItemDto } from '../../types/shopping-list.data';
import { ShoppingListService } from '../../services/shopping-list.service';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroupControls } from '../../types/common.data';

@Component({
    selector: 'fd-shopping-list-page',
    templateUrl: './shopping-list-page.component.html',
    styleUrls: ['./shopping-list-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        MatIconModule,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiCheckboxComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
})
export class ShoppingListPageComponent implements OnInit {
    private readonly shoppingListService = inject(ShoppingListService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly list = signal<ShoppingList | null>(null);
    public readonly items = signal<ShoppingListItem[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly listNameControl = new FormControl<string>('', { nonNullable: true, validators: Validators.required });
    public readonly itemForm: FormGroup<ShoppingListItemFormGroup>;
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];

    private readonly unitValues = Object.values(MeasurementUnit) as MeasurementUnit[];

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
    }

    public ngOnInit(): void {
        this.loadCurrentList();
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

        const amount = this.normalizeAmount(this.itemForm.controls.amount.value);
        const unit = this.itemForm.controls.unit.value ?? null;
        const category = this.itemForm.controls.category.value?.trim() || null;
        const nextItems = [...this.items(), {
            id: this.createTempId(),
            shoppingListId: this.list()?.id ?? '',
            name,
            amount,
            unit,
            category,
            productId: null,
            isChecked: false,
            sortOrder: this.items().length + 1,
        }];

        this.items.set(nextItems);
        this.itemForm.reset({
            name: '',
            amount: null,
            unit: null,
            category: null,
        });
    }

    public removeItem(itemId: string): void {
        const filtered = this.items().filter(item => item.id !== itemId);
        this.items.set(this.rebuildSortOrder(filtered));
    }

    public toggleItemChecked(item: ShoppingListItem, checked: boolean): void {
        const nextItems = this.items().map(entry =>
            entry.id === item.id ? { ...entry, isChecked: checked } : entry,
        );
        this.items.set(nextItems);
    }

    public saveList(): void {
        if (this.listNameControl.invalid || this.isSaving()) {
            this.listNameControl.markAsTouched();
            return;
        }

        const current = this.list();
        if (!current) {
            return;
        }

        this.isSaving.set(true);
        const payloadItems = this.items().map((item, index) => this.mapItemToDto(item, index));
        const payload = {
            name: this.listNameControl.value.trim(),
            items: payloadItems,
        };

        this.shoppingListService
            .update(current.id, payload)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    this.applyList(list);
                },
                error: (error: HttpErrorResponse) => {
                    this.isSaving.set(false);
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.SAVE_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Update shopping list error', error);
                },
            });
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
        return parts.join(' • ');
    }

    private loadCurrentList(): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isLoading.set(false);
                    this.applyList(list);
                },
                error: (error: HttpErrorResponse) => {
                    if (error.status === 404) {
                        this.createDefaultList();
                        return;
                    }
                    this.isLoading.set(false);
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Load shopping list error', error);
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
                },
                error: (error: HttpErrorResponse) => {
                    this.isLoading.set(false);
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.CREATE_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Create shopping list error', error);
                },
            });
    }

    private applyList(list: ShoppingList): void {
        this.list.set(list);
        this.items.set(this.rebuildSortOrder(list.items ?? []));
        this.listNameControl.setValue(list.name, { emitEvent: false });
    }

    private rebuildSortOrder(items: ShoppingListItem[]): ShoppingListItem[] {
        return items.map((item, index) => ({
            ...item,
            sortOrder: index + 1,
        }));
    }

    private normalizeAmount(value: number | null): number | null {
        if (value === null || value === undefined) {
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

    private createTempId(): string {
        return `temp-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
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
