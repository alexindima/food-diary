import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';

type TemplateFormModel = {
    key: string;
    locale: string;
    subject: string;
    htmlBody: string;
    textBody: string;
    isActive: boolean;
};

type TestEmailFormModel = {
    email: string;
};

@Component({
    selector: 'fd-admin-email-template-edit-dialog',
    imports: [
        FormField,
        FormRoot,
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
    private readonly templatesFacade = inject(AdminEmailTemplatesFacade);
    private readonly sanitizer = inject(DomSanitizer);

    protected readonly isNew = (this.data as AdminEmailTemplate & { isNew?: boolean }).isNew === true;
    protected readonly isSaving = signal(false);
    protected readonly isSendingTest = signal(false);
    protected readonly testSendStatus = signal<'idle' | 'sent' | 'failed'>('idle');
    protected readonly previewMode = signal<'html' | 'text'>('html');
    protected readonly previewBrand = signal('FoodDiary');
    protected readonly previewClientName = signal('Alex Johnson');
    protected readonly previewLink = signal(this.getDefaultPreviewLink(this.data.key));
    protected readonly testEmailModel = signal<TestEmailFormModel>({ email: '' });
    protected readonly testEmailForm = form(this.testEmailModel, path => {
        required(path.email);
        email(path.email);
    });
    protected readonly formModel = signal<TemplateFormModel>({
        key: this.data.key,
        locale: this.data.locale,
        subject: this.data.subject,
        htmlBody: this.data.htmlBody,
        textBody: this.data.textBody,
        isActive: this.data.isActive,
    });
    private readonly submitTemplateFormAsync = async (): Promise<void> => {
        this.onSave();
        await Promise.resolve();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.key);
            required(path.locale);
            required(path.subject);
            required(path.htmlBody);
            required(path.textBody);
            disabled(path.key, { when: () => !this.isNew });
            disabled(path.locale, { when: () => !this.isNew });
        },
        {
            submission: {
                action: this.submitTemplateFormAsync,
            },
        },
    );
    protected readonly previewHtml = computed<SafeHtml>(() => {
        const { htmlBody, subject } = this.formModel();
        const html = this.applyTokens(
            htmlBody !== '' ? htmlBody : `<div style="font-family:Segoe UI,Arial,sans-serif;">${subject}</div>`,
            this.previewLink(),
            this.previewBrand(),
            this.previewClientName(),
        );

        return this.sanitizer.bypassSecurityTrustHtml(html);
    });
    protected readonly previewText = computed(() => {
        const { subject, textBody } = this.formModel();
        return this.applyTokens(textBody !== '' ? textBody : subject, this.previewLink(), this.previewBrand(), this.previewClientName());
    });

    protected onCancel(): void {
        this.dialogRef.close(false);
    }

    protected onSave(): void {
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        const value = this.formModel();
        const key = value.key.trim();
        const locale = value.locale.trim();

        this.templatesFacade
            .upsert(key, locale, {
                subject: value.subject,
                htmlBody: value.htmlBody,
                textBody: value.textBody,
                isActive: value.isActive,
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
        if (this.form().invalid() || this.testEmailForm().invalid() || this.isSendingTest()) {
            this.form().markAsTouched();
            this.testEmailForm().markAsTouched();
            return;
        }

        this.isSendingTest.set(true);
        const value = this.formModel();
        this.templatesFacade
            .sendTest({
                toEmail: this.testEmailModel().email.trim(),
                key: value.key.trim(),
                subject: value.subject,
                htmlBody: value.htmlBody,
                textBody: value.textBody,
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
