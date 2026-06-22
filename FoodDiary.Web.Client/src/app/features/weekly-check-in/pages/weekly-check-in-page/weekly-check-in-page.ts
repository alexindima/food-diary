import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WeeklyCheckInStatsCardComponent } from '../../components/weekly-check-in-stats-card/weekly-check-in-stats-card';
import { WeeklyCheckInSuggestionsCardComponent } from '../../components/weekly-check-in-suggestions-card/weekly-check-in-suggestions-card';
import { WeeklyCheckInTrendsComponent } from '../../components/weekly-check-in-trends/weekly-check-in-trends';
import { WeeklyCheckInFacade } from '../../lib/weekly-check-in.facade';
import { WEEKLY_CHECK_IN_TOUR } from './weekly-check-in-tour';

@Component({
    selector: 'fd-weekly-check-in-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        WeeklyCheckInTrendsComponent,
        WeeklyCheckInStatsCardComponent,
        WeeklyCheckInSuggestionsCardComponent,
    ],
    templateUrl: './weekly-check-in-page.html',
    styleUrl: './weekly-check-in-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeeklyCheckInFacade],
})
export class WeeklyCheckInPageComponent {
    private readonly facade = inject(WeeklyCheckInFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly isLoading = this.facade.isLoading;
    protected readonly thisWeek = this.facade.thisWeek;
    protected readonly suggestionRows = this.facade.suggestionRows;
    protected readonly trendCards = this.facade.trendCards;

    public constructor() {
        this.facade.initialize();
    }

    protected startWeeklyCheckInTour(force = true): void {
        this.tourService.start(this.localizedTour.build(WEEKLY_CHECK_IN_TOUR), { force });
    }
}
