import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { fdUiCoerceInputTextValue, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminModerationFacade } from '../lib/admin-moderation.facade';
import type { AdminContentReport } from '../models/admin-moderation.data';

export type AdminModerationActionDialogData = {
    report: AdminContentReport;
    action: 'review' | 'dismiss';
};

export type AdminModerationActionDialogResult = {
    confirmed: boolean;
};

type AdminModerationActionDialogViewState = {
    title: string;
    confirmVariant: 'danger' | 'secondary';
    confirmLabel: string;
};

@Component({
    selector: 'fd-admin-moderation-action-dialog',
    imports: [FdUiButtonComponent, FdUiTextareaComponent, FdUiDialogComponent],
    templateUrl: './admin-moderation-action-dialog.html',
    styleUrls: ['./admin-moderation-action-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationActionDialogComponent {
    protected readonly data = inject<AdminModerationActionDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef =
        inject<FdUiDialogRef<AdminModerationActionDialogComponent, AdminModerationActionDialogResult>>(FdUiDialogRef);
    private readonly moderationFacade = inject(AdminModerationFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly adminNote = signal('');
    protected readonly isSubmitting = signal(false);

    protected readonly viewState = computed((): AdminModerationActionDialogViewState =>
        this.data.action === 'review'
            ? {
                  title: 'Review Report',
                  confirmVariant: 'danger',
                  confirmLabel: 'Review & Remove',
              }
            : {
                  title: 'Dismiss Report',
                  confirmVariant: 'secondary',
                  confirmLabel: 'Dismiss',
              },
    );

    protected onConfirm(): void {
        this.isSubmitting.set(true);
        const adminNote = this.adminNote().trim();
        const action = { adminNote: adminNote.length > 0 ? adminNote : null };

        const operation =
            this.data.action === 'review'
                ? this.moderationFacade.reviewReport(this.data.report.id, action)
                : this.moderationFacade.dismissReport(this.data.report.id, action);

        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.dialogRef.close({ confirmed: true });
            },
            error: () => {
                this.isSubmitting.set(false);
            },
        });
    }

    protected onCancel(): void {
        this.dialogRef.close({ confirmed: false });
    }

    protected updateAdminNote(value: FdUiInputValue): void {
        this.adminNote.set(fdUiCoerceInputTextValue(value));
    }
}
