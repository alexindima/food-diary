import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

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

async function setupEmailTemplateDialogAsync(): Promise<EmailTemplateDialogContext> {
    const service = {
        upsert: vi.fn().mockReturnValue(of(TEMPLATE)),
        sendTest: vi.fn().mockReturnValue(of(void 0)),
    };
    const dialogRef = { close: vi.fn() };

    await TestBed.configureTestingModule({
        imports: [AdminEmailTemplateEditDialogComponent],
        providers: [
            { provide: AdminEmailTemplatesFacade, useValue: service },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: TEMPLATE },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminEmailTemplateEditDialogComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, dialogRef, fixture, service };
}

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
        expect(dialogRef.close).toHaveBeenCalledWith(true);
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

    it('should stop saving on error without closing true', async () => {
        const { component, service } = await setupEmailTemplateDialogAsync();
        service.upsert.mockReturnValueOnce(throwError(() => new Error('save failed')));

        component['onSave']();

        expect(component['isSaving']()).toBe(false);
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
