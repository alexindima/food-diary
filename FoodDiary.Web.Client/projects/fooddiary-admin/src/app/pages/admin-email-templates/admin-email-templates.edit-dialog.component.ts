import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { AdminEmailTemplatesService, AdminEmailTemplate } from '../../services/admin-email-templates.service';

type TemplateForm = {
  key: FormControl<string>;
  locale: FormControl<string>;
  subject: FormControl<string>;
  htmlBody: FormControl<string>;
  textBody: FormControl<string>;
  isActive: FormControl<boolean>;
};

@Component({
  selector: 'fd-admin-email-template-edit-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FdUiInputComponent,
    FdUiTextareaComponent,
    FdUiCheckboxComponent,
    FdUiButtonComponent,
  ],
  templateUrl: './admin-email-templates.edit-dialog.component.html',
  styleUrl: './admin-email-templates.edit-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminEmailTemplateEditDialogComponent {
  public readonly data = inject<AdminEmailTemplate>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<AdminEmailTemplateEditDialogComponent, boolean>>(MatDialogRef);
  private readonly service = inject(AdminEmailTemplatesService);

  public readonly isNew = (this.data as AdminEmailTemplate & { isNew?: boolean }).isNew === true;
  public readonly isSaving = signal(false);
  public readonly form = new FormGroup<TemplateForm>({
    key: new FormControl(this.data.key, { nonNullable: true, validators: [Validators.required] }),
    locale: new FormControl(this.data.locale, { nonNullable: true, validators: [Validators.required] }),
    subject: new FormControl(this.data.subject, { nonNullable: true, validators: [Validators.required] }),
    htmlBody: new FormControl(this.data.htmlBody, { nonNullable: true, validators: [Validators.required] }),
    textBody: new FormControl(this.data.textBody, { nonNullable: true, validators: [Validators.required] }),
    isActive: new FormControl(this.data.isActive, { nonNullable: true }),
  });

  public constructor() {
    if (!this.isNew) {
      this.form.controls.key.disable({ emitEvent: false });
      this.form.controls.locale.disable({ emitEvent: false });
    }
  }

  public onCancel(): void {
    this.dialogRef.close(false);
  }

  public onSave(): void {
    if (this.form.invalid || this.isSaving()) {
      return;
    }
    this.isSaving.set(true);

    const key = this.form.controls.key.value.trim();
    const locale = this.form.controls.locale.value.trim();

    this.service
      .upsert(key, locale, {
        subject: this.form.controls.subject.value,
        htmlBody: this.form.controls.htmlBody.value,
        textBody: this.form.controls.textBody.value,
        isActive: this.form.controls.isActive.value,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.dialogRef.close(true);
        },
        error: () => {
          this.isSaving.set(false);
        },
      });
  }
}
