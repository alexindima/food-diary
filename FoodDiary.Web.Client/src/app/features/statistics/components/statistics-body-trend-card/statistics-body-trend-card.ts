import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiLineChartComponent } from 'fd-ui-kit';

import type { StatisticsBodyTrendData } from '../../models/statistics-dashboard-card.models';

const CHART_MINIMUM_PADDING = 0.5;
const CHART_PADDING_RATIO = 0.2;

@Component({
    selector: 'fd-statistics-body-trend-card',
    imports: [DecimalPipe, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiLineChartComponent],
    templateUrl: './statistics-body-trend-card.html',
    styleUrl: './statistics-body-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsBodyTrendCardComponent {
    public readonly data = input.required<StatisticsBodyTrendData>();
    public readonly historyRoute = input<string>('/weight-history');

    protected readonly hasData = computed(
        () => this.data().currentWeight !== null && this.data().points.some(point => point.value !== null),
    );
    protected readonly bounds = computed(() => {
        const values = this.data()
            .points.map(point => point.value)
            .filter((value): value is number => value !== null);
        if (values.length === 0) {
            return { minimum: 0, maximum: 1 };
        }
        const minimum = Math.min(...values);
        const maximum = Math.max(...values);
        const padding = Math.max((maximum - minimum) * CHART_PADDING_RATIO, CHART_MINIMUM_PADDING);
        return { minimum: minimum - padding, maximum: maximum + padding };
    });
}
