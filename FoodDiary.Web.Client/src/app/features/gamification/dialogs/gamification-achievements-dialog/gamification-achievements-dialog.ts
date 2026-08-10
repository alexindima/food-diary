import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import type { Badge } from '../../models/gamification.data';

export type GamificationAchievementsDialogData = {
    badges: Badge[];
};

@Component({
    selector: 'fd-gamification-achievements-dialog',
    imports: [TranslatePipe, LocalizedDatePipe, FdUiDialogShellComponent, FdUiIconComponent],
    templateUrl: './gamification-achievements-dialog.html',
    styleUrl: './gamification-achievements-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationAchievementsDialogComponent {
    private readonly data = inject<GamificationAchievementsDialogData>(FD_UI_DIALOG_DATA);

    protected readonly badges = this.data.badges;

    protected badgeIcon(badge: Badge): string {
        return badge.icon ?? (badge.category === 'streak' ? 'local_fire_department' : 'restaurant');
    }

    protected badgeNameKey(badge: Badge): string {
        return `GAMIFICATION.BADGE_${badge.key.toUpperCase()}`;
    }
}
