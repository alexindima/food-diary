import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
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
    template: `
        <fd-ui-dialog [title]="dialogTitle" size="sm">
            <p><strong>Target:</strong> {{ data.report.targetType }} ({{ data.report.targetId }})</p>
            <p><strong>Reason:</strong> {{ data.report.reason }}</p>

            <label>Admin note (optional)</label>
            <fd-ui-textarea [(ngModel)]="adminNote" placeholder="Add an admin note..." [rows]="3" />

            <div class="dialog-actions">
                <fd-ui-button type="button" variant="secondary" fill="text" size="sm" (click)="onCancel()"> Cancel </fd-ui-button>
                <fd-ui-button
                    type="button"
                    [variant]="data.action === 'review' ? 'danger' : 'secondary'"
                    size="sm"
                    [disabled]="isSubmitting()"
                    (click)="onConfirm()"
                >
                    {{ data.action === 'review' ? 'Review & Remove' : 'Dismiss' }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styles: `
        .dialog-actions {
            display: flex;
            justify-content: flex-end;
            gap: 8px;
            margin-top: 16px;
        }
        label {
            display: block;
            font-weight: 500;
            margin: 12px 0 4px;
        }
        p {
            margin: 4px 0;
        }
    `,
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
