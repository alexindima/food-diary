import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
import { type AdminEmailTemplate } from '../models/admin-email-template.data';
import { AdminEmailTemplatesComponent } from './admin-email-templates.component';

describe('AdminEmailTemplatesComponent', () => {
    let component: AdminEmailTemplatesComponent;
    let fixture: ComponentFixture<AdminEmailTemplatesComponent>;
    let templatesService: { getAll: ReturnType<typeof vi.fn> };
    let dialogService: { open: ReturnType<typeof vi.fn> };

    const templates: AdminEmailTemplate[] = [
        {
            id: 't1',
            key: 'email_verification',
            locale: 'en',
            subject: 'Verify email',
            htmlBody: '<p>Hello</p>',
            textBody: 'Hello',
            isActive: true,
            createdOnUtc: '2026-01-01T00:00:00Z',
            updatedOnUtc: null,
        },
    ];

    beforeEach(async () => {
        templatesService = { getAll: vi.fn() };
        dialogService = { open: vi.fn() };

        templatesService.getAll.mockReturnValue(of(templates));
        dialogService.open.mockReturnValue({
            afterClosed: () => of(false),
        });

        await TestBed.configureTestingModule({
            imports: [AdminEmailTemplatesComponent],
            providers: [
                { provide: AdminEmailTemplatesService, useValue: templatesService },
                { provide: FdUiDialogService, useValue: dialogService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load templates on init', () => {
        expect(templatesService.getAll).toHaveBeenCalledTimes(1);
        expect(component.templates()).toEqual(templates);
        expect(component.isLoading()).toBe(false);
    });

    it('should reload templates after successful edit dialog close', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component.openEdit(templates[0]);
        close$.next(true);
        close$.complete();

        expect(dialogService.open).toHaveBeenCalled();
        expect(templatesService.getAll).toHaveBeenCalledTimes(2);
    });

    it('should open create dialog and reload after success', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component.openCreate();
        close$.next(true);
        close$.complete();

        expect(dialogService.open).toHaveBeenCalled();
        expect(templatesService.getAll).toHaveBeenCalledTimes(2);
    });
});
