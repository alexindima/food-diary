import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { Badge } from '../../../models/gamification.data';
import type { BadgeDisplay } from '../../gamification-page-lib/gamification-page.models';
import { buildBadgeDisplays, filterEarnedBadges, filterLockedBadges } from '../../gamification-page-lib/gamification-page-view.mapper';

@Component({
    selector: 'fd-gamification-badges-card',
    imports: [TranslatePipe, FdUiIconComponent, FdUiCardComponent],
    templateUrl: './gamification-badges-card.html',
    styleUrl: '../../gamification-page/gamification-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationBadgesCardComponent {
    public readonly badges = input.required<Badge[]>();

    protected readonly badgeDisplays = computed<BadgeDisplay[]>(() => buildBadgeDisplays(this.badges()));
    protected readonly earnedBadges = computed<BadgeDisplay[]>(() => filterEarnedBadges(this.badgeDisplays()));
    protected readonly lockedBadges = computed<BadgeDisplay[]>(() => filterLockedBadges(this.badgeDisplays()));
}
