import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { type ChartConfiguration } from 'chart.js';
import {
    FdUiIconComponent,
    FdUiMenuComponent,
    FdUiMenuItemComponent,
    FdUiMenuTriggerDirective,
    FdUiSectionStateComponent,
} from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';
import { BaseChartDirective } from 'ng2-charts';

export interface SummaryMacro {
    key: string;
    labelKey: string;
    value: number;
    color: string;
}

export interface SummaryMetrics {
    totalCalories: number;
    averageCard: {
        consumption: number;
        steps: number;
        burned: number;
    };
    macros: SummaryMacro[];
}

export type StatisticsSummaryExportFormat = 'csv' | 'pdf';

@Component({
    selector: 'fd-statistics-summary',
    standalone: true,
    imports: [
        CommonModule,
        TranslateModule,
        FdUiSectionStateComponent,
        FdUiCardComponent,
        FdUiCardActionsDirective,
        FdUiAccentSurfaceComponent,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
        BaseChartDirective,
    ],
    templateUrl: './statistics-summary.component.html',
    styleUrls: ['./statistics-summary.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsSummaryComponent {
    public readonly summary = input<SummaryMetrics | null>(null);
    public readonly summarySparklineData = input<ChartConfiguration<'line'>['data'] | null>(null);
    public readonly summarySparklineOptions = input<ChartConfiguration['options'] | null>(null);
    public readonly macroSparklineData = input<Record<string, ChartConfiguration<'line'>['data']> | null>(null);
    public readonly emptyKey = input<string>('STATISTICS.NO_DATA');
    public readonly exportingFormat = input<StatisticsSummaryExportFormat | null>(null);
    public readonly exportRequested = output<StatisticsSummaryExportFormat>();

    public export(format: StatisticsSummaryExportFormat): void {
        this.exportRequested.emit(format);
    }
}
