import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

type SensitiveCycleExportFormModel = { currentPassword: string };

@Component({
    selector: 'fd-sensitive-cycle-export-dialog',
    imports: [FormField, FormRoot, TranslatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiInputComponent],
    templateUrl: './sensitive-cycle-export-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SensitiveCycleExportDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<SensitiveCycleExportDialogComponent, string | null>);
    private readonly translateService = inject(TranslateService);
    protected readonly model = signal<SensitiveCycleExportFormModel>({ currentPassword: '' });
    protected readonly exportForm = form(this.model, path => {
        required(path.currentPassword);
    });

    protected readonly passwordError = computed(() => {
        const field = this.exportForm.currentPassword();
        return field.touched() && field.invalid() ? this.translateService.instant('FORM_ERRORS.REQUIRED') : null;
    });

    protected submit(): void {
        this.exportForm().markAsTouched();
        if (this.exportForm().invalid()) {
            return;
        }

        this.dialogRef.close(this.model().currentPassword);
    }

    protected cancel(): void {
        this.dialogRef.close(null);
    }
}
