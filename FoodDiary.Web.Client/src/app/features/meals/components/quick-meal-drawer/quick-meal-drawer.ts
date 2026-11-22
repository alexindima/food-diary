import { DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    Injector,
    input,
    signal,
    untracked,
    viewChildren,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { firstValueFrom } from 'rxjs';

import { MealDetailsFieldsComponent } from '../../../../components/shared/meal-details-fields/meal-details-fields';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { MealTypeOption } from '../../../../shared/lib/meal-type.util';
import { resolveProductImageUrl } from '../../../../shared/lib/product-image.util';
import { normalizeProductType } from '../../../../shared/lib/product-type.utils';
import { resolveRecipeImageUrl } from '../../../../shared/lib/recipe-image.util';
import { MealSourceType } from '../../../../shared/models/meal.data';
import { ProductType } from '../../../../shared/models/product.data';
import { recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { type QuickMealItem, QuickMealService } from '../../lib/quick/quick-meal.service';
import { recipeServingsFromInput } from '../../lib/recipe-serving/recipe-display-amount';
import type { MealItemFormValues } from '../manage/meal-manage-lib/meal-manage.types';
import { createMealItemValue } from '../manage/meal-manage-lib/meal-manage-form.mapper';
import { buildMealTypeSelectOptions } from '../manage/meal-manage-lib/meal-manage-options.mapper';
import {
    MealManualItemDialogComponent,
    type MealManualItemDialogData,
    type MealManualItemDialogResult,
} from '../manage/meal-manual-item-dialog/meal-manual-item-dialog';

type QuickMealItemView = {
    item: QuickMealItem;
    imageUrl: string;
    name: string;
    amount: number;
    unitKey: string;
    trackingKey: string;
};

type QuickMealToggleView = {
    icon: string;
    labelKey: string;
};

type QuickMealEditOperation = {
    item: QuickMealItem;
    dialogRef?: FdUiDialogRef<MealManualItemDialogComponent, MealManualItemDialogResult | null>;
};

@Component({
    selector: 'fd-quick-meal-drawer',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiSelectComponent, MealDetailsFieldsComponent],
    templateUrl: './quick-meal-drawer.html',
    styleUrls: ['./quick-meal-drawer.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickMealDrawerComponent {
    private static nextId = 0;

    private readonly quickService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly injector = inject(Injector);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly editActions = viewChildren<FdUiButtonComponent>('editAction');
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';
    private activeEdit: QuickMealEditOperation | undefined;

    public readonly forceShow = input(false);
    public readonly layout = input<'fixed' | 'inline'>('fixed');
    public readonly allowEditAndSave = input(true);
    protected readonly titleId = `fd-quick-meal-title-${QuickMealDrawerComponent.nextId++}`;
    protected readonly bodyId = `${this.titleId}-body`;

    protected readonly items = this.quickService.items;
    protected readonly language = injectCurrentLanguage();
    protected readonly mealTypeOptions = computed(() => {
        this.language();
        return buildMealTypeSelectOptions(this.translateService);
    });
    protected readonly itemsLabelKey = computed(
        () => `QUICK_MEAL.ITEMS_${new Intl.PluralRules(this.language()).select(this.items().length).toUpperCase()}`,
    );
    protected readonly details = this.quickService.details;
    protected readonly hasItems = this.quickService.hasItems;
    protected readonly isSaving = this.quickService.isSaving;
    protected readonly editingItemKey = signal<string | null>(null);
    protected readonly isDetailsExpanded = signal(false);
    protected readonly isCollapsed = signal(false);

    protected readonly shouldRender = computed(() => this.forceShow() || this.hasItems());
    protected readonly isInline = computed(() => this.layout() === 'inline');
    protected readonly collapsedToggleView = computed<QuickMealToggleView>(() =>
        this.isCollapsed()
            ? {
                  icon: 'expand_less',
                  labelKey: 'QUICK_MEAL.EXPAND',
              }
            : {
                  icon: 'expand_more',
                  labelKey: 'QUICK_MEAL.COLLAPSE',
              },
    );
    protected readonly detailsToggleView = computed<QuickMealToggleView>(() =>
        this.isDetailsExpanded()
            ? {
                  icon: 'expand_less',
                  labelKey: 'MEAL_DETAILS.HIDE',
              }
            : {
                  icon: 'expand_more',
                  labelKey: 'MEAL_DETAILS.ADD',
              },
    );
    protected readonly bodyInert = computed(() => (this.isCollapsed() ? '' : null));
    protected readonly itemViews = computed<QuickMealItemView[]>(() =>
        this.items().map(item => ({
            item,
            imageUrl: this.imageFor(item),
            name: this.itemName(item),
            amount: item.amount,
            unitKey: this.unitKeyFor(item),
            trackingKey: `${item.key}-${item.flashId ?? 0}`,
        })),
    );

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.cancelEdit();
        });
        let hadItems = this.hasItems();
        effect(() => {
            const items = this.items();
            const hasItems = items.length > 0;
            untracked(() => {
                if (!hasItems) {
                    this.resetUiState();
                } else if (!hadItems) {
                    this.isCollapsed.set(false);
                }

                const edit = this.activeEdit;
                if (edit !== undefined && !items.includes(edit.item)) {
                    this.cancelEdit();
                }
                hadItems = hasItems;
            });
        });
    }

    private imageFor(item: QuickMealItem): string {
        if (item.type === 'product') {
            const product = item.product;
            const type = normalizeProductType(product.productType) ?? ProductType.Unknown;
            return resolveProductImageUrl(product.imageUrl, type) ?? this.fallbackImage;
        }

        return resolveRecipeImageUrl(item.recipe.imageUrl) ?? this.fallbackImage;
    }

    private itemName(item: QuickMealItem): string {
        return item.type === 'product' ? item.product.name : item.recipe.name;
    }

    private unitKeyFor(item: QuickMealItem): string {
        if (item.type === 'product') {
            return `GENERAL.UNITS.${item.product.baseUnit}`;
        }

        const category = new Intl.PluralRules(this.language(), { maximumFractionDigits: 1 }).select(item.amount);
        return `QUICK_MEAL.SERVINGS_${category.toUpperCase()}`;
    }

    protected updateDate(value: string): void {
        this.quickService.updateDetails({ date: value });
    }

    protected updateTime(value: string): void {
        this.quickService.updateDetails({ time: value });
    }

    protected updateComment(value: string): void {
        this.quickService.updateDetails({ comment: value });
    }

    protected toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    protected toggleCollapsed(): void {
        this.isCollapsed.update(value => !value);
    }

    protected updatePreMealSatietyLevel(value: number | null): void {
        this.quickService.updateDetails({ preMealSatietyLevel: value });
    }

    protected updateMealType(mealType: MealTypeOption): void {
        this.quickService.updateDetails({ mealType });
    }

    protected updatePostMealSatietyLevel(value: number | null): void {
        this.quickService.updateDetails({ postMealSatietyLevel: value });
    }

    protected edit(item: QuickMealItem): void {
        if (this.activeEdit !== undefined || this.isSaving() || !this.items().includes(item)) {
            return;
        }
        const operation: QuickMealEditOperation = { item };
        this.activeEdit = operation;
        this.editingItemKey.set(item.key);
        void this.openEditDialogAsync(operation);
    }

    protected remove(key: string): void {
        if (this.activeEdit?.item.key === key) {
            this.cancelEdit();
        }
        this.quickService.removeItem(key);
    }

    protected clear(): void {
        this.cancelEdit();
        this.quickService.clear();
    }

    protected save(): void {
        if (this.activeEdit !== undefined) {
            return;
        }
        this.quickService.saveDraft();
    }

    private resetUiState(): void {
        this.cancelEdit();
        this.isCollapsed.set(false);
        this.isDetailsExpanded.set(false);
    }

    private async openEditDialogAsync(operation: QuickMealEditOperation): Promise<void> {
        let focusKey = operation.item.key;
        try {
            const dialogItem = await this.createDialogItemAsync(operation.item);
            if (!this.isCurrentEdit(operation)) {
                return;
            }
            operation.dialogRef = this.fdDialogService.open<
                MealManualItemDialogComponent,
                MealManualItemDialogData,
                MealManualItemDialogResult | null
            >(MealManualItemDialogComponent, { preset: 'form', data: { item: dialogItem } });
            const result = await firstValueFrom(operation.dialogRef.afterClosed());
            if (result === null || result === undefined || !this.isCurrentEdit(operation)) {
                return;
            }
            focusKey = this.quickService.updateItem(operation.item.key, this.toQuickMealItem(result));
        } finally {
            if (this.activeEdit === operation) {
                this.activeEdit = undefined;
                this.editingItemKey.set(null);
                if (operation.dialogRef !== undefined) {
                    this.restoreEditFocusAfterRender(focusKey);
                }
            }
        }
    }

    private restoreEditFocusAfterRender(key: string): void {
        afterNextRender(
            () => {
                if (this.activeEdit !== undefined || this.document.activeElement !== this.document.body) {
                    return;
                }

                const index = this.items().findIndex(item => item.key === key);
                this.editActions()[index]?.focus();
            },
            { injector: this.injector },
        );
    }

    private isCurrentEdit(operation: QuickMealEditOperation): boolean {
        return this.activeEdit === operation && !this.destroyRef.destroyed && !this.isSaving() && this.items().includes(operation.item);
    }

    private cancelEdit(): void {
        const operation = this.activeEdit;
        this.activeEdit = undefined;
        this.editingItemKey.set(null);
        operation?.dialogRef?.close(null);
    }

    private async createDialogItemAsync(item: QuickMealItem): Promise<MealItemFormValues> {
        if (item.type === 'product') {
            return createMealItemValue(item.product, null, item.amount);
        }

        return this.mealManageFacade.prepareRecipeItemAsync(item.recipe, recipeServingsFromStored(item.amount));
    }

    private toQuickMealItem(item: MealManualItemDialogResult): QuickMealItem {
        if (item.sourceType === MealSourceType.Product) {
            return {
                key: `product-${item.product.id}`,
                type: 'product',
                product: item.product,
                amount: item.amount,
            };
        }

        return {
            key: `recipe-${item.recipe.id}`,
            type: 'recipe',
            recipe: item.recipe,
            amount: recipeServingsFromInput(item.amount, item.recipeMass),
        };
    }
}
