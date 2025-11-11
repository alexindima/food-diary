import { ChangeDetectionStrategy, Component, TemplateRef, ViewChild, inject } from '@angular/core';
import { Recipe, RecipeVisibility } from '../../../types/recipe.data';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientChartData } from '../../../types/charts.data';
import { RecipeService } from '../../../services/recipe.service';

@Component({
    selector: 'fd-recipe-detail',
    templateUrl: './recipe-detail.component.html',
    styleUrls: ['./recipe-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, TuiButton, NutrientsSummaryComponent],
})
export class RecipeDetailComponent {
    public readonly context = injectContext<TuiDialogContext<RecipeDetailActionResult, Recipe>>();
    private readonly dialogService = inject(TuiDialogService);
    private readonly recipeService = inject(RecipeService);

    public readonly recipe: Recipe = this.context.data;
    public readonly calories: number = this.recipe.totalCalories ?? 0;
    public readonly nutrientChartData: NutrientChartData = {
        proteins: this.recipe.totalProteins ?? 0,
        fats: this.recipe.totalFats ?? 0,
        carbs: this.recipe.totalCarbs ?? 0,
    };

    public readonly nutrientSummaryConfig: NutrientsSummaryConfig = {
        styles: {
            charts: {
                chartBlockSize: 140,
            },
        },
    };

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<boolean, void>>;
    public isDuplicateInProgress = false;

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

        const computed = this.computeFiberFromSteps();
        return computed ?? 0;
    }

    public getIngredientCount(): number {
        if (!this.recipe?.steps?.length) {
            return 0;
        }

        return this.recipe.steps.reduce(
            (total, step) => total + (step.ingredients?.length ?? 0),
            0,
        );
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

        this.context.completeWith(new RecipeDetailActionResult(this.recipe.id, 'Edit'));
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
                this.context.completeWith(new RecipeDetailActionResult(duplicated.id, 'Duplicate'));
            },
            error: () => {
                this.isDuplicateInProgress = false;
            },
        });
    }

    private showConfirmDialog(): void {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(confirm => {
                if (confirm) {
                    this.context.completeWith(new RecipeDetailActionResult(this.recipe.id, 'Delete'));
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
