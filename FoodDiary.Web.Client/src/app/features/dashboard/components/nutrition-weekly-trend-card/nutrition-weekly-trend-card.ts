import { DecimalPipe, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { merge, startWith } from 'rxjs';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import type { WeeklyCaloriesPoint } from '../../models/dashboard.data';

const PROTEIN_CALORIES_PER_GRAM = 4;
const CARB_CALORIES_PER_GRAM = 4;
const FAT_CALORIES_PER_GRAM = 9;
const FIBER_CALORIES_PER_GRAM = 2;
const TREND_TICK_COUNT = 5;
const TREND_DAYS = 7;
const CALORIE_SCALE_STEP = 500;
const PERCENT = 100;

type TrendPoint = {
    date: string;
    label: string;
    dayLabel: string;
    monthLabel: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    proteinHeight: number;
    carbHeight: number;
    fatHeight: number;
    fiberHeight: number;
    isLatest: boolean;
};

@Component({
    selector: 'fd-nutrition-weekly-trend-card',
    imports: [DecimalPipe, FdUiIconComponent, TranslatePipe],
    templateUrl: './nutrition-weekly-trend-card.html',
    styleUrl: './nutrition-weekly-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionWeeklyTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly points = input.required<WeeklyCaloriesPoint[]>();
    public readonly dailyGoal = input.required<number>();
    public readonly carbGoal = input<number | null>(null);
    public readonly details = output();

    protected readonly maxCalories = computed(() => {
        const maxStack = Math.max(0, ...this.points().map(point => this.calculateStackCalories(point)));
        const upperBound = Math.max(this.dailyGoal(), maxStack);
        return Math.max(CALORIE_SCALE_STEP, Math.ceil(upperBound / CALORIE_SCALE_STEP) * CALORIE_SCALE_STEP);
    });
    protected readonly ticks = computed(() =>
        Array.from({ length: TREND_TICK_COUNT }, (_, index) =>
            Math.round(this.maxCalories() * ((TREND_TICK_COUNT - 1 - index) / (TREND_TICK_COUNT - 1))),
        ),
    );
    protected readonly goalPosition = computed(() => Math.min(PERCENT, (this.dailyGoal() / this.maxCalories()) * PERCENT));
    protected readonly trendPoints = computed<TrendPoint[]>(() => {
        this.translationChange();
        const locale = resolveTranslateLanguage(this.translateService);
        const points = this.points().slice(-TREND_DAYS);

        return points.map((point, index) => {
            const proteins = point.proteins ?? 0;
            const fats = point.fats ?? 0;
            const carbs = point.carbs ?? 0;
            const fiber = point.fiber ?? 0;
            const maxCalories = this.maxCalories();

            return {
                date: point.date,
                label: formatDate(point.date, 'd MMM', locale),
                dayLabel: formatDate(point.date, 'd', locale),
                monthLabel: formatDate(point.date, 'MMM', locale),
                calories: point.calories,
                proteins,
                fats,
                carbs,
                fiber,
                proteinHeight: (proteins * PROTEIN_CALORIES_PER_GRAM * PERCENT) / maxCalories,
                carbHeight: (carbs * CARB_CALORIES_PER_GRAM * PERCENT) / maxCalories,
                fatHeight: (fats * FAT_CALORIES_PER_GRAM * PERCENT) / maxCalories,
                fiberHeight: (fiber * FIBER_CALORIES_PER_GRAM * PERCENT) / maxCalories,
                isLatest: index === points.length - 1,
            };
        });
    });
    protected readonly latestPoint = computed(() => this.trendPoints().at(-1) ?? null);
    protected readonly hasCarbExcess = computed(() => {
        const latest = this.latestPoint();
        const goal = this.carbGoal();
        return latest !== null && goal !== null && goal > 0 && latest.carbs > goal;
    });

    private calculateStackCalories(point: WeeklyCaloriesPoint): number {
        return (
            (point.proteins ?? 0) * PROTEIN_CALORIES_PER_GRAM +
            (point.carbs ?? 0) * CARB_CALORIES_PER_GRAM +
            (point.fats ?? 0) * FAT_CALORIES_PER_GRAM +
            (point.fiber ?? 0) * FIBER_CALORIES_PER_GRAM
        );
    }
}
