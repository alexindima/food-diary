import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { FdUiBarChartItem, FdUiPieChartSegment } from 'fd-ui-kit';

import type { NutrientData } from '../../../shared/models/charts.data';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { ChartColorsService } from '../../../shared/theme/chart-colors.service';
import { CustomGroupComponent } from '../custom-group/custom-group';
import { NutrientsSummaryChartsComponent } from './nutrients-summary-charts/nutrients-summary-charts';
import { mergeNutrientsSummaryConfig, type NutrientsSummaryConfig } from './nutrients-summary-lib/nutrients-summary.config';

@Component({
    selector: 'fd-nutrients-summary',
    imports: [DecimalPipe, TranslatePipe, CustomGroupComponent, NgTemplateOutlet, NutrientsSummaryChartsComponent],
    templateUrl: './nutrients-summary.html',
    styleUrl: './nutrients-summary.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientsSummaryComponent {
    protected readonly CHART_COLORS = inject(ChartColorsService).palette;

    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly browserWindow = inject(BrowserWindowService);

    public readonly calories = input.required<number>();
    public readonly nutrientChartData = input.required<NutrientData>();
    public readonly fiberValue = input<number | null>(null);
    public readonly fiberUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public readonly alcoholValue = input<number | null>(null);
    public readonly alcoholUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public readonly bare = input<boolean>(false);
    public readonly showBarChart = input<boolean>(false);

    public readonly config = input<NutrientsSummaryConfig>({});
    protected readonly mergedConfig = computed(() => mergeNutrientsSummaryConfig(this.config()));
    private readonly viewportWidth = signal(
        this.browserWindow.isAvailable() ? this.browserWindow.getViewportWidth() : Number.MAX_SAFE_INTEGER,
    );
    protected readonly isColumnLayout = computed(() => this.viewportWidth() <= this.mergedConfig().styles.charts.breakpoints.columnLayout);
    protected readonly areChartsBelowInfo = computed(
        () => this.viewportWidth() <= this.mergedConfig().styles.common.infoBreakpoints.columnLayout,
    );
    protected readonly summaryWrapperStyles = computed(() => {
        const gapValue = this.isColumnLayout()
            ? this.mergedConfig().styles.common.infoBreakpoints.gap
            : this.mergedConfig().styles.common.gap;

        return { gap: `${gapValue}px` };
    });
    protected readonly nutrientStyles = computed(() => {
        const { fontSize, lineHeight } = this.mergedConfig().styles.info.lineStyles.nutrients;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    });
    protected readonly nutrientColorStyles = computed(() => {
        const { colorWidthMultiplier, fontSize } = this.mergedConfig().styles.info.lineStyles.nutrients;
        return {
            height: `${fontSize}px`,
            width: `${fontSize * colorWidthMultiplier}px`,
        };
    });
    protected readonly chartsWrapperStyles = computed(() => {
        const gapValue = this.isColumnLayout() ? this.mergedConfig().styles.charts.breakpoints.gap : this.mergedConfig().styles.charts.gap;

        return {
            gap: `${gapValue}px`,
        };
    });
    protected readonly chartsBlockSize = computed(() => {
        if (this.areChartsBelowInfo()) {
            return this.mergedConfig().styles.common.infoBreakpoints.chartBlockSize;
        }

        return this.isColumnLayout()
            ? this.mergedConfig().styles.charts.breakpoints.chartBlockSize
            : this.mergedConfig().styles.charts.chartBlockSize;
    });
    protected readonly chartStyles = computed(() => ({
        width: `${this.chartsBlockSize()}px`,
        height: `${this.chartsBlockSize()}px`,
    }));
    protected readonly hasNutrientData = computed(() => {
        const data = this.nutrientChartData();
        return data.proteins + data.fats + data.carbs > 0;
    });
    protected readonly nutrientPieSegments = computed<FdUiPieChartSegment[]>(() => this.buildNutrientChartItems());
    protected readonly nutrientBarItems = computed<FdUiBarChartItem[]>(() => this.buildNutrientChartItems());

    public constructor() {
        if (!this.browserWindow.isAvailable()) {
            return;
        }

        const removeResizeListener = this.browserWindow.onResize(() => {
            this.viewportWidth.set(this.browserWindow.getViewportWidth());
        });
        this.destroyRef.onDestroy(removeResizeListener);
    }

    private buildNutrientChartItems(): FdUiPieChartSegment[] {
        const data = this.nutrientChartData();

        return [
            { label: this.translateService.instant('NUTRIENTS.PROTEINS'), value: data.proteins, color: this.CHART_COLORS.proteins },
            { label: this.translateService.instant('NUTRIENTS.FATS'), value: data.fats, color: this.CHART_COLORS.fats },
            { label: this.translateService.instant('NUTRIENTS.CARBS'), value: data.carbs, color: this.CHART_COLORS.carbs },
        ];
    }
}
