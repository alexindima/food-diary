import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { GamificationFacade } from '../../lib/gamification.facade';
import { GamificationBadgesCardComponent } from '../gamification-page-sections/badges-card/gamification-badges-card.component';
import { GamificationHealthScoreCardComponent } from '../gamification-page-sections/health-score-card/gamification-health-score-card.component';
import { GamificationStatsGridComponent } from '../gamification-page-sections/stats-grid/gamification-stats-grid.component';

@Component({
    selector: 'fd-gamification-page',
    standalone: true,
    imports: [
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        GamificationStatsGridComponent,
        GamificationHealthScoreCardComponent,
        GamificationBadgesCardComponent,
    ],
    templateUrl: './gamification-page.component.html',
    styleUrl: './gamification-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [GamificationFacade],
})
export class GamificationPageComponent {
    private readonly facade = inject(GamificationFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly currentStreak = this.facade.currentStreak;
    public readonly longestStreak = this.facade.longestStreak;
    public readonly totalMealsLogged = this.facade.totalMealsLogged;
    public readonly healthScore = this.facade.healthScore;
    public readonly weeklyAdherence = this.facade.weeklyAdherence;
    public readonly badges = this.facade.badges;

    public constructor() {
        this.facade.initialize();
    }
}
