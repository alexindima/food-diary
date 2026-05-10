import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { AdminModerationService } from '../api/admin-moderation.service';
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
    standalone: true,
    imports: [FormsModule, FdUiButtonComponent, FdUiTextareaComponent, FdUiDialogComponent],
    templateUrl: './admin-moderation-action-dialog.component.html',
    styleUrls: ['./admin-moderation-action-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationActionDialogComponent {
    public readonly data = inject<AdminModerationActionDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef =
        inject<FdUiDialogRef<AdminModerationActionDialogComponent, AdminModerationActionDialogResult>>(FdUiDialogRef);
    private readonly moderationService = inject(AdminModerationService);
    private readonly destroyRef = inject(DestroyRef);

    public adminNote = '';
    public readonly isSubmitting = signal(false);

    public readonly viewState = computed(
        (): AdminModerationActionDialogViewState =>
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

    public onConfirm(): void {
        this.isSubmitting.set(true);
        const adminNote = this.adminNote.trim();
        const action = { adminNote: adminNote.length > 0 ? adminNote : null };

        const operation =
            this.data.action === 'review'
                ? this.moderationService.reviewReport(this.data.report.id, action)
                : this.moderationService.dismissReport(this.data.report.id, action);

        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.dialogRef.close({ confirmed: true });
            },
            error: () => {
                this.isSubmitting.set(false);
            },
        });
    }

    public onCancel(): void {
        this.dialogRef.close({ confirmed: false });
    }
}
