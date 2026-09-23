import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';

import { ChartColorsService } from '../../../../../shared/theme/chart-colors.service';
import { MealDetailFacade } from '../../../lib/detail/meal-detail.facade';
import type { Meal } from '../../../models/meal.data';
import { buildMealDetailViewModel } from '../meal-detail-lib/meal-detail.mapper';
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

    protected readonly meal: Meal;
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
    protected readonly isItemPreviewExpanded = signal(false);
    protected readonly macroBlocks: MealMacroBlock[];
    protected readonly macroSummaryBlocks = computed(() =>
        this.macroBlocks.filter(macro => macro.labelKey !== 'GENERAL.NUTRIENTS.ALCOHOL' || macro.value !== 0),
    );
    protected readonly itemPreview: MealDetailItemPreview[];

    public constructor() {
        const meal = inject<Meal>(FD_UI_DIALOG_DATA);
        const viewModel = buildMealDetailViewModel(meal, key => this.translate.instant(key), {
            ...inject(ChartColorsService).palette,
            proteins: 'var(--fd-color-primary-600)',
            fats: 'var(--fd-color-orange-500)',
            carbs: 'var(--fd-color-sky-500)',
            fiber: 'var(--fd-color-rose-500)',
        });

        this.meal = meal;
        this.calories = viewModel.calories;
        this.proteins = viewModel.proteins;
        this.fats = viewModel.fats;
        this.carbs = viewModel.carbs;
        this.fiber = viewModel.fiber;
        this.alcohol = viewModel.alcohol;
        this.formattedDate = this.datePipe.transform(this.meal.date, 'dd.MM.yyyy, HH:mm');
        this.mealTypeLabel = viewModel.mealTypeLabel;
        this.preMealSatietyMeta = viewModel.preMealSatietyMeta;
        this.postMealSatietyMeta = viewModel.postMealSatietyMeta;
        this.itemPreview = viewModel.itemPreview;
        this.macroBlocks = viewModel.macroBlocks;

        this.mealDetailFacade.initialize(meal);
    }

    protected close(): void {
        this.mealDetailFacade.close(this.meal);
    }

    protected toggleFavorite(): void {
        this.mealDetailFacade.toggleFavorite(this.meal);
    }

    protected toggleItemPreviewExpanded(): void {
        this.isItemPreviewExpanded.update(isExpanded => !isExpanded);
    }

    protected onRepeat(): void {
        this.mealDetailFacade.repeat(this.meal);
    }

    protected onEdit(): void {
        this.mealDetailFacade.edit(this.meal);
    }

    protected onDelete(): void {
        this.mealDetailFacade.delete(this.meal);
    }
}
