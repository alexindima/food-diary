import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';

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
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiCheckboxComponent,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
    ],
    templateUrl: './admin-email-template-edit-dialog.html',
    styleUrl: './admin-email-template-edit-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminEmailTemplateEditDialogComponent {
    protected readonly data = inject<AdminEmailTemplate>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<AdminEmailTemplateEditDialogComponent, boolean>>(FdUiDialogRef);
    private readonly service = inject(AdminEmailTemplatesService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly sanitizer = inject(DomSanitizer);

    protected readonly isNew = (this.data as AdminEmailTemplate & { isNew?: boolean }).isNew === true;
    protected readonly isSaving = signal(false);
    protected readonly isSendingTest = signal(false);
    protected readonly testSendStatus = signal<'idle' | 'sent' | 'failed'>('idle');
    protected readonly previewMode = signal<'html' | 'text'>('html');
    protected readonly previewHtml = signal<SafeHtml>('');
    protected readonly previewText = signal('');
    protected readonly previewBrand = signal('FoodDiary');
    protected readonly previewClientName = signal('Alex Johnson');
    protected readonly previewLink = signal(this.getDefaultPreviewLink(this.data.key));
    protected readonly testEmailControl = new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
    });
    protected readonly form = new FormGroup<TemplateForm>({
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
        this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updatePreview();
        });
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }

    protected onSave(): void {
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

    protected setPreviewMode(mode: 'html' | 'text'): void {
        this.previewMode.set(mode);
    }

    protected onSendTest(): void {
        this.testSendStatus.set('idle');
        if (this.form.invalid || this.testEmailControl.invalid || this.isSendingTest()) {
            this.form.markAllAsTouched();
            this.testEmailControl.markAsTouched();
            return;
        }

        this.isSendingTest.set(true);
        this.service
            .sendTest({
                toEmail: this.testEmailControl.value.trim(),
                key: this.form.controls.key.value.trim(),
                subject: this.form.controls.subject.value,
                htmlBody: this.form.controls.htmlBody.value,
                textBody: this.form.controls.textBody.value,
            })
            .subscribe({
                next: () => {
                    this.isSendingTest.set(false);
                    this.testSendStatus.set('sent');
                },
                error: () => {
                    this.isSendingTest.set(false);
                    this.testSendStatus.set('failed');
                },
            });
    }

    private updatePreview(): void {
        const subject = this.form.controls.subject.value;
        const htmlBody = this.form.controls.htmlBody.value;
        const textBody = this.form.controls.textBody.value;
        const brand = this.previewBrand();
        const link = this.previewLink();
        const clientName = this.previewClientName();

        const html = this.applyTokens(
            htmlBody !== '' ? htmlBody : `<div style="font-family:Segoe UI,Arial,sans-serif;">${subject}</div>`,
            link,
            brand,
            clientName,
        );
        this.previewHtml.set(this.sanitizer.bypassSecurityTrustHtml(html));
        this.previewText.set(this.applyTokens(textBody !== '' ? textBody : subject, link, brand, clientName));
    }

    private applyTokens(value: string, link: string, brand: string, clientName: string): string {
        return value
            .replaceAll(/{{\s*link\s*}}/gi, link)
            .replaceAll(/{{\s*brand\s*}}/gi, brand)
            .replaceAll(/{{\s*clientname\s*}}/gi, clientName);
    }

    private getDefaultPreviewLink(key: string): string {
        return key === 'dietologist_invitation'
            ? 'https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo'
            : 'https://fooddiary.club/verify-email?userId=demo&token=demo';
    }
}
