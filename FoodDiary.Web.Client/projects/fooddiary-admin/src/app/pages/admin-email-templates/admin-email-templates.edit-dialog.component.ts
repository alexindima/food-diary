import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { AdminEmailTemplatesService, AdminEmailTemplate } from '../../services/admin-email-templates.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  private readonly destroyRef = inject(DestroyRef);

  public readonly isNew = (this.data as AdminEmailTemplate & { isNew?: boolean }).isNew === true;
  public readonly isSaving = signal(false);
  public readonly previewMode = signal<'html' | 'text'>('html');
  public readonly previewHtml = signal('');
  public readonly previewText = signal('');
  public readonly previewBrand = signal('FoodDiary');
  public readonly previewLink = signal('https://fooddiary.club/verify-email?userId=demo&token=demo');
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

    this.updatePreview();
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.updatePreview());
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

  public setPreviewMode(mode: 'html' | 'text'): void {
    this.previewMode.set(mode);
  }

  private updatePreview(): void {
    const subject = this.form.controls.subject.value ?? '';
    const htmlBody = this.form.controls.htmlBody.value ?? '';
    const textBody = this.form.controls.textBody.value ?? '';
    const brand = this.previewBrand();
    const link = this.previewLink();

    this.previewHtml.set(this.applyTokens(htmlBody || `<div style="font-family:Segoe UI,Arial,sans-serif;">${subject}</div>`, link, brand));
    this.previewText.set(this.applyTokens(textBody || subject, link, brand));
  }

  private applyTokens(value: string, link: string, brand: string): string {
    return value
      .replace(/{{\s*link\s*}}/gi, link)
      .replace(/{{\s*brand\s*}}/gi, brand);
  }
}
