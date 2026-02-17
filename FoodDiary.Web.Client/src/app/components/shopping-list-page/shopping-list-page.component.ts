import { BreakpointObserver } from '@angular/cdk/layout';
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
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { ConfirmDeleteDialogComponent, ConfirmDeleteDialogData } from '../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { MeasurementUnit } from '../../types/product.data';
import { ShoppingList, ShoppingListItem, ShoppingListItemDto, ShoppingListSummary } from '../../types/shopping-list.data';
import { ShoppingListService } from '../../services/shopping-list.service';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroupControls } from '../../types/common.data';
import { Subject, debounceTime, distinctUntilChanged, map } from 'rxjs';

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
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly saveQueue = new Subject<void>();

    public readonly list = signal<ShoppingList | null>(null);
    public readonly items = signal<ShoppingListItem[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    public readonly lists = signal<ShoppingListSummary[]>([]);
    public readonly listSelectControl = new FormControl<string | null>(null);
    public readonly listNameControl = new FormControl<string>('', { nonNullable: true, validators: Validators.required });
    public readonly itemForm: FormGroup<ShoppingListItemFormGroup>;
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];
    public listOptions: FdUiSelectOption<string>[] = [];

    private readonly unitValues = Object.values(MeasurementUnit) as MeasurementUnit[];
    private lastLoadedListId: string | null = null;
    private suppressAutosave = false;
    private pendingSave = false;
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

        this.listNameControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.scheduleSave());

        this.saveQueue
            .pipe(
                debounceTime(500),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => this.persistList());
    }

    public ngOnInit(): void {
        this.breakpointObserver
            .observe('(max-width: 768px)')
            .pipe(
                map(result => result.matches),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(isMobile => {
                this.isMobileView.set(isMobile);
                if (!isMobile) {
                    this.isMobileManageOpen.set(false);
                }
            });

        this.loadLists();

        this.listSelectControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(id => {
                if (!id || id === this.lastLoadedListId) {
                    return;
                }
                this.loadListById(id);
            });
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
        this.scheduleSave();
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
        this.scheduleSave();
    }

    public toggleItemChecked(item: ShoppingListItem, checked: boolean): void {
        const nextItems = this.items().map(entry =>
            entry.id === item.id ? { ...entry, isChecked: checked } : entry,
        );
        this.items.set(nextItems);
        this.scheduleSave();
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

    public toggleMobileManage(): void {
        this.isMobileManageOpen.update(value => !value);
    }

    public get isMobileManageVisible(): boolean {
        return this.isMobileManageOpen();
    }
    public get canDeleteList(): boolean {
        return this.lists().length > 1 && !!this.list() && !this.isSaving() && !this.isLoading();
    }

    public get canClearList(): boolean {
        return this.lists().length === 1
            && (this.items().length > 0)
            && !!this.list()
            && !this.isSaving()
            && !this.isLoading();
    }

    public deleteCurrentList(): void {
        const current = this.list();
        if (!current || !this.canDeleteList) {
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
                size: 'sm',
                data,
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (!confirmed) {
                    return;
                }
                this.performDelete(current.id);
            });
    }

    public clearCurrentList(): void {
        const current = this.list();
        if (!current || !this.canClearList) {
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
                size: 'sm',
                data,
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (!confirmed) {
                    return;
                }
                this.clearListItems(current.id, current.name);
            });
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
                    this.buildListOptions();
                    const currentSelection = this.listSelectControl.value;
                    const selectedId = currentSelection && lists.some(list => list.id === currentSelection)
                        ? currentSelection
                        : lists[0].id;
                    this.listSelectControl.setValue(selectedId, { emitEvent: false });
                    this.loadListById(selectedId);
                },
                error: (error: HttpErrorResponse) => {
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
                    this.loadLists();
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
        this.suppressAutosave = true;
        this.list.set(list);
        this.items.set(this.rebuildSortOrder(list.items ?? []));
        this.listNameControl.setValue(list.name, { emitEvent: false });
        this.lastLoadedListId = list.id;
        this.listSelectControl.setValue(list.id, { emitEvent: false });
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

    private loadListById(id: string): void {
        this.isLoading.set(true);
        this.shoppingListService
            .getById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isLoading.set(false);
                    this.applyList(list);
                },
                error: (error: HttpErrorResponse) => {
                    this.isLoading.set(false);
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.LOAD_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Load shopping list error', error);
                },
            });
    }

    private buildListOptions(): void {
        this.listOptions = this.lists().map(list => ({
            value: list.id,
            label: `${list.name} (${list.itemsCount})`,
        }));
    }

    private updateListSummary(list: ShoppingList): void {
        const next = this.lists().map(entry =>
            entry.id === list.id
                ? { ...entry, name: list.name, itemsCount: list.items.length }
                : entry,
        );
        this.lists.set(next);
        this.buildListOptions();
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

        this.saveQueue.next();
    }

    private persistList(): void {
        const current = this.list();
        if (!current || this.isSaving() || this.isLoading()) {
            return;
        }

        const name = this.listNameControl.value.trim();
        if (!name) {
            this.listNameControl.markAsTouched();
            return;
        }

        this.isSaving.set(true);
        const payloadItems = this.items().map((item, index) => this.mapItemToDto(item, index));
        const payload = { name, items: payloadItems };

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
                        this.scheduleSave();
                    }
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

    private clearListItems(id: string, name: string): void {
        this.isSaving.set(true);
        this.shoppingListService
            .update(id, { name, items: [] })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: list => {
                    this.isSaving.set(false);
                    this.applyList(list);
                    this.updateListSummary(list);
                },
                error: (error: HttpErrorResponse) => {
                    this.isSaving.set(false);
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.CLEAR_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Clear shopping list error', error);
                },
            });
    }

    private performDelete(id: string): void {
        this.suppressAutosave = true;
        this.pendingSave = false;
        this.list.set(null);
        this.items.set([]);
        this.lastLoadedListId = null;
        this.listSelectControl.setValue(null, { emitEvent: false });
        this.isSaving.set(true);
        this.shoppingListService
            .deleteById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.suppressAutosave = false;
                    this.loadLists();
                },
                error: (error: HttpErrorResponse) => {
                    this.isSaving.set(false);
                    this.suppressAutosave = false;
                    this.toastService.open(
                        this.translateService.instant('SHOPPING_LIST.DELETE_ERROR'),
                        { appearance: 'negative' },
                    );
                    console.error('Delete shopping list error', error);
                },
            });
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

