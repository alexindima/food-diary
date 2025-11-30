import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Recipe } from '../../../types/recipe.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { RecipeService } from '../../../services/recipe.service';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';

@Component({
    selector: 'fd-recipe-detail',
    standalone: true,
    templateUrl: './recipe-detail.component.html',
    styleUrls: ['./recipe-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        BaseChartDirective,
    ],
})
export class RecipeDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<RecipeDetailComponent, RecipeDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);

    public readonly recipe: Recipe;
    public readonly calories: number;
    public readonly proteins: number;
    public readonly fats: number;
    public readonly carbs: number;
    public readonly fiber: number;
    public readonly pieChartData: ChartData<'pie', number[], string>;
    public readonly barChartData: ChartData<'bar', number[], string>;
    public readonly pieChartOptions: ChartOptions<'pie'>;
    public readonly barChartOptions: ChartOptions<'bar'>;
    public readonly chartSize = 200;
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
    }[];
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'RECIPE_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'RECIPE_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly totalTime: number | null;
    public readonly ingredientCount: number;

    public isDuplicateInProgress = false;

    public constructor() {
        const data = inject<Recipe>(FD_UI_DIALOG_DATA);

        this.recipe = data;
        this.calories = this.resolveNutrientValue(data.totalCalories, data.manualCalories);
        this.proteins = this.resolveNutrientValue(data.totalProteins, data.manualProteins);
        this.fats = this.resolveNutrientValue(data.totalFats, data.manualFats);
        this.carbs = this.resolveNutrientValue(data.totalCarbs, data.manualCarbs);
        this.fiber = this.fiberValueComputed;
        this.totalTime = this.calculateTotalPreparationTime();
        this.ingredientCount = this.computeIngredientCount();
        const labels = [
            this.translateService.instant('NUTRIENTS.PROTEINS'),
            this.translateService.instant('NUTRIENTS.FATS'),
            this.translateService.instant('NUTRIENTS.CARBS'),
        ];
        const datasetValues = [this.proteins, this.fats, this.carbs];
        const colors = [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs];
        this.pieChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        this.barChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        const tooltipLabel = (label: string, value: number) =>
            `${label}: ${value.toFixed(2)} ${this.translateService.instant('STATISTICS.GRAMS')}`;
        this.pieChartOptions = {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.barChartOptions = {
            responsive: true,
            scales: {
                x: { display: false },
                y: { beginAtZero: true },
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.macroBlocks = [
            {
                labelKey: 'PRODUCT_LIST.PROTEINS',
                value: this.proteins,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.proteins,
            },
            {
                labelKey: 'PRODUCT_LIST.FATS',
                value: this.fats,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.fats,
            },
            {
                labelKey: 'PRODUCT_LIST.CARBS',
                value: this.carbs,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.carbs,
            },
            {
                labelKey: 'PRODUCT_DETAIL.SUMMARY.FIBER',
                value: this.fiber,
                unitKey: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                color: CHART_COLORS.fiber,
            },
        ];
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

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    private calculateTotalPreparationTime(): number | null {
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

    private computeIngredientCount(): number {
        if (!this.recipe?.steps?.length) {
            return 0;
        }

        return this.recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
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
