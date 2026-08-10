import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';

import type { WeeklyReviewViewModel } from '../../lib/weekly-check-in.types';
import type { WeekSummary } from '../../models/weekly-check-in.data';

export type WeeklyReviewDialogData = {
    review: WeeklyReviewViewModel;
    week: WeekSummary;
};

@Component({
    selector: 'fd-weekly-review-dialog',
    imports: [DecimalPipe, TranslatePipe, FdUiDialogComponent, FdUiIconComponent],
    templateUrl: './weekly-review-dialog.html',
    styleUrl: './weekly-review-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyReviewDialogComponent {
    protected readonly data = inject<WeeklyReviewDialogData>(FD_UI_DIALOG_DATA);
}
