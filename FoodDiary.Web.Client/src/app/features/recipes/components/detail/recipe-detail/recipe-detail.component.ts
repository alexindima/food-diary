import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import type { Recipe } from '../../../models/recipe.data';
import { RecipeDetailFacade } from '../recipe-detail-lib/recipe-detail.facade';
import type { IngredientPreviewItem, MacroBlock } from '../recipe-detail-lib/recipe-detail.types';
import { buildRecipeDetailViewModel, type RecipeDetailNutritionForm } from '../recipe-detail-lib/recipe-detail-nutrition.mapper';
import { RecipeDetailSummaryComponent } from '../recipe-detail-summary/recipe-detail-summary.component';

@Component({
    selector: 'fd-recipe-detail',
    templateUrl: './recipe-detail.component.html',
    styleUrls: ['./recipe-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeDetailFacade],
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
    private readonly recipeDetailFacade = inject(RecipeDetailFacade);

    protected readonly isFavorite = this.recipeDetailFacade.isFavorite;
    protected readonly isFavoriteLoading = this.recipeDetailFacade.isFavoriteLoading;
    protected readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    protected readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'RECIPE_DETAIL.REMOVE_FAVORITE' : 'RECIPE_DETAIL.ADD_FAVORITE',
    );

    protected readonly recipe: Recipe;
    protected readonly calories: number;
    protected readonly proteins: number;
    protected readonly fats: number;
    protected readonly carbs: number;
    protected readonly fiber: number;
    protected readonly alcohol: number;
    protected readonly qualityScore: number;
    protected readonly qualityGrade: string;
    protected readonly macroBlocks: MacroBlock[];
    protected readonly macroSummaryBlocks: MacroBlock[];
    protected readonly ingredientPreview: IngredientPreviewItem[];
    protected readonly nutritionControlNames: NutritionControlNames;
    protected readonly nutritionForm: FormGroup<RecipeDetailNutritionForm>;
    protected readonly macroBarState: NutritionMacroState;
    protected readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'RECIPE_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'RECIPE_DETAIL.TABS.NUTRIENTS' },
    ];
    protected activeTab: 'summary' | 'nutrients' = 'summary';
    protected readonly totalTime: number | null;
    protected readonly ingredientCount: number;
    protected readonly isDeleteDisabled: boolean;
    protected readonly isEditDisabled: boolean;
    protected readonly canModify: boolean;
    protected readonly warningMessage: string | null;

    protected readonly isDuplicateInProgress = computed(() => this.recipeDetailFacade.isDuplicateInProgress());

    public constructor() {
        const recipe = inject<Recipe>(FD_UI_DIALOG_DATA);
        const translateService = inject(TranslateService);
        const viewModel = buildRecipeDetailViewModel(recipe, translateService.instant('RECIPE_DETAIL.UNKNOWN_INGREDIENT'));

        this.recipe = recipe;
        this.calories = viewModel.calories;
        this.proteins = viewModel.proteins;
        this.fats = viewModel.fats;
        this.carbs = viewModel.carbs;
        this.fiber = viewModel.fiber;
        this.alcohol = viewModel.alcohol;
        this.qualityScore = viewModel.qualityScore;
        this.qualityGrade = viewModel.qualityGrade;
        this.macroBlocks = viewModel.macroBlocks;
        this.macroSummaryBlocks = viewModel.macroSummaryBlocks;
        this.ingredientPreview = viewModel.ingredientPreview;
        this.nutritionControlNames = viewModel.nutritionControlNames;
        this.nutritionForm = viewModel.nutritionForm;
        this.macroBarState = viewModel.macroBarState;
        this.totalTime = viewModel.totalTime;
        this.ingredientCount = viewModel.ingredientCount;
        this.isDeleteDisabled = !recipe.isOwnedByCurrentUser || recipe.usageCount > 0;
        this.isEditDisabled = !recipe.isOwnedByCurrentUser || recipe.usageCount > 0;
        this.canModify = !this.isEditDisabled;
        this.warningMessage = this.resolveWarningMessage();

        this.recipeDetailFacade.initialize(recipe);
    }

    protected close(): void {
        this.recipeDetailFacade.close(this.recipe);
    }

    protected onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    protected onEdit(): void {
        if (this.isEditDisabled) {
            return;
        }

        this.recipeDetailFacade.edit(this.recipe);
    }

    protected onDelete(): void {
        if (this.isDeleteDisabled) {
            return;
        }

        this.recipeDetailFacade.delete(this.recipe);
    }

    protected onDuplicate(): void {
        this.recipeDetailFacade.duplicate(this.recipe);
    }

    protected toggleFavorite(): void {
        this.recipeDetailFacade.toggleFavorite(this.recipe);
    }

    private resolveWarningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.recipe.isOwnedByCurrentUser ? 'RECIPE_DETAIL.WARNING_IN_USE' : 'RECIPE_DETAIL.WARNING_NOT_OWNER';
    }
}
