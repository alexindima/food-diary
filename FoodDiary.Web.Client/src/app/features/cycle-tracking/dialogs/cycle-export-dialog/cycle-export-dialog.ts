import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { type CycleExportRange, cycleExportRangeError } from '../../lib/cycle-export-range';

export type CycleExportDialogData = CycleExportRange & { sensitive: boolean; today: string };
export type CycleExportSelection = CycleExportRange & { currentPassword?: string };

@Component({
    selector: 'fd-cycle-export-dialog',
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiFormErrorComponent,
        FdUiInputComponent,
    ],
    templateUrl: './cycle-export-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleExportDialogComponent {
    protected readonly data = inject<CycleExportDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<CycleExportDialogComponent, CycleExportSelection | null>>(FdUiDialogRef);
    protected readonly attempted = signal(false);
    protected readonly model = signal({ dateFrom: this.data.dateFrom, dateTo: this.data.dateTo, currentPassword: '' });
    protected readonly exportForm = form(this.model, path => {
        required(path.dateFrom);
        required(path.dateTo);
        required(path.currentPassword, { when: () => this.data.sensitive });
    });
    protected readonly rangeError = computed(() => cycleExportRangeError(this.model(), this.data.today));
    protected readonly passwordError = computed(() =>
        this.attempted() && this.exportForm.currentPassword().invalid() ? 'FORM_ERRORS.REQUIRED' : null,
    );

    protected submit(event: Event): void {
        event.preventDefault();
        this.attempted.set(true);
        this.exportForm().markAsTouched();
        if (this.exportForm().invalid() || this.rangeError() !== null) {
            return;
        }
        const { dateFrom, dateTo, currentPassword } = this.model();
        this.dialogRef.close(this.data.sensitive ? { dateFrom, dateTo, currentPassword } : { dateFrom, dateTo });
    }

    protected cancel(): void {
        this.dialogRef.close(null);
    }
}
