import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

import type { GamificationStatTile } from '../../gamification-page-lib/gamification-page.models';
import { buildGamificationStats } from '../../gamification-page-lib/gamification-page-view.mapper';

@Component({
    selector: 'fd-gamification-stats-grid',
    imports: [TranslatePipe, FdUiIconComponent, FdUiAccentSurfaceComponent],
    templateUrl: './gamification-stats-grid.component.html',
    styleUrl: '../../gamification-page/gamification-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationStatsGridComponent {
    public readonly currentStreak = input.required<number>();
    public readonly longestStreak = input.required<number>();
    public readonly totalMealsLogged = input.required<number>();
    public readonly weeklyAdherence = input.required<number>();

    protected readonly stats = computed<GamificationStatTile[]>(() =>
        buildGamificationStats(this.currentStreak(), this.longestStreak(), this.totalMealsLogged(), this.weeklyAdherence()),
    );
}
