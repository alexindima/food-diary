import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { GamificationFacade } from '../lib/gamification.facade';

@Component({
    selector: 'fd-gamification-page',
    standalone: true,
    imports: [
        UpperCasePipe,
        TranslatePipe,
        FdUiIconComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdUiCardComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './gamification-page.component.html',
    styleUrl: './gamification-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [GamificationFacade],
})
export class GamificationPageComponent {
    private readonly facade = inject(GamificationFacade);

    protected readonly Math = Math;

    public readonly isLoading = this.facade.isLoading;
    public readonly currentStreak = this.facade.currentStreak;
    public readonly longestStreak = this.facade.longestStreak;
    public readonly totalMealsLogged = this.facade.totalMealsLogged;
    public readonly healthScore = this.facade.healthScore;
    public readonly weeklyAdherence = this.facade.weeklyAdherence;
    public readonly earnedBadges = this.facade.earnedBadges;
    public readonly lockedBadges = this.facade.lockedBadges;
    public readonly getBadgeIcon = this.facade.getBadgeIcon.bind(this.facade);

    public constructor() {
        this.facade.initialize();
    }
}
