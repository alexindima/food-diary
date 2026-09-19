import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    type ElementRef,
    inject,
    input,
    linkedSignal,
    signal,
    viewChild,
    viewChildren,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { merge, startWith } from 'rxjs';

import { LocalizedNumberPipe } from '../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame';
import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';
import { buildDayNutrientContour, EXTREME_NUTRIENT_PERCENT, getDayNutrientAnchor } from './day-nutrition-contour.utils';
import { buildDayNutrientBarViewModels, calculateDaySummaryPercent } from './day-nutrition-summary.utils';

const PERCENT = 100;
let uniqueId = 0;

type DayNutritionSummaryData = {
    dailyGoal: number;
    mealCount?: number;
    dailyConsumed: number;
    weeklyConsumed: number;
    weeklyGoal: number | null;
    nutrientBars: NutrientBar[] | null;
};

@Component({
    selector: 'fd-day-nutrition-summary',
    imports: [LocalizedNumberPipe, TranslatePipe, DashboardWidgetFrameComponent, FdUiIconComponent, FdUiButtonComponent],
    templateUrl: './day-nutrition-summary.html',
    styleUrl: './day-nutrition-summary.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DayNutritionSummaryComponent {
    private readonly visual = viewChild<ElementRef<HTMLElement>>('visual');
    private readonly nutrientLabels = viewChildren<ElementRef<HTMLElement>>('nutrientLabel');
    protected readonly connectorEnds = signal<Record<string, { x: number; y: number }>>({});
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly data = input.required<DayNutritionSummaryData>();
    protected readonly insightIndex = linkedSignal({ source: this.data, computation: () => 0 });
    protected readonly insights = computed(() => {
        this.translationChange();
        const translate = (key: string): string => this.translateService.instant(`DASHBOARD.DAY_SUMMARY.${key}`);
        const bars = this.bars().filter(bar => bar.target > 0);
        if ((this.data().mealCount ?? 0) === 0 && this.data().dailyConsumed === 0 && bars.every(bar => bar.current === 0)) {
            return [{ title: translate('PULSE_EMPTY_TITLE'), text: translate('PULSE_EMPTY_TEXT') }];
        }
        const result = [{ title: translate('PULSE_CALORIES'), text: this.calorieComparisonText() }];
        const highest = [...bars].sort((a, b) => b.percent - a.percent).at(0);
        const lowest = [...bars].sort((a, b) => a.percent - b.percent).at(0);
        if (highest !== undefined && highest.current > highest.target) {
            result.push({ title: highest.labelText, text: highest.comparisonText });
        }
        if (lowest !== undefined && lowest.current < lowest.target) {
            result.push({ title: lowest.labelText, text: lowest.comparisonText });
        }
        return result;
    });
    protected readonly activeInsight = computed(() => this.insights()[this.insightIndex() % this.insights().length]);

    protected moveInsight(direction: number): void {
        this.insightIndex.update(index => (index + direction + this.insights().length) % this.insights().length);
    }
    protected readonly chartId = `day-nutrition-${uniqueId++}`;
    protected readonly language = computed(() => {
        this.translationChange();
        return resolveTranslateLanguage(this.translateService);
    });
    protected readonly dailyPercent = computed(() => calculateDaySummaryPercent(this.data().dailyConsumed, this.data().dailyGoal));
    protected readonly calorieRingProgress = computed(() => {
        const { dailyConsumed, dailyGoal } = this.data();
        if (!Number.isFinite(dailyConsumed) || !Number.isFinite(dailyGoal) || dailyGoal <= 0) {
            return 0;
        }
        return Math.min(PERCENT, Math.max(0, (dailyConsumed / dailyGoal) * PERCENT));
    });
    protected readonly calorieDifference = computed(() => Math.abs(this.data().dailyGoal - this.data().dailyConsumed));
    protected readonly calorieComparisonText = computed(() => {
        this.translationChange();

        return this.buildComparisonText(this.data().dailyConsumed, this.data().dailyGoal, this.translateService.instant('MEAL_RING.UNIT'));
    });
    protected readonly bars = computed(() => {
        this.translationChange();

        return buildDayNutrientBarViewModels(this.data().nutrientBars ?? [], PERCENT).map(bar => {
            const unitText = bar.unitKey !== undefined && bar.unitKey.length > 0 ? this.translateService.instant(bar.unitKey) : bar.unit;

            return {
                ...bar,
                labelText: bar.labelKey !== undefined && bar.labelKey.length > 0 ? this.translateService.instant(bar.labelKey) : bar.label,
                unitText,
                anchor: getDayNutrientAnchor(bar.id, bar.percent),
                isExtreme: bar.percent >= EXTREME_NUTRIENT_PERCENT,
                comparisonText: this.buildComparisonText(bar.current, bar.target, unitText),
            };
        });
    });

    protected readonly contourPath = computed(() => buildDayNutrientContour(this.bars()));
    protected readonly contourGradient = computed(() => {
        const colors = ['fats', 'fiber', 'carbs', 'protein', 'fats'].map(
            id => this.bars().find(bar => bar.id === id)?.colorEnd ?? 'transparent',
        );
        return `conic-gradient(from 45deg, ${colors.join(', ')})`;
    });

    public constructor() {
        afterRenderEffect(onCleanup => {
            const visual = this.visual()?.nativeElement;
            const labels = this.nutrientLabels().map(label => label.nativeElement);
            const bars = this.bars();
            if (visual === undefined || typeof ResizeObserver === 'undefined') {
                return;
            }

            const update = (): void => {
                const chart = visual.getBoundingClientRect();
                if (chart.width === 0) {
                    return;
                }
                const viewBoxSize = 416;
                const viewBoxOffset = -48;
                const half = 2;
                const scale = viewBoxSize / chart.width;
                const ends: Record<string, { x: number; y: number }> = {};
                labels.forEach((label, index) => {
                    const bar = bars[index];
                    if (bar.anchor === null) {
                        return;
                    }
                    const rect = label.getBoundingClientRect();
                    const x = (rect.left + rect.width / half - chart.left) * scale + viewBoxOffset;
                    const y = (rect.top + rect.height / half - chart.top) * scale + viewBoxOffset;
                    const dx = x - bar.anchor.x;
                    const dy = y - bar.anchor.y;
                    const inset = Math.min(((rect.width / half) * scale) / Math.abs(dx), ((rect.height / half) * scale) / Math.abs(dy), 1);
                    ends[bar.id] = { x: x - dx * inset, y: y - dy * inset };
                });
                this.connectorEnds.set(ends);
            };
            const observer = new ResizeObserver(update);
            observer.observe(visual);
            if (visual.parentElement !== null) {
                observer.observe(visual.parentElement);
            }
            labels.forEach(label => {
                observer.observe(label);
            });
            update();
            onCleanup(() => {
                observer.disconnect();
            });
        });
    }

    protected getNutrientIcon(id: string): string {
        return id === 'protein' ? 'fitness_center' : id === 'carbs' ? 'grass' : id === 'fats' ? 'water_drop' : 'spa';
    }

    private buildComparisonText(current: number, target: number, unit: string): string {
        const difference = Math.round(Math.abs(target - current));
        if (current === target) {
            return this.translateService.instant('DASHBOARD.DAY_SUMMARY.TARGET_REACHED');
        }

        const key = current > target ? 'DASHBOARD.DAY_SUMMARY.EXCEEDED_BY' : 'DASHBOARD.DAY_SUMMARY.REMAINING';
        const value = new Intl.NumberFormat(resolveAppLocale(this.language())).format(difference);
        return this.translateService.instant(key, { value, unit });
    }
}
