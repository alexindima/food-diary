import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs';

import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import type { Meal } from '../../../models/meal.data';
import { MEAL_DETAIL_MACRO_SUMMARY_LIMIT } from '../meal-detail-lib/meal-detail.config';
import { MealDetailFacade } from '../meal-detail-lib/meal-detail.facade';
import { buildMealDetailViewModel, type MealDetailNutritionForm } from '../meal-detail-lib/meal-detail.mapper';
import type { MealDetailItemPreview, MealMacroBlock, MealSatietyMeta } from '../meal-detail-lib/meal-detail.types';
import { MealDetailSummaryComponent } from '../meal-detail-summary/meal-detail-summary';

@Component({
    selector: 'fd-meal-detail',
    templateUrl: './meal-detail.html',
    styleUrls: ['./meal-detail.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe, MealDetailFacade],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        NutritionEditorComponent,
        MealDetailSummaryComponent,
    ],
})
export class MealDetailComponent {
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);
    private readonly mealDetailFacade = inject(MealDetailFacade);

    protected readonly isFavorite = this.mealDetailFacade.isFavorite;
    protected readonly isFavoriteLoading = this.mealDetailFacade.isFavoriteLoading;
    protected readonly favoriteIcon = this.mealDetailFacade.favoriteIcon;
    protected readonly favoriteAriaLabelKey = this.mealDetailFacade.favoriteAriaLabelKey;

    protected readonly consumption: Meal;
    protected readonly calories: number;
    protected readonly proteins: number;
    protected readonly fats: number;
    protected readonly carbs: number;
    protected readonly fiber: number;
    protected readonly alcohol: number;
    protected readonly formattedDate: string | null;
    protected readonly mealTypeLabel: string | null;
    protected readonly preMealSatietyMeta: MealSatietyMeta;
    protected readonly postMealSatietyMeta: MealSatietyMeta;
    protected readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'CONSUMPTION_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'CONSUMPTION_DETAIL.TABS.NUTRIENTS' },
    ];
    protected activeTab: 'summary' | 'nutrients' = 'summary';
    protected readonly isItemPreviewExpanded = signal(false);
    protected readonly macroBlocks: MealMacroBlock[];
    protected readonly macroSummaryBlocks = computed(() => this.macroBlocks.slice(0, MEAL_DETAIL_MACRO_SUMMARY_LIMIT));
    protected readonly itemPreview: MealDetailItemPreview[];
    protected readonly nutritionControlNames: NutritionControlNames;
    protected readonly nutritionForm: FormGroup<MealDetailNutritionForm>;
    protected readonly macroBarState: NutritionMacroState;

    public constructor() {
        const meal = inject<Meal>(FD_UI_DIALOG_DATA);
        const viewModel = buildMealDetailViewModel(meal, key => this.translate.instant(key));

        this.consumption = meal;
        this.calories = viewModel.calories;
        this.proteins = viewModel.proteins;
        this.fats = viewModel.fats;
        this.carbs = viewModel.carbs;
        this.fiber = viewModel.fiber;
        this.alcohol = viewModel.alcohol;
        this.formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy, HH:mm');
        this.mealTypeLabel = viewModel.mealTypeLabel;
        this.preMealSatietyMeta = viewModel.preMealSatietyMeta;
        this.postMealSatietyMeta = viewModel.postMealSatietyMeta;
        this.itemPreview = viewModel.itemPreview;
        this.macroBlocks = viewModel.macroBlocks;
        this.nutritionControlNames = viewModel.nutritionControlNames;
        this.nutritionForm = viewModel.nutritionForm;
        this.macroBarState = viewModel.macroBarState;

        this.mealDetailFacade.initialize(meal);
    }

    protected close(): void {
        this.mealDetailFacade.close(this.consumption);
    }

    protected toggleFavorite(): void {
        this.mealDetailFacade.toggleFavorite(this.consumption);
    }

    protected onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    protected toggleItemPreviewExpanded(): void {
        this.isItemPreviewExpanded.update(isExpanded => !isExpanded);
    }

    protected onRepeat(): void {
        this.mealDetailFacade.repeat(this.consumption);
    }

    protected onEdit(): void {
        this.mealDetailFacade.edit(this.consumption);
    }

    protected onDelete(): void {
        this.mealDetailFacade.delete(this.consumption);
    }
}
