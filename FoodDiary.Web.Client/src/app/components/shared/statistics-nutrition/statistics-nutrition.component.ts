import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import {
    FdUiBarChartComponent,
    type FdUiBarChartItem,
    FdUiLineChartComponent,
    type FdUiLineChartPoint,
    type FdUiLineChartSeries,
    FdUiPieChartComponent,
    type FdUiPieChartSegment,
    FdUiSectionStateComponent,
} from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

export type NutritionTrendGroup = FdUiLineChartSeries & {
    key: string;
};

@Component({
    selector: 'fd-statistics-nutrition',
    imports: [
        CommonModule,
        TranslateModule,
        FdUiSectionStateComponent,
        FdUiCardComponent,
        FdUiTabsComponent,
        FdUiLineChartComponent,
        FdUiPieChartComponent,
        FdUiBarChartComponent,
    ],
    templateUrl: './statistics-nutrition.component.html',
    styleUrls: ['./statistics-nutrition.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutritionComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input.required<string>();
    public readonly hasData = input.required<boolean>();

    public readonly caloriesTrendPoints = input.required<readonly FdUiLineChartPoint[]>();
    public readonly nutrientTrendGroups = input.required<readonly NutritionTrendGroup[]>();
    public readonly nutrientPieSegments = input.required<readonly FdUiPieChartSegment[]>();
    public readonly nutrientBarItems = input.required<readonly FdUiBarChartItem[]>();

    public readonly selectedTabChange = output<string>();

    protected onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }
}
