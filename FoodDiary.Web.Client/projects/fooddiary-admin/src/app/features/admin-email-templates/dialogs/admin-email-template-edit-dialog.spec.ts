import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminTemplateRevision } from '../../admin-template-history/contracts/history';
import { AdminTemplateHistoryFacade } from '../../admin-template-history/lib/admin-template-history.facade';
import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import { AdminEmailTemplateEditDialogComponent } from './admin-email-template-edit-dialog';

const TEMPLATE = {
    id: 't1',
    key: 'email_verification',
    locale: 'en',
    subject: 'Verify {{brand}}',
    htmlBody: '<a href="{{link}}">Verify</a>',
    textBody: 'Verify {{brand}}: {{link}}',
    isActive: true,
    createdOnUtc: '2026-01-01T00:00:00Z',
    updatedOnUtc: null,
};

type EmailTemplatesServiceMock = {
    sendTest: ReturnType<typeof vi.fn>;
    upsert: ReturnType<typeof vi.fn>;
};
type EmailTemplateDialogContext = {
    component: AdminEmailTemplateEditDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<AdminEmailTemplateEditDialogComponent>;
    service: EmailTemplatesServiceMock;
};

async function setupEmailTemplateDialogAsync(
    template = TEMPLATE,
    revisions: AdminTemplateRevision[] = [],
): Promise<EmailTemplateDialogContext> {
    const service = {
        upsert: vi.fn().mockReturnValue(of(TEMPLATE)),
        sendTest: vi.fn().mockReturnValue(of(void 0)),
    };
    const dialogRef = { close: vi.fn() };

    await TestBed.configureTestingModule({
        imports: [AdminEmailTemplateEditDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminEmailTemplatesFacade, useValue: service },
            {
                provide: AdminTemplateHistoryFacade,
                useValue: { getRevisions: (): ReturnType<AdminTemplateHistoryFacade['getRevisions']> => of(revisions) },
            },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: template },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminEmailTemplateEditDialogComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, dialogRef, fixture, service };
}

function emailField(root: HTMLElement, caption: string): HTMLInputElement | HTMLTextAreaElement {
    const label = [...root.querySelectorAll('label')].find(candidate => candidate.textContent.trim().startsWith(caption));
    const field = label?.control;
    if (!(field instanceof HTMLInputElement) && !(field instanceof HTMLTextAreaElement)) {
        throw new Error(`Expected email template field ${caption}.`);
    }
    return field;
}

function setEmailField(root: HTMLElement, caption: string, value: string): void {
    const field = emailField(root, caption);
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
}

function emailButton(root: ParentNode, text: string): HTMLButtonElement {
    const button = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === text);
    if (button === undefined) {
        throw new Error(`Expected email template button ${text}.`);
    }
    return button;
}

describe('Email template preview fallback', () => {
    it('shows a plain subject in both preview modes when both bodies are empty', async () => {
        const { fixture, service } = await setupEmailTemplateDialogAsync();
        const root = fixture.nativeElement as HTMLElement;
        setEmailField(root, 'Subject', 'Compare <strong>Plans</strong> & {{BRAND}} {{unsupported}}');
        setEmailField(root, 'HTML body', '');
        setEmailField(root, 'Text body', '');
        fixture.detectChanges();
        const expected = 'Compare <strong>Plans</strong> & FoodDiary {{unsupported}}';
        const htmlPreview = root.querySelector('.preview--html');
        expect(htmlPreview?.textContent).toContain(expected);
        expect(htmlPreview?.querySelector('strong')).toBeNull();
        emailButton(root, 'Text').click();
        fixture.detectChanges();
        expect(root.querySelector('.preview--text')?.textContent).toBe(expected);
        expect(emailButton(root, 'Save').disabled).toBe(true);
        expect(service.upsert).not.toHaveBeenCalled();
    });
});

describe('Email template preview links', () => {
    it.each([
        { key: 'subscription_cancellation', link: 'https://fooddiary.club/premium' },
        { key: 'account_created', link: 'https://fooddiary.club/login' },
        { key: 'password_reset', link: 'https://fooddiary.club/reset-password?userId=demo&token=demo' },
        { key: 'dietologist_invitation', link: 'https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo' },
    ])('uses the appropriate example link for $key', async ({ key, link }) => {
        const { fixture } = await setupEmailTemplateDialogAsync({ ...TEMPLATE, key });
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('.preview--html a')?.getAttribute('href')).toBe(link);
        expect(root.querySelector('.token-list')?.textContent).toContain('{{link}}');
    });
});

