import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiLineChartComponent, FdUiSectionStateComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

export type StatisticsBodyChartPoint = {
    label: string;
    value: number | null;
};

@Component({
    selector: 'fd-statistics-body',
    imports: [CommonModule, TranslateModule, FdUiCardComponent, FdUiTabsComponent, FdUiSectionStateComponent, FdUiLineChartComponent],
    templateUrl: './statistics-body.component.html',
    styleUrls: ['./statistics-body.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsBodyComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input.required<string>();
    public readonly isLoading = input.required<boolean>();
    public readonly hasLoadError = input.required<boolean>();
    public readonly bodyChartPoints = input.required<readonly StatisticsBodyChartPoint[]>();
    public readonly hasBodyData = input.required<boolean>();
    public readonly noDataKey = input<string>('STATISTICS.BODY_NO_DATA');
    public readonly loadErrorKey = input<string>('ERRORS.LOAD_FAILED_MESSAGE');

    public readonly selectedTabChange = output<string>();
    public readonly retry = output();
    public readonly sectionState = computed<'loading' | 'error' | 'content' | 'empty'>(() => {
        if (this.isLoading()) {
            return 'loading';
        }

        if (this.hasLoadError()) {
            return 'error';
        }

        return this.hasBodyData() ? 'content' : 'empty';
    });
    public readonly bodyChartUnitKey = computed(() => {
        const selectedTab = this.selectedTab();

        if (selectedTab === 'weight') {
            return 'GENERAL.UNITS.KG';
        }

        if (selectedTab === 'waist') {
            return 'GENERAL.UNITS.CM';
        }

        return '';
    });

    public onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }

    public onRetry(): void {
        this.retry.emit();
    }
}
