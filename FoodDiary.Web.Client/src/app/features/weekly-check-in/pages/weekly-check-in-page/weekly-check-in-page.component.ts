import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { WeeklyCheckInStatsCardComponent } from '../../components/weekly-check-in-stats-card/weekly-check-in-stats-card.component';
import { WeeklyCheckInSuggestionsCardComponent } from '../../components/weekly-check-in-suggestions-card/weekly-check-in-suggestions-card.component';
import { WeeklyCheckInTrendsComponent } from '../../components/weekly-check-in-trends/weekly-check-in-trends.component';
import { WeeklyCheckInFacade } from '../../lib/weekly-check-in.facade';

@Component({
    selector: 'fd-weekly-check-in-page',
    imports: [
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        WeeklyCheckInTrendsComponent,
        WeeklyCheckInStatsCardComponent,
        WeeklyCheckInSuggestionsCardComponent,
    ],
    templateUrl: './weekly-check-in-page.component.html',
    styleUrl: './weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeeklyCheckInFacade],
})
export class WeeklyCheckInPageComponent {
    private readonly facade = inject(WeeklyCheckInFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly thisWeek = this.facade.thisWeek;
    public readonly suggestionRows = this.facade.suggestionRows;
    public readonly trendCards = this.facade.trendCards;

    public constructor() {
        this.facade.initialize();
    }
}
