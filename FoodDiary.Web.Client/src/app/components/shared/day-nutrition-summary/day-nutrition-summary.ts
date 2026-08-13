import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconComponent, FdUiProgressRingComponent } from 'fd-ui-kit';
import { merge, startWith } from 'rxjs';

import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame';
import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';
import {
    buildDayNutrientBarViewModels,
    calculateDaySummaryGoalPosition,
    calculateDaySummaryPercent,
    resolveDaySummaryScaleMax,
} from './day-nutrition-summary.utils';

const PERCENT = 100;

type DayNutritionSummaryData = {
    dailyGoal: number;
    dailyConsumed: number;
    weeklyConsumed: number;
    weeklyGoal: number | null;
    nutrientBars: NutrientBar[] | null;
};

@Component({
    selector: 'fd-day-nutrition-summary',
    imports: [DecimalPipe, TranslatePipe, DashboardWidgetFrameComponent, FdUiIconComponent, FdUiProgressRingComponent],
    templateUrl: './day-nutrition-summary.html',
    styleUrl: './day-nutrition-summary.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DayNutritionSummaryComponent {
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly data = input.required<DayNutritionSummaryData>();
    protected readonly dailyPercent = computed(() => calculateDaySummaryPercent(this.data().dailyConsumed, this.data().dailyGoal));
    protected readonly dailyProgressValue = computed(() => Math.min(PERCENT, this.dailyPercent()));
    protected readonly weeklyPercent = computed(() => calculateDaySummaryPercent(this.data().weeklyConsumed, this.data().weeklyGoal ?? 0));
    protected readonly calorieComparisonText = computed(() => {
        this.translationChange();

        return this.buildComparisonText(this.data().dailyConsumed, this.data().dailyGoal, this.translateService.instant('MEAL_RING.UNIT'));
    });
    protected readonly scaleMax = computed(() => resolveDaySummaryScaleMax(this.data().nutrientBars ?? []));
    protected readonly showGoalMarkers = computed(() => this.scaleMax() > PERCENT);
    protected readonly goalPosition = computed(() => calculateDaySummaryGoalPosition(this.scaleMax()));
    protected readonly bars = computed(() => {
        this.translationChange();

        return buildDayNutrientBarViewModels(this.data().nutrientBars ?? [], this.scaleMax()).map(bar => {
            const unitText = bar.unitKey !== undefined && bar.unitKey.length > 0 ? this.translateService.instant(bar.unitKey) : bar.unit;

            return {
                ...bar,
                labelText: bar.labelKey !== undefined && bar.labelKey.length > 0 ? this.translateService.instant(bar.labelKey) : bar.label,
                unitText,
                background: `linear-gradient(90deg, ${bar.colorStart}, ${bar.colorEnd})`,
                comparisonText: this.buildComparisonText(bar.current, bar.target, unitText),
            };
        });
    });

    protected getNutrientIcon(id: string): string {
        return id === 'protein' ? 'fitness_center' : id === 'carbs' ? 'grass' : id === 'fats' ? 'water_drop' : 'spa';
    }

    private buildComparisonText(current: number, target: number, unit: string): string {
        const difference = Math.round(Math.abs(target - current));
        if (current === target) {
            return this.translateService.instant('DASHBOARD.DAY_SUMMARY.TARGET_REACHED');
        }

        const key = current > target ? 'DASHBOARD.DAY_SUMMARY.EXCEEDED_BY' : 'DASHBOARD.DAY_SUMMARY.REMAINING';
        return this.translateService.instant(key, { value: difference, unit });
    }
}
