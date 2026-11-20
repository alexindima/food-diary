import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiLineChartComponent, type FdUiLineChartPoint, type FdUiLineChartReferenceLine } from 'fd-ui-kit';
import { map } from 'rxjs';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue, parseCalendarDateUtc, parseDateValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import {
    WEIGHT_TREND_CHART_MINIMUM_PADDING,
    WEIGHT_TREND_CHART_RANGE_PADDING_RATIO,
    WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
    WEIGHT_TREND_FILL_COLOR_PERCENT,
    WEIGHT_TREND_MINIMUM_CHART_POINTS,
} from './weight-trend-card.config';

export type WeightTrendPoint = {
    date: string | Date;
    value: number | null;
};

@Component({
    selector: 'fd-weight-trend-card',
    imports: [
        LocalizedNumberPipe,
        CommonModule,
        TranslatePipe,
        RouterLink,
        FdUiButtonComponent,
        FdUiLineChartComponent,
        DashboardWidgetFrameComponent,
    ],
    templateUrl: './weight-trend-card.html',
    styleUrl: './weight-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    protected readonly locale = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
    private readonly measurements = inject(MeasurementSystemService);
    private readonly router = inject(Router);
    public readonly title = input<string>('WEIGHT_CARD.TITLE');
    public readonly currentWeight = input.required<number | null>();
    public readonly currentDate = input<string | null>(null);
    public readonly change = input.required<number | null>();
    public readonly timeframeLabel = input.required<string>();
    public readonly actionLabelKey = input<string>('WEIGHT_TREND_CARD.ADD_WEIGHT');
    public readonly actionRoute = input<string>('/weight-history');
    public readonly firstMeasurementKey = input<string>('WEIGHT_TREND_CARD.FIRST_MEASUREMENT');
    public readonly twoMeasurementsKey = input<string>('WEIGHT_TREND_CARD.TWO_MEASUREMENTS');
    public readonly stableTrendKey = input<string>('WEIGHT_TREND_CARD.STABLE_TREND');
    public readonly points = input.required<WeightTrendPoint[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly unitKey = input<string>('WEIGHT_CARD.KG');
    public readonly measurementKind = input<'weight' | 'length'>('weight');
    public readonly emptyStateKey = input<string>('WEIGHT_TREND_CARD.NO_DATA');
    public readonly iconName = input<string | null>('monitor_weight');
    public readonly accentColor = input<string>('var(--fd-color-blue-500)');
    public readonly targetValue = input<number | null>(null);

    protected readonly measurementPoints = computed<ReadonlyArray<FdUiLineChartPoint & { value: number }>>(() => {
        const points = this.points()
            .flatMap(({ date, value }) => {
                if (value === null || !Number.isFinite(value)) {
                    return [];
                }

                const encodedDate = typeof date === 'string' ? date : parseDateValue(date)?.toISOString();
                const calendarDate = parseCalendarDateUtc(encodedDate);
                return calendarDate === null ? [] : [{ timestamp: calendarDate.getTime(), value }];
            })
            .sort((left, right) => left.timestamp - right.timestamp);
        const firstTimestamp = points[0]?.timestamp ?? 0;
        const lastTimestamp = points.at(-1)?.timestamp ?? firstTimestamp;
        const range = lastTimestamp - firstTimestamp;
        const formatter = new Intl.DateTimeFormat(this.locale(), {
            day: 'numeric',
            month: 'long',
            timeZone: 'UTC',
        });

        return points.map(point => ({
            label: formatter.format(point.timestamp),
            value: this.displayValue(point.value),
            xPosition: range > 0 ? (point.timestamp - firstTimestamp) / range : undefined,
        }));
    });
    protected readonly measurementCount = computed(() => this.measurementPoints().length);
    protected readonly hasMeasurements = computed(() => this.measurementCount() > 0);
    protected readonly hasEnoughMeasurements = computed(() => this.measurementCount() >= WEIGHT_TREND_MINIMUM_CHART_POINTS);
    protected readonly showChart = this.hasEnoughMeasurements;
    protected readonly sparseMessageKey = computed(() => {
        if (!this.hasMeasurements()) {
            return 'WEIGHT_TREND_CARD.NO_RECENT_MEASUREMENTS';
        }
        return this.measurementCount() === 1 ? this.firstMeasurementKey() : this.twoMeasurementsKey();
    });
    protected readonly measurementDateLabel = computed(() =>
        formatDateValue(parseCalendarDateUtc(this.currentDate()), this.locale(), {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
            timeZone: 'UTC',
        }),
    );
    protected readonly chartBounds = computed(() => {
        const values = this.measurementPoints().map(point => point.value);
        const minimum = Math.min(...values);
        const maximum = Math.max(...values);
        const padding = Math.max((maximum - minimum) * WEIGHT_TREND_CHART_RANGE_PADDING_RATIO, WEIGHT_TREND_CHART_MINIMUM_PADDING);
        return { minimum: minimum - padding, maximum: maximum + padding };
    });

    protected readonly chartFillColor = computed(
        () => `color-mix(in srgb, ${this.accentColor()} ${WEIGHT_TREND_FILL_COLOR_PERCENT}%, transparent)`,
    );
    protected readonly referenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        const targetValue = this.targetValue();
        return targetValue === null
            ? []
            : [
                  {
                      value: this.displayValue(targetValue),
                      color: this.accentColor(),
                  },
              ];
    });
    protected readonly goalLabel = computed(() => {
        const targetValue = this.targetValue();
        return targetValue === null
            ? null
            : this.translateService.instant('WEIGHT_TREND_CARD.CHART_GOAL', {
                  value: new Intl.NumberFormat(this.locale(), {
                      maximumFractionDigits: 1,
                  }).format(this.displayValue(targetValue)),
                  unit: this.translateService.instant(this.displayUnitKey()),
              });
    });

    protected readonly formattedChangeValue = computed(() => {
        const delta = this.displayValue(this.change() ?? 0, WEIGHT_TREND_DISPLAY_FRACTION_DIGITS);
        if (delta === 0 || !Number.isFinite(delta)) {
            return null;
        }
        const sign = delta > 0 ? '+' : '';
        const formattedDelta = new Intl.NumberFormat(this.locale(), {
            minimumFractionDigits: WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
            maximumFractionDigits: WEIGHT_TREND_DISPLAY_FRACTION_DIGITS,
        }).format(delta);
        return `${sign}${formattedDelta}`;
    });

    protected readonly displayCurrentValue = computed(() => {
        const value = this.currentWeight();
        return value === null || !Number.isFinite(value) ? null : this.displayValue(value);
    });
    protected readonly displayUnitKey = computed(() =>
        this.measurementKind() === 'weight' ? this.measurements.weightUnitKey() : this.measurements.lengthUnitKey(),
    );

    protected addMeasurement(): void {
        if (!this.isLoading()) {
            void this.router.navigateByUrl(this.router.createUrlTree([this.actionRoute()], { queryParams: { addEntry: true } }));
        }
    }

    private displayValue(value: number, fractionDigits?: number): number {
        return this.measurementKind() === 'weight'
            ? this.measurements.displayWeight(value, fractionDigits)
            : this.measurements.displayLength(value, fractionDigits);
    }
}
