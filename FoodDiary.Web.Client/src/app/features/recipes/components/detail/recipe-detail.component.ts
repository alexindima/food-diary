import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { of, switchMap } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { CHART_COLORS } from '../../../../constants/chart-colors';
import { NUTRIENT_ROUNDING_FACTOR, PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { normalizeQualityScore } from '../../../../shared/lib/quality-score.utils';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import type { Recipe } from '../../models/recipe.data';
import { type IngredientPreviewItem, type MacroBlock, RecipeDetailActionResult } from './recipe-detail.types';
import { RecipeDetailSummaryComponent } from './recipe-detail-summary.component';

const MACRO_SUMMARY_LIMIT = 3;
const MIN_MACRO_BAR_PERCENT = 4;
const INGREDIENT_PREVIEW_LIMIT = 6;

@Component({
    selector: 'fd-recipe-detail',
    standalone: true,
    templateUrl: './recipe-detail.component.html',
    styleUrls: ['./recipe-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        NutritionEditorComponent,
        RecipeDetailSummaryComponent,
    ],
})
export class RecipeDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<RecipeDetailComponent, RecipeDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeService = inject(RecipeService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly translateService = inject(TranslateService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'RECIPE_DETAIL.REMOVE_FAVORITE' : 'RECIPE_DETAIL.ADD_FAVORITE',
    );
    private initialFavoriteState = false;
    private favoriteRecipeId: string | null = null;

    public readonly recipe: Recipe;
    public readonly calories: number;
    public readonly proteins: number;
    public readonly fats: number;
    public readonly carbs: number;
    public readonly fiber: number;
    public readonly alcohol: number;
    public readonly qualityScore: number;
    public readonly qualityGrade: string;
    public readonly qualityHintKey: string;
    public readonly macroBlocks: MacroBlock[];
    public readonly macroSummaryBlocks = computed(() => this.macroBlocks.slice(0, MACRO_SUMMARY_LIMIT));
    public readonly ingredientPreview: IngredientPreviewItem[];
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'calories',
        proteins: 'proteins',
        fats: 'fats',
        carbs: 'carbs',
        fiber: 'fiber',
        alcohol: 'alcohol',
    };
    public readonly nutritionForm: FormGroup;
    public readonly macroBarState: NutritionMacroState;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'RECIPE_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'RECIPE_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly totalTime: number | null;
    public readonly ingredientCount: number;
    public readonly visibilityKey: string;
    public readonly isDeleteDisabled: boolean;
    public readonly isEditDisabled: boolean;
    public readonly canModify: boolean;
    public readonly warningMessage: string | null;

    public isDuplicateInProgress = false;

    public constructor() {
        const data = inject<Recipe>(FD_UI_DIALOG_DATA);

        this.recipe = data;
        this.initializeFavoriteState();
        this.calories = this.resolveNutrientValue(data.totalCalories, data.manualCalories);
        this.proteins = this.resolveNutrientValue(data.totalProteins, data.manualProteins);
        this.fats = this.resolveNutrientValue(data.totalFats, data.manualFats);
        this.carbs = this.resolveNutrientValue(data.totalCarbs, data.manualCarbs);
        this.fiber = this.resolveFiberValue();
        this.alcohol = this.resolveAlcoholValue();
        this.qualityScore = normalizeQualityScore(data.qualityScore);
        this.qualityGrade = data.qualityGrade ?? 'yellow';
        this.qualityHintKey = `QUALITY.${this.qualityGrade.toUpperCase()}`;
        this.totalTime = this.calculateTotalPreparationTime();
        this.ingredientCount = this.computeIngredientCount();
        this.ingredientPreview = this.buildIngredientPreview();
        this.visibilityKey = `RECIPE_VISIBILITY.${this.recipe.visibility}`;
        this.isDeleteDisabled = !this.recipe.isOwnedByCurrentUser || this.recipe.usageCount > 0;
        this.isEditDisabled = !this.recipe.isOwnedByCurrentUser || this.recipe.usageCount > 0;
        this.canModify = !this.isEditDisabled;
        this.warningMessage = this.resolveWarningMessage();
        const datasetValues = [this.proteins, this.fats, this.carbs];
        this.nutritionForm = this.buildNutritionForm({
            calories: this.calories,
            proteins: this.proteins,
            fats: this.fats,
            carbs: this.carbs,
            fiber: this.fiber,
            alcohol: this.alcohol,
        });
        this.macroBarState = this.buildMacroBarState(datasetValues);
        this.macroBlocks = this.buildMacroBlocks(datasetValues);
        this.favoriteRecipeService.isFavorite(this.recipe.id).subscribe(isFav => {
            this.initialFavoriteState = isFav;
            this.isFavorite.set(isFav);
        });
    }

    private initializeFavoriteState(): void {
        this.initialFavoriteState = this.recipe.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteRecipeId = this.recipe.favoriteRecipeId ?? null;
    }

    private resolveWarningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.recipe.isOwnedByCurrentUser ? 'RECIPE_DETAIL.WARNING_IN_USE' : 'RECIPE_DETAIL.WARNING_NOT_OWNER';
    }

    private buildMacroBlocks(datasetValues: number[]): MacroBlock[] {
        return [
            {
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: this.proteins,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.proteins,
                percent: this.resolveMacroPercent(this.proteins, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: this.fats,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fats,
                percent: this.resolveMacroPercent(this.fats, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: this.carbs,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.carbs,
                percent: this.resolveMacroPercent(this.carbs, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FIBER',
                value: this.fiber,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fiber,
                percent: this.resolveMacroPercent(this.fiber, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
                value: this.alcohol,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.alcohol,
                percent: this.resolveMacroPercent(this.alcohol, datasetValues),
            },
        ];
    }

    private buildNutritionForm(values: {
        calories: number;
        proteins: number;
        fats: number;
        carbs: number;
        fiber: number;
        alcohol: number;
    }): FormGroup {
        return new FormGroup({
            calories: new FormControl(values.calories),
            proteins: new FormControl(values.proteins),
            fats: new FormControl(values.fats),
            carbs: new FormControl(values.carbs),
            fiber: new FormControl(values.fiber),
            alcohol: new FormControl(values.alcohol),
        });
    }

    private buildMacroBarState(values: number[]): NutritionMacroState {
        const total = values.reduce((sum, value) => sum + value, 0);

        return {
            isEmpty: total <= 0,
            segments: [
                { key: 'proteins', percent: total > 0 ? (values[0] / total) * PERCENT_MULTIPLIER : 0 },
                { key: 'fats', percent: total > 0 ? (values[1] / total) * PERCENT_MULTIPLIER : 0 },
                { key: 'carbs', percent: total > 0 ? (values[2] / total) * PERCENT_MULTIPLIER : 0 },
            ],
        };
    }

    private resolveMacroPercent(value: number, values: number[]): number {
        const max = Math.max(...values, value, 1);
        return Math.max(MIN_MACRO_BAR_PERCENT, Math.round((value / max) * PERCENT_MULTIPLIER));
    }

    private buildIngredientPreview(): IngredientPreviewItem[] {
        return this.recipe.steps
            .flatMap(step => step.ingredients)
            .slice(0, INGREDIENT_PREVIEW_LIMIT)
            .map(ingredient => ({
                name:
                    ingredient.productName ??
                    ingredient.nestedRecipeName ??
                    this.translateService.instant('RECIPE_DETAIL.UNKNOWN_INGREDIENT'),
                amount: ingredient.amount,
                unitKey:
                    ingredient.productBaseUnit !== null && ingredient.productBaseUnit !== undefined && ingredient.productBaseUnit.length > 0
                        ? `GENERAL.UNITS.${ingredient.productBaseUnit}`
                        : null,
            }));
    }

    public close(): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new RecipeDetailActionResult(this.recipe.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
    }

    private resolveFiberValue(): number {
        if (this.recipe.totalFiber !== null && this.recipe.totalFiber !== undefined) {
            return this.recipe.totalFiber;
        }

        if (this.recipe.manualFiber !== null && this.recipe.manualFiber !== undefined) {
            return this.recipe.manualFiber;
        }

        const computedFiber = this.computeFiberFromSteps();
        return computedFiber ?? 0;
    }

    private resolveAlcoholValue(): number {
        if (this.recipe.totalAlcohol !== null && this.recipe.totalAlcohol !== undefined) {
            return this.recipe.totalAlcohol;
        }

        if (this.recipe.manualAlcohol !== null && this.recipe.manualAlcohol !== undefined) {
            return this.recipe.manualAlcohol;
        }

        const computedAlcohol = this.computeAlcoholFromSteps();
        return computedAlcohol ?? 0;
    }

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    private calculateTotalPreparationTime(): number | null {
        const hasPrep = this.recipe.prepTime !== null && this.recipe.prepTime !== undefined;
        const hasCook = this.recipe.cookTime !== null && this.recipe.cookTime !== undefined;

        if (hasPrep === false && hasCook === false) {
            return null;
        }

        const prep = this.recipe.prepTime ?? 0;
        const cook = this.recipe.cookTime ?? 0;
        const total = prep + cook;

        if (hasPrep && hasCook) {
            return total;
        }

        return hasPrep ? prep : cook;
    }

    private computeFiberFromSteps(): number | null {
        if (this.recipe.steps.length === 0) {
            return null;
        }

        let totalFiber = 0;
        let hasFiber = false;

        for (const step of this.recipe.steps) {
            for (const ingredient of step.ingredients) {
                const fiberPerBase = ingredient.productFiberPerBase;
                const baseAmount = ingredient.productBaseAmount;
                if (
                    fiberPerBase === null ||
                    fiberPerBase === undefined ||
                    baseAmount === null ||
                    baseAmount === undefined ||
                    baseAmount === 0
                ) {
                    continue;
                }

                const multiplier = ingredient.amount / baseAmount;
                totalFiber += fiberPerBase * multiplier;
                hasFiber = true;
            }
        }

        return hasFiber ? Math.round(totalFiber * NUTRIENT_ROUNDING_FACTOR) / NUTRIENT_ROUNDING_FACTOR : null;
    }

    private computeAlcoholFromSteps(): number | null {
        if (this.recipe.steps.length === 0) {
            return null;
        }

        let totalAlcohol = 0;
        let hasAlcohol = false;

        for (const step of this.recipe.steps) {
            for (const ingredient of step.ingredients) {
                const alcoholPerBase = ingredient.productAlcoholPerBase;
                const baseAmount = ingredient.productBaseAmount;
                if (
                    alcoholPerBase === null ||
                    alcoholPerBase === undefined ||
                    baseAmount === null ||
                    baseAmount === undefined ||
                    baseAmount === 0
                ) {
                    continue;
                }

                const multiplier = ingredient.amount / baseAmount;
                totalAlcohol += alcoholPerBase * multiplier;
                hasAlcohol = true;
            }
        }

        return hasAlcohol ? Math.round(totalAlcohol * NUTRIENT_ROUNDING_FACTOR) / NUTRIENT_ROUNDING_FACTOR : null;
    }

    private computeIngredientCount(): number {
        if (this.recipe.steps.length === 0) {
            return 0;
        }

        return this.recipe.steps.reduce((total, step) => total + step.ingredients.length, 0);
    }

    private resolveNutrientValue(value?: number | null, manual?: number | null): number {
        if (manual !== null && manual !== undefined) {
            return manual;
        }

        if (value !== null && value !== undefined) {
            return value;
        }

        return 0;
    }

    public onEdit(): void {
        if (this.isEditDisabled) {
            return;
        }

        this.dialogRef.close(new RecipeDetailActionResult(this.recipe.id, 'Edit', this.hasFavoriteChanged()));
    }

    public onDelete(): void {
        if (this.isDeleteDisabled) {
            return;
        }

        this.showConfirmDialog();
    }

    public onDuplicate(): void {
        if (this.isDuplicateInProgress) {
            return;
        }

        this.isDuplicateInProgress = true;
        this.recipeService.duplicate(this.recipe.id).subscribe({
            next: duplicated => {
                this.dialogRef.close(new RecipeDetailActionResult(duplicated.id, 'Duplicate', this.hasFavoriteChanged()));
            },
            error: () => {
                this.isDuplicateInProgress = false;
            },
        });
    }

    public toggleFavorite(): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            if (this.favoriteRecipeId !== null && this.favoriteRecipeId.length > 0) {
                this.favoriteRecipeService.remove(this.favoriteRecipeId).subscribe({
                    next: () => {
                        this.isFavorite.set(false);
                        this.favoriteRecipeId = null;
                        this.isFavoriteLoading.set(false);
                    },
                    error: () => {
                        this.isFavoriteLoading.set(false);
                    },
                });
                return;
            }

            this.favoriteRecipeService
                .getAll()
                .pipe(
                    switchMap(favorites => {
                        const match = favorites.find(f => f.recipeId === this.recipe.id);
                        return match === undefined ? of(null) : this.favoriteRecipeService.remove(match.id);
                    }),
                )
                .subscribe({
                    next: () => {
                        this.isFavorite.set(false);
                        this.favoriteRecipeId = null;
                        this.isFavoriteLoading.set(false);
                    },
                    error: () => {
                        this.isFavoriteLoading.set(false);
                    },
                });
        } else {
            this.favoriteRecipeService.add(this.recipe.id).subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteRecipeId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => {
                    this.isFavoriteLoading.set(false);
                },
            });
        }
    }

    private hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }

    private showConfirmDialog(): void {
        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('CONFIRM_DELETE.TITLE', {
                type: this.translateService.instant('RECIPE_DETAIL.ENTITY_NAME'),
            }),
            message: this.translateService.instant('CONFIRM_DELETE.MESSAGE', { name: this.recipe.name }),
            name: this.recipe.name,
            entityType: this.translateService.instant('RECIPE_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translateService.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translateService.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, {
                size: 'sm',
                data,
            })
            .afterClosed()
            .subscribe(confirm => {
                if (confirm === true) {
                    this.dialogRef.close(new RecipeDetailActionResult(this.recipe.id, 'Delete', this.hasFavoriteChanged()));
                }
            });
    }
}
