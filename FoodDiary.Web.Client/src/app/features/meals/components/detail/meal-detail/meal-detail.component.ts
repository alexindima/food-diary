import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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
import type { Meal } from '../../../models/meal.data';
import { MEAL_DETAIL_MACRO_SUMMARY_LIMIT } from '../meal-detail-lib/meal-detail.config';
import { MealDetailFacade } from '../meal-detail-lib/meal-detail.facade';
import { buildMealDetailViewModel, type MealDetailNutritionForm } from '../meal-detail-lib/meal-detail.mapper';
import type { MealDetailItemPreview, MealMacroBlock, MealSatietyMeta } from '../meal-detail-lib/meal-detail.types';
import { MealDetailSummaryComponent } from '../meal-detail-summary/meal-detail-summary.component';

@Component({
    selector: 'fd-meal-detail',
    standalone: true,
    templateUrl: './meal-detail.component.html',
    styleUrls: ['./meal-detail.component.scss'],
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

    public readonly isFavorite = this.mealDetailFacade.isFavorite;
    public readonly isFavoriteLoading = this.mealDetailFacade.isFavoriteLoading;
    public readonly favoriteIcon = this.mealDetailFacade.favoriteIcon;
    public readonly favoriteAriaLabelKey = this.mealDetailFacade.favoriteAriaLabelKey;

    public readonly consumption: Meal;
    public readonly calories: number;
    public readonly proteins: number;
    public readonly fats: number;
    public readonly carbs: number;
    public readonly fiber: number;
    public readonly alcohol: number;
    public readonly formattedDate: string | null;
    public readonly mealTypeLabel: string | null;
    public readonly preMealSatietyMeta: MealSatietyMeta;
    public readonly postMealSatietyMeta: MealSatietyMeta;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'CONSUMPTION_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'CONSUMPTION_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly isItemPreviewExpanded = signal(false);
    public readonly macroBlocks: MealMacroBlock[];
    public readonly macroSummaryBlocks = computed(() => this.macroBlocks.slice(0, MEAL_DETAIL_MACRO_SUMMARY_LIMIT));
    public readonly itemPreview: MealDetailItemPreview[];
    public readonly nutritionControlNames: NutritionControlNames;
    public readonly nutritionForm: FormGroup<MealDetailNutritionForm>;
    public readonly macroBarState: NutritionMacroState;

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

    public close(): void {
        this.mealDetailFacade.close(this.consumption);
    }

    public toggleFavorite(): void {
        this.mealDetailFacade.toggleFavorite(this.consumption);
    }

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    public toggleItemPreviewExpanded(): void {
        this.isItemPreviewExpanded.update(isExpanded => !isExpanded);
    }

    public onRepeat(): void {
        this.mealDetailFacade.repeat(this.consumption);
    }

    public onEdit(): void {
        this.mealDetailFacade.edit(this.consumption);
    }

    public onDelete(): void {
        this.mealDetailFacade.delete(this.consumption);
    }
}
