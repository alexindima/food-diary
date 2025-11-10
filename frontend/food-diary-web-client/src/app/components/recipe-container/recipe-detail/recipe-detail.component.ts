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

    public get visibilityKey(): string {
        return `RECIPE_VISIBILITY.${this.recipe.visibility}`;
    }

    public get isDeleteDisabled(): boolean {
        return !this.recipe.isOwnedByCurrentUser || this.recipe.usageCount > 0;
    }

    public get isEditDisabled(): boolean {
        return !this.recipe.isOwnedByCurrentUser;
    }

    public get warningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.recipe.isOwnedByCurrentUser
            ? 'RECIPE_DETAIL.WARNING_IN_USE'
            : 'RECIPE_DETAIL.WARNING_NOT_OWNER';
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

export type RecipeDetailAction = 'Edit' | 'Delete';

export class RecipeDetailActionResult {
    public constructor(
        public id: string,
        public action: RecipeDetailAction,
    ) {}
}
