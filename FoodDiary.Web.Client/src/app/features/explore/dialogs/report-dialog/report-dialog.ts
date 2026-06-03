import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { CreateReportDto } from '../../models/report.data';
import { REPORT_REASON_MAX_LENGTH } from './report-dialog.tokens';

export type ReportDialogData = {
    targetType: 'Recipe' | 'Comment';
    targetId: string;
};

@Component({
    selector: 'fd-report-dialog',
    templateUrl: './report-dialog.html',
    styleUrls: ['./report-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormField, TranslatePipe, FdUiButtonComponent, FdUiTextareaComponent, FdUiDialogComponent],
})
export class ReportDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<ReportDialogComponent, boolean>>(FdUiDialogRef);
    private readonly data = inject<ReportDialogData>(FD_UI_DIALOG_DATA);
    private readonly exploreInteractionsFacade = inject(ExploreInteractionsFacade);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly reportReasonMaxLength = inject(REPORT_REASON_MAX_LENGTH);

    protected readonly formModel = signal({
        reason: '',
    });
    protected readonly form = form(this.formModel, path => {
        required(path.reason);
        maxLength(path.reason, this.reportReasonMaxLength);
    });
    protected readonly isSubmitting = signal(false);

    protected onSubmit(): void {
        this.form().markAsTouched();
        const reason = this.formModel().reason.trim();
        if (reason.length === 0 || this.form().invalid() || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        const dto: CreateReportDto = {
            targetType: this.data.targetType,
            targetId: this.data.targetId,
            reason,
        };

        this.exploreInteractionsFacade
            .createReport(dto)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.toastService.success(this.translateService.instant('REPORT.SUCCESS'));
                    this.dialogRef.close(true);
                },
                error: () => {
                    this.isSubmitting.set(false);
                },
            });
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}
