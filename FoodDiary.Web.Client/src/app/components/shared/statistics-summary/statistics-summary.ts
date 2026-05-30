import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import {
    FdUiIconComponent,
    FdUiLineChartComponent,
    FdUiMenuComponent,
    FdUiMenuItemComponent,
    FdUiMenuTriggerDirective,
    FdUiSectionStateComponent,
} from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';

export type SummaryMacro = {
    key: string;
    labelKey: string;
    value: number;
    color: string;
};

export type SummaryMetrics = {
    totalCalories: number;
    averageCard: {
        consumption: number;
        steps: number;
        burned: number;
    };
    macros: SummaryMacro[];
};

export type StatisticsSummaryExportFormat = 'csv' | 'pdf';

export type SummarySparklinePoint = {
    label: string;
    value: number | null;
};

@Component({
    selector: 'fd-statistics-summary',
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
        FdUiLineChartComponent,
    ],
    templateUrl: './statistics-summary.html',
    styleUrls: ['./statistics-summary.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsSummaryComponent {
    public readonly summary = input.required<SummaryMetrics | null>();
    public readonly summarySparklinePoints = input.required<readonly SummarySparklinePoint[]>();
    public readonly macroSparklinePoints = input.required<Record<string, readonly SummarySparklinePoint[]> | null>();
    public readonly emptyKey = input<string>('STATISTICS.NO_DATA');
    public readonly exportingFormat = input.required<StatisticsSummaryExportFormat | null>();
    public readonly exportRequested = output<StatisticsSummaryExportFormat>();

    protected export(format: StatisticsSummaryExportFormat): void {
        this.exportRequested.emit(format);
    }
}
