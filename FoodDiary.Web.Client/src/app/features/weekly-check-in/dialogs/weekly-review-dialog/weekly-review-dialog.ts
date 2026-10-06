import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { WeeklyReviewViewModel } from '../../lib/weekly-check-in.types';
import type { WeekSummary } from '../../models/weekly-check-in.data';
import type { WeeklyGoal } from '../../models/weekly-goal.data';

export type WeeklyReviewDialogData = {
    review: WeeklyReviewViewModel;
    week: WeekSummary;
    goal?: WeeklyGoal | null;
};

@Component({
    selector: 'fd-weekly-review-dialog',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiDialogComponent, FdUiIconComponent],
    templateUrl: './weekly-review-dialog.html',
    styleUrl: './weekly-review-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyReviewDialogComponent {
    protected readonly data = inject<WeeklyReviewDialogData>(FD_UI_DIALOG_DATA);
    protected readonly language = injectCurrentLanguage();
}
