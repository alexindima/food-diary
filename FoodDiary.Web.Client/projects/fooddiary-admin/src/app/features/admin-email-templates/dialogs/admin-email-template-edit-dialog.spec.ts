import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminTemplateRevision } from '../../admin-template-history/contracts/history';
import { AdminTemplateHistoryFacade } from '../../admin-template-history/lib/admin-template-history.facade';
import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
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
    template: typeof TEMPLATE & { isNew?: boolean } = TEMPLATE,
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
            { provide: AdminEmailTemplatesService, useValue: service },
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

it.each(['constructor', '__proto__'])('allows editing a new template with an unrecognized key %s', async key => {
    const { fixture, service } = await setupEmailTemplateDialogAsync({ ...TEMPLATE, key: '', isNew: true });
    const root = fixture.nativeElement as HTMLElement;
    setEmailField(root, 'Key', key);
    fixture.detectChanges();

    expect(root.querySelector('.preview--html')?.textContent).toContain('Verify');
    expect(root.querySelectorAll('.token-list code')).toHaveLength(0);
    expect(emailButton(root, 'Save').disabled).toBe(false);
    expect(service.upsert).not.toHaveBeenCalled();
    setEmailField(root, 'Key', 'password_reset');
    fixture.detectChanges();

    expect(root.querySelector('.token-list')?.textContent).toContain('{{link}}');
    expect(root.querySelector('.preview--html a')?.getAttribute('href')).toContain('/reset-password');
});

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
        const { fixture, dialogRef } = await setupEmailTemplateDialogAsync();
        emailButton(fixture.nativeElement as HTMLElement, 'Cancel').click();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should save and close true on success', async () => {
        const { fixture, dialogRef, service } = await setupEmailTemplateDialogAsync();
        const root = fixture.nativeElement as HTMLElement;
        setEmailField(root, 'Subject', 'Updated subject');
        fixture.detectChanges();
        emailButton(root, 'Save').click();
        await fixture.whenStable();

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

    it('blocks invalid and duplicate native submits at the form boundary', async () => {
        const { fixture, service, dialogRef } = await setupEmailTemplateDialogAsync();
        const pending = new Subject<typeof TEMPLATE>();
        service.upsert.mockReturnValueOnce(pending);
        const root = fixture.nativeElement as HTMLElement;
        const form = root.querySelector('form');
        expect(form).not.toBeNull();

        setEmailField(root, 'Subject', '');
        fixture.detectChanges();
        const invalidSubmit = new Event('submit', { bubbles: true, cancelable: true });
        form?.dispatchEvent(invalidSubmit);
        await fixture.whenStable();
        expect(invalidSubmit.defaultPrevented).toBe(true);
        expect(service.upsert).not.toHaveBeenCalled();

        setEmailField(root, 'Subject', 'Ready to save');
        fixture.detectChanges();
        form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        expect(service.upsert).toHaveBeenCalledOnce();
        expect(dialogRef.close).not.toHaveBeenCalled();
        pending.next(TEMPLATE);
        await fixture.whenStable();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
    });

    it('shows a save error, preserves the draft and allows retry', async () => {
        const { service, fixture, dialogRef } = await setupEmailTemplateDialogAsync();
        const root = fixture.nativeElement as HTMLElement;
        service.upsert.mockReturnValueOnce(throwError(() => new Error('save failed')));
        setEmailField(root, 'Subject', 'Unsaved draft');
        fixture.detectChanges();
        emailButton(root, 'Save').click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_TEMPLATES.SAVE_ERROR');
        expect(emailField(root, 'Subject').value).toBe('Unsaved draft');

        emailButton(root, 'Save').click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(dialogRef.close).toHaveBeenCalledWith(true);
        expect(root.querySelector('[role="alert"]')).toBeNull();
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
        const revision: AdminTemplateRevision = {
            id: adminId<'template-revision'>('older-draft'),
            subject: 'Archived subject',
            htmlBody: '<p>Archived HTML</p>',
            textBody: 'Archived body',
            isActive: false,
            version: 1,
            savedOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
            archivedOnUtc: adminUtcInstant('2026-01-02T00:00:00Z'),
        };
        const { dialogRef, fixture, service } = await setupEmailTemplateDialogAsync(TEMPLATE, [revision]);
        const pending = new Subject<typeof TEMPLATE>();
        service.upsert.mockReturnValueOnce(pending);
        const element = fixture.nativeElement as HTMLElement;
        await fixture.whenStable();
        element.querySelector<HTMLElement>('fd-admin-template-history details > summary')?.click();
        setEmailField(element, 'Send test to', 'test@example.com');
        setEmailField(element, 'Subject', 'Pending subject');
        fixture.detectChanges();
        const save = emailButton(element, 'Save');
        const cancel = emailButton(element, 'Cancel');
        const restore = emailButton(element, 'ADMIN_TEMPLATE_HISTORY.RESTORE');
        const send = emailButton(element, 'Send test');

        save.click();
        restore.click();
        cancel.click();
        send.click();
        fixture.detectChanges();

        for (const control of element.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>('input, textarea')) {
            expect(control.disabled).toBe(true);
        }
        expect(cancel.disabled).toBe(true);
        expect(element.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.disabled).toBe(true);
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(service.sendTest).not.toHaveBeenCalled();

        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_TEMPLATES.SAVE_ERROR');
            expect(save.disabled).toBe(false);
        });
        expect(element.querySelector<HTMLTextAreaElement>('textarea')?.disabled).toBe(false);
        expect(emailField(element, 'Subject').value).toBe('Pending subject');
        save.click();
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