it('requires a valid recipient before sending an edited test email', async () => {
    const { fixture, service } = await setupEmailTemplateDialogAsync();
    const root = fixture.nativeElement as HTMLElement;
    setEmailField(root, 'Send test to', 'editor@example.test');
    fixture.detectChanges();
    const send = emailButton(root, 'Send test');
    expect(send.disabled).toBe(false);
    setEmailField(root, 'Send test to', 'invalid address');
    send.click();
    fixture.detectChanges();
    expect(send.disabled).toBe(true);
    expect(service.sendTest).not.toHaveBeenCalled();
    expect(root.querySelector('.test-send [role="alert"]')?.textContent).toContain('FORM_ERRORS.EMAIL');

    setEmailField(root, 'Subject', 'Current draft');
    setEmailField(root, 'Send test to', 'editor@example.test');
    fixture.detectChanges();
    send.click();
    fixture.detectChanges();
    expect(service.sendTest).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ toEmail: 'editor@example.test', subject: 'Current draft' }),
    );
    expect(root.querySelector('.test-send__status--sent')?.textContent).toContain('Test email sent.');
});

describe('Email template revision restoration', () => {
    it.each([true, false])('restores a revision into the draft with both bodies present: %s', async hasBodies => {
        const subject = hasBodies ? 'Archived invitation' : null;
        const htmlBody = hasBodies ? '<p>Archived copy</p>' : null;
        const revision: AdminTemplateRevision = {
            id: adminId<'template-revision'>('revision-1'),
            subject,
            htmlBody,
            textBody: 'Archived copy {{brand}}',
            isActive: false,
            version: 1,
            savedOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
            archivedOnUtc: adminUtcInstant('2026-01-02T00:00:00Z'),
        };
        const { fixture, service, dialogRef } = await setupEmailTemplateDialogAsync(TEMPLATE, [revision]);
        const root = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
        fixture.detectChanges();
        root.querySelector<HTMLElement>('fd-admin-template-history details > summary')?.click();
        emailButton(root, 'ADMIN_TEMPLATE_HISTORY.RESTORE').click();
        fixture.detectChanges();
        expect(emailField(root, 'Subject').value).toBe(subject ?? '');
        expect(emailField(root, 'HTML body').value).toBe(htmlBody ?? '');
        expect(emailField(root, 'Text body').value).toBe(revision.textBody);
        expect(service.upsert).not.toHaveBeenCalled();

        const save = emailButton(root, 'Save');
        expect(save.disabled).toBe(!hasBodies);
        save.click();
        await fixture.whenStable();
        if (hasBodies) {
            expect(service.upsert).toHaveBeenCalledExactlyOnceWith(TEMPLATE.key, TEMPLATE.locale, {
                subject,
                htmlBody,
                textBody: revision.textBody,
                isActive: false,
            });
            expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
        } else {
            expect(service.upsert).not.toHaveBeenCalled();
            expect(dialogRef.close).not.toHaveBeenCalled();
        }
    });
});

describe('AdminEmailTemplateEditDialogComponent', () => {
    it('should create', async () => {
        const { component } = await setupEmailTemplateDialogAsync();

        expect(component).toBeTruthy();
    });

    it('should disable key and locale controls for existing template', async () => {
        const { component } = await setupEmailTemplateDialogAsync();

        expect(component['form'].key().disabled()).toBe(true);
        expect(component['form'].locale().disabled()).toBe(true);
    });
});

describe('AdminEmailTemplateEditDialogComponent preview', () => {
    it('should update preview when form changes', async () => {
        const { component } = await setupEmailTemplateDialogAsync();
        component['formModel'].update(value => ({ ...value, subject: 'Hello {{brand}}', textBody: 'Open {{link}}' }));

        expect(component['previewText']()).toContain('https://fooddiary.club/verify-email');
        expect(component['previewBrand']()).toBe('FoodDiary');
    });

    it('should switch preview mode', async () => {
        const { component } = await setupEmailTemplateDialogAsync();
        component['setPreviewMode']('text');
        expect(component['previewMode']()).toBe('text');
    });

    it('should sanitize unsafe html while preserving safe preview markup', async () => {
        const { component, fixture } = await setupEmailTemplateDialogAsync();
        component['formModel'].update(value => ({
            ...value,
            htmlBody: '<strong>Safe</strong><img src="x" onerror="alert(1)"><script>alert(2)</script>',
        }));
        fixture.detectChanges();

        const preview = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.preview--html');
        expect(preview?.querySelector('strong')?.textContent).toBe('Safe');
        expect(preview?.querySelector('img')?.hasAttribute('onerror')).toBe(false);
        expect(preview?.querySelector('script')).toBeNull();
    });
});

