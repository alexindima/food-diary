import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';
import { AdminEmailTemplatesComponent } from './admin-email-templates';

const templates: AdminEmailTemplate[] = [
    {
        id: adminId<'email-template'>('t1'),
        key: 'email_verification',
        locale: 'en',
        subject: 'Verify email',
        htmlBody: '<p>Hello</p>',
        textBody: 'Hello',
        isActive: true,
        createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
        updatedOnUtc: null,
    },
];

describe('AdminEmailTemplatesComponent', () => {
    let component: AdminEmailTemplatesComponent;
    let fixture: ComponentFixture<AdminEmailTemplatesComponent>;
    let templatesService: { getAll: ReturnType<typeof vi.fn> };
    let dialogService: { open: ReturnType<typeof vi.fn> };

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
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminEmailTemplatesFacade, useValue: templatesService },
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

    it('pages the list and clamps the last page after records are removed', () => {
        component['templates'].set(
            Array.from({ length: 21 }, (_, index) => ({ ...templates[0], id: adminId<'email-template'>(`t${index}`) })),
        );
        expect(component['pageItems']()).toHaveLength(component['pageSize']);
        component['requestedPage'].set(1);
        expect(component['pageItems']().map(item => item.id)).toEqual(['t20']);
        component['templates'].set(templates);
        expect(component['pageIndex']()).toBe(0);
        expect(component['pageItems']()).toEqual(templates);
    });

    it('should load templates on init', () => {
        expect(templatesService.getAll).toHaveBeenCalledTimes(1);
        expect(component['templates']()).toEqual(templates);
        expect(component['isLoading']()).toBe(false);
    });

    it('should reload templates after successful edit dialog close', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component['openEdit'](templates[0]);
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

        component['openCreate']();
        close$.next(true);
        close$.complete();

        const serializedDialogConfig = JSON.stringify(dialogService.open.mock.calls[0]?.[1]);
        expect(serializedDialogConfig).toContain('"key":""');
        expect(serializedDialogConfig).toContain('"locale":""');
        expect(serializedDialogConfig).toContain('"isNew":true');
        expect(templatesService.getAll).toHaveBeenCalledTimes(2);
    });
});

describe('AdminEmailTemplatesComponent rendered loading and recovery', () => {
    const getAll = vi.fn<AdminEmailTemplatesFacade['getAll']>();
    const closed = vi.fn<() => Observable<boolean>>();

    beforeEach(async () => {
        vi.resetAllMocks();
        closed.mockReturnValue(of(false));
        await TestBed.configureTestingModule({
            imports: [AdminEmailTemplatesComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminEmailTemplatesFacade, useValue: { getAll } },
                { provide: FdUiDialogService, useValue: { open: (): { afterClosed: typeof closed } => ({ afterClosed: closed }) } },
            ],
        }).compileComponents();
    });

    it('keeps the newly created template visible when the original load later fails', () => {
        const oldRequest = new Subject<AdminEmailTemplate[]>();
        const created = { ...templates[0], subject: 'New verification message' };
        const saved = new Subject<boolean>();
        getAll.mockReturnValueOnce(oldRequest).mockReturnValueOnce(of([created]));
        closed.mockReturnValueOnce(saved);
        const fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.textContent).toContain('Loading templates...');
        const createButton = Array.from(element.querySelectorAll('button')).find(button => button.textContent.trim() === 'Create template');
        if (createButton === undefined) {
            throw new Error('Missing create template button');
        }
        createButton.click();
        saved.next(true);
        saved.complete();
        fixture.detectChanges();
        expect(element.querySelector('tbody')?.textContent).toContain(created.subject);

        oldRequest.error(new Error('Obsolete request failed'));
        fixture.detectChanges();
        expect(element.querySelector('fd-admin-load-error')).toBeNull();
        expect(element.querySelector('tbody')?.textContent).toContain(created.subject);
        expect(getAll).toHaveBeenCalledTimes(2);
    });

    it('retries a current list failure from the rendered error action', () => {
        getAll.mockReturnValueOnce(throwError(() => new Error('Offline'))).mockReturnValueOnce(of(templates));
        const fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const retry = element.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        if (retry === null) {
            throw new Error('Missing retry button for failed template load');
        }
        retry.click();
        fixture.detectChanges();
        expect(element.querySelector('fd-admin-load-error')).toBeNull();
        expect(element.querySelector('tbody')?.textContent).toContain(templates[0].subject);
        expect(getAll).toHaveBeenCalledTimes(2);
    });
});
