import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { merge, startWith } from 'rxjs';

import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame';
import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';
import {
    buildDayNutrientBarViewModels,
    calculateDaySummaryGoalPosition,
    calculateDaySummaryPercent,
    resolveDaySummaryScaleMax,
} from './day-nutrition-summary.utils';

const RING_RADIUS = 104;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;
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
    imports: [DecimalPipe, TranslatePipe, DashboardWidgetFrameComponent],
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
    protected readonly ringDasharray = computed(() => {
        const progress = (Math.min(PERCENT, this.dailyPercent()) / PERCENT) * RING_CIRCUMFERENCE;
        return `${progress} ${RING_CIRCUMFERENCE}`;
    });
    protected readonly weeklyPercent = computed(() => calculateDaySummaryPercent(this.data().weeklyConsumed, this.data().weeklyGoal ?? 0));
    protected readonly scaleMax = computed(() => resolveDaySummaryScaleMax(this.data().nutrientBars ?? []));
    protected readonly showGoalMarkers = computed(() => this.scaleMax() > PERCENT);
    protected readonly goalPosition = computed(() => calculateDaySummaryGoalPosition(this.scaleMax()));
    protected readonly bars = computed(() => {
        this.translationChange();

        return buildDayNutrientBarViewModels(this.data().nutrientBars ?? [], this.scaleMax()).map(bar => ({
            ...bar,
            labelText: bar.labelKey !== undefined && bar.labelKey.length > 0 ? this.translateService.instant(bar.labelKey) : bar.label,
            unitText: bar.unitKey !== undefined && bar.unitKey.length > 0 ? this.translateService.instant(bar.unitKey) : bar.unit,
            background: `linear-gradient(90deg, ${bar.colorStart}, ${bar.colorEnd})`,
        }));
    });
}
