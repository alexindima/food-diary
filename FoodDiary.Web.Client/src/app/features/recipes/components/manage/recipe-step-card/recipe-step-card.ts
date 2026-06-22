import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { IngredientFormValues, StepFormValues } from '../recipe-manage-lib/recipe-manage.types';

export type RecipeStepCardField<T> = {
    error: string | null;
    value: T;
};

export type RecipeStepCardState = {
    description: RecipeStepCardField<StepFormValues['description']>;
    imageUrl: RecipeStepCardField<StepFormValues['imageUrl']>;
    ingredients: readonly RecipeStepIngredientState[];
    title: RecipeStepCardField<StepFormValues['title']>;
};

export type RecipeStepIngredientState = {
    amount: RecipeStepCardField<IngredientFormValues['amount']>;
    food: IngredientFormValues['food'];
    foodName: RecipeStepCardField<IngredientFormValues['foodName']>;
    nestedRecipeId: IngredientFormValues['nestedRecipeId'];
};

@Component({
    selector: 'fd-recipe-step-card',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSegmentedToggleComponent,
        FdUiTextareaComponent,
        ImageUploadFieldComponent,
        CdkDragHandle,
    ],
    templateUrl: './recipe-step-card.html',
    styleUrls: ['./recipe-step-card.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeStepCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly currentLanguage = signal(this.getCurrentLanguage());

    public readonly step = input.required<RecipeStepCardState>();
    public readonly stepIndex = input.required<number>();
    public readonly isExpanded = input.required<boolean>();
    public readonly dragDisabled = input.required<boolean>();

    public readonly removeStep = output();
    public readonly toggleExpanded = output();
    public readonly addIngredient = output();
    public readonly removeIngredient = output<number>();
    public readonly selectProduct = output<RecipeIngredientSelectEvent>();
    public readonly stepTitleChange = output<string | null>();
    public readonly stepImageChange = output<ImageSelection | null>();
    public readonly stepDescriptionChange = output<string>();
    public readonly ingredientAmountChange = output<{ ingredientIndex: number; amount: number | null }>();

    protected readonly isStepTitleEditing = signal(false);
    protected readonly ingredientTypeOptions: FdUiSegmentedToggleOption[] = [
        { value: 'Product', label: this.translateService.instant('CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Product') },
        { value: 'Recipe', label: this.translateService.instant('CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.Recipe') },
    ];
    protected readonly ingredientsCount = computed(() => this.ingredients.length);
    protected readonly descriptionSummary = computed(() => {
        this.currentLanguage();
        const description = this.step().description.value.trim();
        if (description.length === 0) {
            return this.translateService.instant('RECIPE_MANAGE.STEP_NO_DESCRIPTION');
        }
        return description;
    });
    protected readonly titleDisplay = computed(() => {
        this.currentLanguage();
        const titleValue = this.step().title.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        if (trimmedTitle.length > 0) {
            return trimmedTitle;
        }
        return this.translateService.instant('RECIPE_MANAGE.STEP_TITLE', { index: this.stepIndex() + 1 });
    });
    protected readonly descriptionError = computed(() => {
        this.currentLanguage();
        return this.step().description.error;
    });
    protected readonly expandedIcon = computed(() => (this.isExpanded() ? 'expand_less' : 'expand_more'));
    protected readonly isFirst = computed(() => this.stepIndex() === 0);
    protected readonly ingredientRows = computed<RecipeIngredientRowView[]>(() => {
        this.currentLanguage();

        return this.ingredients.map((ingredient, index) => {
            const food = ingredient.food;
            const nestedRecipeId = ingredient.nestedRecipeId;
            const unitKey = food?.baseUnit !== undefined ? `PRODUCT_AMOUNT_UNITS.${food.baseUnit}` : null;

            return {
                index,
                prefixIcon: nestedRecipeId !== null && nestedRecipeId.length > 0 ? 'menu_book' : food !== null ? 'restaurant' : 'search',
                amountLabel: this.resolveIngredientAmountLabel(nestedRecipeId !== null && nestedRecipeId.length > 0, unitKey),
                itemType: nestedRecipeId !== null && nestedRecipeId.length > 0 ? 'Recipe' : 'Product',
                foodNameError: ingredient.foodName.error,
                amountError: ingredient.amount.error,
            };
        });
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
            this.currentLanguage.set(this.getCurrentLanguage());
        });
    }

    protected get ingredients(): readonly RecipeStepIngredientState[] {
        return this.step().ingredients;
    }

    protected toggleStepTitleEdit(): void {
        if (this.isStepTitleEditing()) {
            this.commitStepTitle();
            this.isStepTitleEditing.set(false);
            return;
        }
        this.isStepTitleEditing.set(true);
    }

    protected onStepTitleBlur(): void {
        this.commitStepTitle();
        this.isStepTitleEditing.set(false);
    }

    protected onProductSelectClick(ingredientIndex: number, itemType: RecipeIngredientItemType): void {
        this.selectProduct.emit({ ingredientIndex, itemType });
    }

    protected onIngredientTypeChange(ingredientIndex: number, itemType: string): void {
        this.selectProduct.emit({ ingredientIndex, itemType: itemType === 'Recipe' ? 'Recipe' : 'Product' });
    }

    protected onRemoveIngredient(ingredientIndex: number): void {
        this.removeIngredient.emit(ingredientIndex);
    }

    protected onAddIngredient(): void {
        this.addIngredient.emit();
    }

    protected onStepTitleInput(value: FdUiInputValue): void {
        this.stepTitleChange.emit(value === null || value === undefined ? null : fdUiCoerceInputTextValue(value));
    }

    protected onStepImageChange(value: ImageSelection | null | undefined): void {
        this.stepImageChange.emit(value ?? null);
    }

    protected onStepDescriptionInput(value: FdUiInputValue): void {
        this.stepDescriptionChange.emit(fdUiCoerceInputTextValue(value));
    }

    protected onIngredientAmountInput(ingredientIndex: number, value: FdUiInputValue): void {
        if (ingredientIndex < 0 || ingredientIndex >= this.ingredients.length) {
            return;
        }

        const trimmedValue = fdUiCoerceInputTextValue(value).trim();
        const parsedValue = trimmedValue.length === 0 ? null : Number(trimmedValue);
        this.ingredientAmountChange.emit({
            ingredientIndex,
            amount: parsedValue !== null && Number.isFinite(parsedValue) ? parsedValue : null,
        });
    }

    protected onRemoveStep(): void {
        this.removeStep.emit();
    }

    protected onToggleExpanded(): void {
        this.toggleExpanded.emit();
    }

    private commitStepTitle(): void {
        const titleValue = this.step().title.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        this.stepTitleChange.emit(trimmedTitle.length > 0 ? trimmedTitle : null);
    }

    private resolveIngredientAmountLabel(isNestedRecipe: boolean, unitKey: string | null): string {
        if (isNestedRecipe) {
            return this.translateService.instant('RECIPE_SELECT_DIALOG.SERVINGS');
        }

        const amountLabel = this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT');
        if (unitKey === null || unitKey.length === 0) {
            return amountLabel;
        }

        return `${amountLabel} (${this.translateService.instant(unitKey)})`;
    }

    private getCurrentLanguage(): string {
        const currentLang = this.normalizeLanguage(this.translateService.getCurrentLang());
        if (currentLang.length > 0) {
            return currentLang;
        }

        const fallbackLang = this.normalizeLanguage(this.translateService.getFallbackLang());
        return fallbackLang.length > 0 ? fallbackLang : 'en';
    }

    private normalizeLanguage(value: unknown): string {
        return typeof value === 'string' ? value : '';
    }
}

type RecipeIngredientRowView = {
    index: number;
    prefixIcon: 'menu_book' | 'restaurant' | 'search';
    amountLabel: string;
    itemType: RecipeIngredientItemType;
    foodNameError: string | null;
    amountError: string | null;
};

export type RecipeIngredientItemType = 'Product' | 'Recipe';

export type RecipeIngredientSelectEvent = {
    ingredientIndex: number;
    itemType: RecipeIngredientItemType;
};
