import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { firstValueFrom } from 'rxjs';

import { AdminTemplateHistoryComponent } from '../../admin-template-history/components/admin-template-history';
import type { AdminTemplateRevision } from '../../admin-template-history/models/admin-template-revision';
import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import { emailTemplateVariables } from '../lib/email-template-variables';
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
        AdminTemplateHistoryComponent,
        TranslatePipe,
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

    protected readonly isNew = (this.data as AdminEmailTemplate & { isNew?: boolean }).isNew === true;
    protected readonly isSaving = signal(false);
    protected readonly isSendingTest = signal(false);
    protected readonly testSendStatus = signal<'idle' | 'sent' | 'failed'>('idle');
    protected readonly previewMode = signal<'html' | 'text'>('html');
    protected readonly previewBrand = signal('FoodDiary');
    protected readonly previewClientName = signal('Alex Johnson');
    protected readonly previewLink = computed(() => this.getDefaultPreviewLink(this.formModel().key));
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
        await this.saveAsync();
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
    protected readonly previewHtml = computed(() => {
        const { htmlBody, subject } = this.formModel();
        const html = this.applyTokens(
            htmlBody !== '' ? htmlBody : `<div style="font-family:Segoe UI,Arial,sans-serif;">${subject}</div>`,
            this.previewLink(),
            this.previewBrand(),
            this.previewClientName(),
        );

        return html;
    });
    protected readonly previewText = computed(() => {
        const { subject, textBody } = this.formModel();
        return this.applyTokens(textBody !== '' ? textBody : subject, this.previewLink(), this.previewBrand(), this.previewClientName());
    });
    protected readonly supportedVariables = computed(() => emailTemplateVariables(this.formModel().key));

    protected onCancel(): void {
        this.dialogRef.close(false);
    }

    protected restoreRevision(revision: AdminTemplateRevision): void {
        this.formModel.update(value => ({
            ...value,
            subject: revision.subject ?? '',
            htmlBody: revision.htmlBody ?? '',
            textBody: revision.textBody,
            isActive: revision.isActive,
        }));
    }

    protected onSave(): void {
        void this.saveAsync();
    }

    private async saveAsync(): Promise<void> {
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        const value = this.formModel();
        const key = value.key.trim();
        const locale = value.locale.trim();

        try {
            await firstValueFrom(
                this.templatesFacade.upsert(key, locale, {
                    subject: value.subject,
                    htmlBody: value.htmlBody,
                    textBody: value.textBody,
                    isActive: value.isActive,
                }),
            );
            this.isSaving.set(false);
            this.dialogRef.close(true);
        } catch {
            this.isSaving.set(false);
        }
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
        const samples: Record<string, string> = {
            link,
            brand,
            clientname: clientName,
            email: 'example@example.com',
            temporarypassword: 'Demo-only-password',
            loginlink: 'https://fooddiary.club/login',
        };
        const supported = new Set(this.supportedVariables().map(name => name.toLowerCase()));
        return value.replaceAll(/{{([a-z]+)}}/gi, (match: string, name: string) =>
            supported.has(name.toLowerCase()) ? samples[name.toLowerCase()] : match,
        );
    }

    private getDefaultPreviewLink(key: string): string {
        if (key === 'account_created') {
            return 'https://fooddiary.club/login';
        }
        if (key === 'password_reset') {
            return 'https://fooddiary.club/reset-password?userId=demo&token=demo';
        }
        return key === 'dietologist_invitation'
            ? 'https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo'
            : 'https://fooddiary.club/verify-email?userId=demo&token=demo';
    }
}
