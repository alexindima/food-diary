import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { type ChartConfiguration } from 'chart.js';
import { FdUiSectionStateComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';

@Component({
    selector: 'fd-statistics-body',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiCardComponent, FdUiTabsComponent, FdUiSectionStateComponent, BaseChartDirective],
    templateUrl: './statistics-body.component.html',
    styleUrls: ['./statistics-body.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsBodyComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input<string>('weight');
    public readonly isLoading = input<boolean>(false);
    public readonly hasLoadError = input<boolean>(false);
    public readonly bodyChartData = input<ChartConfiguration<'line'>['data'] | null>(null);
    public readonly bodyChartOptions = input<ChartConfiguration['options'] | null>(null);
    public readonly hasBodyData = input<boolean>(false);
    public readonly noDataKey = input<string>('STATISTICS.BODY_NO_DATA');
    public readonly loadErrorKey = input<string>('ERRORS.LOAD_FAILED_MESSAGE');

    public readonly selectedTabChange = output<string>();
    public readonly retry = output<void>();
    public readonly sectionState = computed<'loading' | 'error' | 'content' | 'empty'>(() => {
        if (this.isLoading()) {
            return 'loading';
        }

        if (this.hasLoadError()) {
            return 'error';
        }

        return this.hasBodyData() ? 'content' : 'empty';
    });

    public onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }

    public onRetry(): void {
        this.retry.emit();
    }
}
