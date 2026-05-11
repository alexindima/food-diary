import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { ReportService } from '../../api/report.service';
import type { CreateReportDto } from '../../models/report.data';

export interface ReportDialogData {
    targetType: 'Recipe' | 'Comment';
    targetId: string;
}

const REPORT_REASON_MAX_LENGTH = 1000;

@Component({
    selector: 'fd-report-dialog',
    templateUrl: './report-dialog.component.html',
    styleUrls: ['./report-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiTextareaComponent, FdUiDialogComponent],
})
export class ReportDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<ReportDialogComponent, boolean>>(FdUiDialogRef);
    private readonly data = inject<ReportDialogData>(FD_UI_DIALOG_DATA);
    private readonly reportService = inject(ReportService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly reasonControl = new FormControl('', [Validators.required, Validators.maxLength(REPORT_REASON_MAX_LENGTH)]);
    public readonly isSubmitting = signal(false);

    public onSubmit(): void {
        if (this.reasonControl.invalid || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        const dto: CreateReportDto = {
            targetType: this.data.targetType,
            targetId: this.data.targetId,
            reason: (this.reasonControl.value ?? '').trim(),
        };

        this.reportService
            .create(dto)
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

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
