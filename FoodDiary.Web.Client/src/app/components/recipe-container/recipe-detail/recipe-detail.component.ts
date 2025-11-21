import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Recipe, RecipeVisibility } from '../../../types/recipe.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig,
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientData } from '../../../types/charts.data';
import { RecipeService } from '../../../services/recipe.service';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FdUiConfirmDialogComponent,
    FdUiConfirmDialogData,
} from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';

@Component({
    selector: 'fd-recipe-detail',
    standalone: true,
    templateUrl: './recipe-detail.component.html',
    styleUrls: ['./recipe-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        NutrientsSummaryComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
    ],
})
export class RecipeDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<RecipeDetailComponent, RecipeDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);

    public readonly recipe: Recipe;
    public readonly calories: number;
    public readonly nutrientChartData: NutrientData;

    public readonly nutrientSummaryConfig: NutrientsSummaryConfig = {
        styles: {
            charts: {
                chartBlockSize: 140,
            },
        },
    };

    public isDuplicateInProgress = false;

    public constructor() {
        const data = inject<Recipe>(FD_UI_DIALOG_DATA);

        this.recipe = data;
        this.calories = this.recipe.totalCalories ?? 0;
        this.nutrientChartData = {
            proteins: this.recipe.totalProteins ?? 0,
            fats: this.recipe.totalFats ?? 0,
            carbs: this.recipe.totalCarbs ?? 0,
        };
    }

    public get visibilityKey(): string {
        return `RECIPE_VISIBILITY.${this.recipe.visibility}`;
    }

    public get isDeleteDisabled(): boolean {
        return !this.recipe.isOwnedByCurrentUser || this.recipe.usageCount > 0;
    }

    public get isEditDisabled(): boolean {
        return !this.recipe.isOwnedByCurrentUser || this.recipe.usageCount > 0;
    }

    public get canModify(): boolean {
        return !this.isEditDisabled;
    }

    public get warningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.recipe.isOwnedByCurrentUser
            ? 'RECIPE_DETAIL.WARNING_IN_USE'
            : 'RECIPE_DETAIL.WARNING_NOT_OWNER';
    }

    public get fiberValueComputed(): number {
        if (this.recipe.totalFiber !== null && this.recipe.totalFiber !== undefined) {
            return this.recipe.totalFiber;
        }

        if (this.recipe.manualFiber !== null && this.recipe.manualFiber !== undefined) {
            return this.recipe.manualFiber;
        }

        const computed = this.computeFiberFromSteps();
        return computed ?? 0;
    }

    public getIngredientCount(): number {
        if (!this.recipe?.steps?.length) {
            return 0;
        }

        return this.recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }

    public getTotalPreparationTime(): number | null {
        const hasPrep = this.recipe.prepTime !== null && this.recipe.prepTime !== undefined;
        const hasCook = this.recipe.cookTime !== null && this.recipe.cookTime !== undefined;

        if (!hasPrep && !hasCook) {
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
        if (!this.recipe.steps?.length) {
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

        return hasFiber ? Math.round(totalFiber * 100) / 100 : null;
    }

    public onEdit(): void {
        if (this.isEditDisabled) {
            return;
        }

        this.dialogRef.close(new RecipeDetailActionResult(this.recipe.id, 'Edit'));
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
                this.dialogRef.close(new RecipeDetailActionResult(duplicated.id, 'Duplicate'));
            },
            error: () => {
                this.isDuplicateInProgress = false;
            },
        });
    }

    private showConfirmDialog(): void {
        const data: FdUiConfirmDialogData = {
            title: this.translateService.instant('RECIPE_DETAIL.CONFIRM_DELETE'),
            message: this.recipe.name,
            confirmLabel: this.translateService.instant('RECIPE_DETAIL.CONFIRM_BUTTON'),
            cancelLabel: this.translateService.instant('RECIPE_DETAIL.CANCEL_BUTTON'),
            danger: true,
        };

        this.fdDialogService
            .open(FdUiConfirmDialogComponent, {
                size: 'sm',
                data,
            })
            .afterClosed()
            .subscribe(confirm => {
                if (confirm) {
                    this.dialogRef.close(new RecipeDetailActionResult(this.recipe.id, 'Delete'));
                }
            });
    }
}

export type RecipeDetailAction = 'Edit' | 'Delete' | 'Duplicate';

export class RecipeDetailActionResult {
    public constructor(
        public id: string,
        public action: RecipeDetailAction,
    ) {}
}
