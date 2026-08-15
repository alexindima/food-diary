import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiCardComponent,
    FdUiIconComponent,
    FdUiLineChartComponent,
    type FdUiLineChartReferenceLine,
} from 'fd-ui-kit';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs';
import { merge, startWith } from 'rxjs';

import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type {
    StatisticsBodyMetricData,
    StatisticsBodyMetricKey,
    StatisticsBodyTrendData,
} from '../../models/statistics-dashboard-card.models';

const CHART_MINIMUM_PADDING = 0.5;
const CHART_PADDING_RATIO = 0.2;

@Component({
    selector: 'fd-statistics-body-trend-card',
    imports: [
        DecimalPipe,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiIconComponent,
        FdUiLineChartComponent,
        FdUiSegmentedToggleComponent,
    ],
    templateUrl: './statistics-body-trend-card.html',
    styleUrl: './statistics-body-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsBodyTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly measurements = inject(MeasurementSystemService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly data = input.required<StatisticsBodyTrendData>();

    protected readonly selectedMetric = signal<StatisticsBodyMetricKey>('weight');
    protected readonly metricTabs: FdUiTab[] = [
        { value: 'weight', labelKey: 'STATISTICS.DASHBOARD.BODY.WEIGHT' },
        { value: 'waist', labelKey: 'STATISTICS.DASHBOARD.BODY.WAIST' },
    ];
    protected readonly metric = computed<StatisticsBodyMetricData>(() => this.data()[this.selectedMetric()]);
    protected readonly displayMetric = computed<StatisticsBodyMetricData>(() => {
        const metric = this.metric();
        return {
            ...metric,
            current: this.displayValue(metric.current),
            change: this.displayValue(metric.change),
            goal: this.displayValue(metric.goal),
            points: metric.points.map(point => ({ ...point, value: this.displayValue(point.value) })),
        };
    });
    protected readonly hasData = computed(() => this.metric().points.some(point => point.value !== null));
    protected readonly measurementCount = computed(() => this.metric().points.filter(point => point.value !== null).length);
    protected readonly unitKey = computed(() =>
        this.selectedMetric() === 'weight' ? this.measurements.weightUnitKey() : this.measurements.lengthUnitKey(),
    );
    protected readonly metricIcon = computed(() => (this.selectedMetric() === 'weight' ? 'monitor_weight' : 'straighten'));
    protected readonly currentLabelKey = computed(() =>
        this.selectedMetric() === 'weight' ? 'STATISTICS.DASHBOARD.BODY.CURRENT_WEIGHT' : 'STATISTICS.DASHBOARD.BODY.CURRENT_WAIST',
    );
    protected readonly historyLabelKey = computed(() =>
        this.selectedMetric() === 'weight'
            ? 'STATISTICS.DASHBOARD.BODY.OPEN_WEIGHT_HISTORY'
            : 'STATISTICS.DASHBOARD.BODY.OPEN_WAIST_HISTORY',
    );
    protected readonly emptyLabelKey = computed(() =>
        this.selectedMetric() === 'weight' ? 'STATISTICS.DASHBOARD.BODY.EMPTY_WEIGHT' : 'STATISTICS.DASHBOARD.BODY.EMPTY_WAIST',
    );
    protected readonly historyRoute = computed(() => (this.selectedMetric() === 'weight' ? '/weight-history' : '/waist-history'));
    protected readonly distanceToGoal = computed(() => {
        const current = this.displayMetric().current;
        const goal = this.displayMetric().goal;
        return current === null || goal === null ? null : Math.abs(current - goal);
    });
    protected readonly hasPositiveChange = computed(() => (this.metric().change ?? 0) < 0);
    protected readonly hasNegativeChange = computed(() => (this.metric().change ?? 0) > 0);
    protected readonly changePrefix = computed(() => (this.hasNegativeChange() ? '+' : ''));
    protected readonly bounds = computed(() => {
        const metric = this.displayMetric();
        const values = metric.points.map(point => point.value).filter((value): value is number => value !== null);
        if (metric.goal !== null) {
            values.push(metric.goal);
        }
        if (values.length === 0) {
            return { minimum: 0, maximum: 1 };
        }
        const minimum = Math.min(...values);
        const maximum = Math.max(...values);
        const padding = Math.max((maximum - minimum) * CHART_PADDING_RATIO, CHART_MINIMUM_PADDING);
        return { minimum: Math.max(0, minimum - padding), maximum: maximum + padding };
    });
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        this.translationChange();
        const goal = this.displayMetric().goal;
        return goal === null
            ? []
            : [
                  {
                      value: goal,
                      label: this.translateService.instant('STATISTICS.DASHBOARD.BODY.CHART_GOAL', {
                          value: goal,
                          unit: this.translateService.instant(this.unitKey()),
                      }),
                      color: 'var(--fd-color-text-subtle)',
                      lineStyle: 'dashed',
                      outOfRangeBehavior: 'hide',
                  },
              ];
    });

    protected selectMetric(value: string): void {
        if (value === 'weight' || value === 'waist') {
            this.selectedMetric.set(value);
        }
    }

    private displayValue(value: number | null): number | null {
        if (value === null) {
            return null;
        }

        return this.selectedMetric() === 'weight' ? this.measurements.displayWeight(value) : this.measurements.displayLength(value);
    }
}
