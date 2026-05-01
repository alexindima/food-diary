import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { AdminModerationService } from '../api/admin-moderation.service';
import { AdminContentReport } from '../models/admin-moderation.data';

export type AdminModerationActionDialogData = {
    report: AdminContentReport;
    action: 'review' | 'dismiss';
};

export type AdminModerationActionDialogResult = {
    confirmed: boolean;
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

    public get dialogTitle(): string {
        return this.data.action === 'review' ? 'Review Report' : 'Dismiss Report';
    }

    public onConfirm(): void {
        this.isSubmitting.set(true);
        const action = { adminNote: this.adminNote.trim() || null };

        const operation =
            this.data.action === 'review'
                ? this.moderationService.reviewReport(this.data.report.id, action)
                : this.moderationService.dismissReport(this.data.report.id, action);

        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => this.dialogRef.close({ confirmed: true }),
            error: () => {
                this.isSubmitting.set(false);
            },
        });
    }

    public onCancel(): void {
        this.dialogRef.close({ confirmed: false });
    }
}
