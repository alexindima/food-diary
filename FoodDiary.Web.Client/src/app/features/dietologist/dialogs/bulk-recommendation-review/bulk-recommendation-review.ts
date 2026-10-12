import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { UserId } from '../../../../shared/models/semantics/entity-id';

export type BulkRecommendationReviewData = {
    recipients: Array<{ id: UserId; label: string; email: string | null }>;
    text: string;
};

@Component({
    selector: 'fd-bulk-recommendation-review',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './bulk-recommendation-review.html',
    styleUrl: './bulk-recommendation-review.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BulkRecommendationReviewComponent {
    protected readonly data = inject<BulkRecommendationReviewData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<BulkRecommendationReviewComponent, boolean>>(FdUiDialogRef);

    protected close(confirmed: boolean): void {
        this.dialogRef.close(confirmed);
    }
}
