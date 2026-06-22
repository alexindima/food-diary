import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { GamificationFacade } from '../../lib/gamification.facade';
import { GamificationBadgesCardComponent } from '../gamification-page-sections/badges-card/gamification-badges-card';
import { GamificationHealthScoreCardComponent } from '../gamification-page-sections/health-score-card/gamification-health-score-card';
import { GamificationStatsGridComponent } from '../gamification-page-sections/stats-grid/gamification-stats-grid';
import { GAMIFICATION_TOUR } from './gamification-tour';

@Component({
    selector: 'fd-gamification-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        GamificationStatsGridComponent,
        GamificationHealthScoreCardComponent,
        GamificationBadgesCardComponent,
    ],
    templateUrl: './gamification-page.html',
    styleUrl: './gamification-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [GamificationFacade],
})
export class GamificationPageComponent {
    private readonly facade = inject(GamificationFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly isLoading = this.facade.isLoading;
    protected readonly currentStreak = this.facade.currentStreak;
    protected readonly longestStreak = this.facade.longestStreak;
    protected readonly totalMealsLogged = this.facade.totalMealsLogged;
    protected readonly healthScore = this.facade.healthScore;
    protected readonly weeklyAdherence = this.facade.weeklyAdherence;
    protected readonly badges = this.facade.badges;

    public constructor() {
        this.facade.initialize();
    }

    protected startGamificationTour(force = true): void {
        this.tourService.start(this.localizedTour.build(GAMIFICATION_TOUR), { force });
    }
}
