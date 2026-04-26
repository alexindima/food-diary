import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { AdminEmailTemplateEditDialogComponent } from './admin-email-template-edit-dialog.component';
import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';

describe('AdminEmailTemplateEditDialogComponent', () => {
    let component: AdminEmailTemplateEditDialogComponent;
    let fixture: ComponentFixture<AdminEmailTemplateEditDialogComponent>;
    let service: { upsert: ReturnType<typeof vi.fn>; sendTest: ReturnType<typeof vi.fn> };
    let dialogRef: { close: ReturnType<typeof vi.fn> };

    const template = {
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

    beforeEach(async () => {
        service = { upsert: vi.fn(), sendTest: vi.fn() };
        dialogRef = { close: vi.fn() };
        service.upsert.mockReturnValue(of(template));
        service.sendTest.mockReturnValue(of(undefined));

        await TestBed.configureTestingModule({
            imports: [AdminEmailTemplateEditDialogComponent],
            providers: [
                { provide: AdminEmailTemplatesService, useValue: service },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: template },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminEmailTemplateEditDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should disable key and locale controls for existing template', () => {
        expect(component.form.controls.key.disabled).toBe(true);
        expect(component.form.controls.locale.disabled).toBe(true);
    });

    it('should update preview when form changes', () => {
        component.form.controls.subject.setValue('Hello {{brand}}');
        component.form.controls.textBody.setValue('Open {{link}}');

        expect(component.previewText()).toContain('https://fooddiary.club/verify-email');
        expect(component.previewBrand()).toBe('FoodDiary');
    });

    it('should switch preview mode', () => {
        component.setPreviewMode('text');
        expect(component.previewMode()).toBe('text');
    });

    it('should close false on cancel', () => {
        component.onCancel();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should save and close true on success', () => {
        component.form.controls.subject.setValue('Updated subject');
        component.onSave();

        expect(service.upsert).toHaveBeenCalledWith('email_verification', 'en', {
            subject: 'Updated subject',
            htmlBody: '<a href="{{link}}">Verify</a>',
            textBody: 'Verify {{brand}}: {{link}}',
            isActive: true,
        });
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should stop saving on error without closing true', () => {
        service.upsert.mockReturnValueOnce(throwError(() => new Error('save failed')));

        component.onSave();

        expect(component.isSaving()).toBe(false);
    });

    it('should send current form values as a test email', () => {
        component.testEmailControl.setValue('admin@example.com');
        component.form.controls.subject.setValue('Updated subject');

        component.onSendTest();

        expect(service.sendTest).toHaveBeenCalledWith({
            toEmail: 'admin@example.com',
            key: 'email_verification',
            subject: 'Updated subject',
            htmlBody: '<a href="{{link}}">Verify</a>',
            textBody: 'Verify {{brand}}: {{link}}',
        });
        expect(component.testSendStatus()).toBe('sent');
    });

    it('should show failed status when test email send fails', () => {
        service.sendTest.mockReturnValueOnce(throwError(() => new Error('send failed')));
        component.testEmailControl.setValue('admin@example.com');

        component.onSendTest();

        expect(component.isSendingTest()).toBe(false);
        expect(component.testSendStatus()).toBe('failed');
    });
});
