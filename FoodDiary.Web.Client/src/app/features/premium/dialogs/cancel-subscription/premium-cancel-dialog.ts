import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { CANCELLATION_REASONS, type CancellationFeedback } from '../../models/cancellation.models';

export type PremiumCancelDialogData = { periodEnd: string | null; immediate: boolean };

@Component({
    selector: 'fd-premium-cancel-dialog',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        TranslatePipe,
        FdUiDialogShellComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
    ],
    templateUrl: './premium-cancel-dialog.html',
})
export class PremiumCancelDialogComponent {
    protected readonly data = inject<PremiumCancelDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<PremiumCancelDialogComponent, CancellationFeedback>>(FdUiDialogRef);
    private readonly translate = inject(TranslateService);
    private readonly draft = signal({ reasonCode: '', comment: '' });
    protected readonly survey = form(this.draft);
    protected readonly reasons = computed(() => [
        { value: '', label: this.translate.instant('PREMIUM_PAGE.CANCELLATION.SKIP_REASON') },
        ...CANCELLATION_REASONS.map(value => ({ value, label: this.translate.instant(`PREMIUM_PAGE.CANCELLATION.REASONS.${value}`) })),
    ]);

    protected confirm(): void {
        // No survey validator participates in cancellation.
        const draft = this.draft();
        this.dialogRef.close({
            reasonCode: draft.reasonCode.length > 0 ? draft.reasonCode : null,
            comment: draft.comment.trim().length > 0 ? draft.comment.trim() : null,
        });
    }

    protected close(): void {
        this.dialogRef.close();
    }
}