describe('AdminEmailTemplateEditDialogComponent saving', () => {
    it('should close false on cancel', async () => {
        const { component, dialogRef } = await setupEmailTemplateDialogAsync();
        component['onCancel']();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should save and close true on success', async () => {
        const { component, dialogRef, service } = await setupEmailTemplateDialogAsync();
        component['formModel'].update(value => ({ ...value, subject: 'Updated subject' }));
        component['onSave']();

        expect(service.upsert).toHaveBeenCalledWith('email_verification', 'en', {
            subject: 'Updated subject',
            htmlBody: '<a href="{{link}}">Verify</a>',
            textBody: 'Verify {{brand}}: {{link}}',
            isActive: true,
        });
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(true);
        });
    });

    it('should prevent native form submit when saving', async () => {
        const { fixture, service } = await setupEmailTemplateDialogAsync();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(service.upsert).toHaveBeenCalledOnce();
    });

    it('shows a save error, preserves the draft and allows retry', async () => {
        const { component, service, fixture, dialogRef } = await setupEmailTemplateDialogAsync();
        service.upsert.mockReturnValueOnce(throwError(() => new Error('save failed')));
        component['formModel'].update(value => ({ ...value, subject: 'Unsaved draft' }));

        component['onSave']();

        await vi.waitFor(() => {
            expect(component['isSaving']()).toBe(false);
        });
        fixture.detectChanges();
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_TEMPLATES.SAVE_ERROR');
        expect(component['formModel']().subject).toBe('Unsaved draft');

        component['onSave']();
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(true);
        });
        expect(component['saveFailed']()).toBe(false);
        expect(service.upsert).toHaveBeenLastCalledWith('email_verification', 'en', expect.objectContaining({ subject: 'Unsaved draft' }));
    });
});

describe('AdminEmailTemplateEditDialogComponent test send', () => {
    it('should send current form values as a test email', async () => {
        const { component, service } = await setupEmailTemplateDialogAsync();
        component['testEmailModel'].set({ email: 'admin@example.com' });
        component['formModel'].update(value => ({ ...value, subject: 'Updated subject' }));

        component['onSendTest']();

        expect(service.sendTest).toHaveBeenCalledWith({
            toEmail: 'admin@example.com',
            key: 'email_verification',
            subject: 'Updated subject',
            htmlBody: '<a href="{{link}}">Verify</a>',
            textBody: 'Verify {{brand}}: {{link}}',
        });
        expect(component['testSendStatus']()).toBe('sent');
    });

    it('should show failed status when test email send fails', async () => {
        const { component, service } = await setupEmailTemplateDialogAsync();
        service.sendTest.mockReturnValueOnce(throwError(() => new Error('send failed')));
        component['testEmailModel'].set({ email: 'admin@example.com' });

        component['onSendTest']();

        expect(component['isSendingTest']()).toBe(false);
        expect(component['testSendStatus']()).toBe('failed');
    });
});

describe('AdminEmailTemplateEditDialogComponent pending saves', () => {
    it('locks draft controls and dismissal while a save is pending, then preserves the draft for retry', async () => {
        const { component, dialogRef, fixture, service } = await setupEmailTemplateDialogAsync();
        const pending = new Subject<typeof TEMPLATE>();
        service.upsert.mockReturnValueOnce(pending);
        component['testEmailModel'].set({ email: 'test@example.com' });
        component['formModel'].update(value => ({ ...value, subject: 'Pending subject' }));
        component['onSave']();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        for (const control of element.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>('input, textarea')) {
            expect(control.disabled).toBe(true);
        }
        const cancel = Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
            button => button.textContent.trim() === 'Cancel',
        );
        expect(cancel?.disabled).toBe(true);
        expect(element.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.disabled).toBe(true);
        component['onCancel']();
        component['onSendTest']();
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(service.sendTest).not.toHaveBeenCalled();

        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(component['isSaving']()).toBe(false);
            expect(component['saveFailed']()).toBe(true);
        });
        expect(element.querySelector<HTMLTextAreaElement>('textarea')?.disabled).toBe(false);
        expect(component['formModel']().subject).toBe('Pending subject');
        component['onSave']();
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(true);
        });
        expect(service.upsert).toHaveBeenCalledTimes(2);
        expect(service.upsert).toHaveBeenLastCalledWith(
            'email_verification',
            'en',
            expect.objectContaining({ subject: 'Pending subject' }),
        );
    });
});
