import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import { badgeIcon, badgeNameKey } from '../../lib/gamification-badge-view';
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
    protected readonly locale = injectCurrentLanguage();
    private readonly data = inject<GamificationAchievementsDialogData>(FD_UI_DIALOG_DATA);

    protected readonly badges = this.data.badges;
    protected readonly badgeIcon = badgeIcon;
    protected readonly badgeNameKey = badgeNameKey;
}
