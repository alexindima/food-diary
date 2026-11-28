import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
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
            afterClosed: () => of(undefined),
        });

        await TestBed.configureTestingModule({
            imports: [AdminEmailTemplatesComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminEmailTemplatesService, useValue: templatesService },
                { provide: FdUiDialogService, useValue: dialogService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load templates on init', () => {
        expect(templatesService.getAll).toHaveBeenCalledTimes(1);
        expect(component['templates']()).toEqual(templates);
        expect(component['isLoading']()).toBe(false);
    });

    it('should reload templates after successful edit dialog close', () => {
        const close$ = new Subject<AdminEmailTemplate>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component['openEdit'](templates[0]);
        close$.next(templates[0]);
        close$.complete();
        fixture.detectChanges();

        expect(dialogService.open).toHaveBeenCalled();
        expect(templatesService.getAll).toHaveBeenCalledTimes(2);
    });

    it('should open create dialog and reload after success', () => {
        const close$ = new Subject<AdminEmailTemplate>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component['openCreate']();
        close$.next(templates[0]);
        close$.complete();
        fixture.detectChanges();

        const serializedDialogConfig = JSON.stringify(dialogService.open.mock.calls[0]?.[1]);
        expect(serializedDialogConfig).toContain('"key":""');
        expect(serializedDialogConfig).toContain('"locale":""');
        expect(serializedDialogConfig).toContain('"isNew":true');
        expect(templatesService.getAll).toHaveBeenCalledTimes(2);
    });
});

describe('AdminEmailTemplatesComponent rendered loading and recovery', () => {
    const getAll = vi.fn<AdminEmailTemplatesService['getAll']>();
    const closed = vi.fn<() => Observable<AdminEmailTemplate | undefined>>();

    beforeEach(async () => {
        vi.resetAllMocks();
        closed.mockReturnValue(of(undefined));
        await configureRenderedTemplatesAsync(getAll, closed);
    });

    it('keeps the newly created template visible when the original load later fails', async () => {
        const oldRequest = new Subject<AdminEmailTemplate[]>();
        const created = { ...templates[0], subject: 'New verification message' };
        const saved = new Subject<AdminEmailTemplate>();
        getAll.mockReturnValueOnce(oldRequest).mockReturnValueOnce(of([created]));
        closed.mockReturnValueOnce(saved);
        const fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.textContent).toContain('Loading templates...');
        expect(oldRequest.observed).toBe(true);
        const createButton = Array.from(element.querySelectorAll('button')).find(button => button.textContent.trim() === 'Create template');
        if (createButton === undefined) {
            throw new Error('Missing create template button');
        }
        createButton.click();
        saved.next(created);
        saved.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(element.querySelector('tbody')?.textContent).toContain(created.subject);

        expect(oldRequest.observed).toBe(false);
        oldRequest.error(new Error('Obsolete request failed'));
        fixture.detectChanges();
        expect(element.querySelector('fd-admin-load-error')).toBeNull();
        expect(element.querySelector('tbody')?.textContent).toContain(created.subject);
        expect(getAll).toHaveBeenCalledTimes(2);
    });

    it('retries a current list failure from the rendered error action', async () => {
        getAll.mockReturnValueOnce(throwError(() => new Error('Offline'))).mockReturnValueOnce(of(templates));
        const fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const retry = element.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        if (retry === null) {
            throw new Error('Missing retry button for failed template load');
        }
        retry.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(element.querySelector('fd-admin-load-error')).toBeNull();
        expect(element.querySelector('tbody')?.textContent).toContain(templates[0].subject);
        expect(getAll).toHaveBeenCalledTimes(2);
    });
});

it.each(['created', 'edited'] as const)('keeps a %s template and the known rows if refresh fails', async action => {
    const other = { ...templates[0], id: adminId<'email-template'>('other'), key: 'welcome', subject: 'Welcome' };
    const saved = { ...templates[0], subject: 'Confirmed by server' };
    const initial = action === 'created' ? [other] : [templates[0], other];
    const getAll = vi
        .fn<AdminEmailTemplatesService['getAll']>()
        .mockReturnValueOnce(of(initial))
        .mockReturnValueOnce(throwError(() => new Error('Offline')))
        .mockReturnValueOnce(of([saved, other]));
    await configureRenderedTemplatesAsync(getAll, () => of(saved));
    const fixture = TestBed.createComponent(AdminEmailTemplatesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const selector = action === 'created' ? 'header button' : 'tbody button';
    element.querySelector<HTMLButtonElement>(selector)?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const retry = element.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    expect(retry).not.toBeNull();
    expect(getAll).toHaveBeenCalledTimes(2);
    const rows = Array.from(element.querySelectorAll('tbody tr'), row => row.textContent);
    expect(rows).toHaveLength(2);
    expect(rows.join(' ')).toContain(other.subject);
    expect(rows.join(' ')).toContain(saved.subject);
    expect(rows.join(' ')).not.toContain(templates[0].subject);
    retry?.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(element.querySelector('fd-admin-load-error')).toBeNull();
    expect(element.querySelector('tbody')?.textContent).toContain(saved.subject);
});

it('filters locally and clamps the last page after a reload removes records', async () => {
    const matchingTemplates = Array.from({ length: 21 }, (_, index) => ({
        ...templates[0],
        id: adminId<'email-template'>(`template-${index}`),
        subject: `Verify template ${index}`,
    }));
    const other = { ...templates[0], id: adminId<'email-template'>('other'), subject: 'Subscription renewal' };
    const getAll = vi
        .fn<AdminEmailTemplatesService['getAll']>()
        .mockReturnValueOnce(of([...matchingTemplates, other]))
        .mockReturnValueOnce(of(templates));
    await configureRenderedTemplatesAsync(getAll, () => of(matchingTemplates[20]));
    const harness = await RouterTestingHarness.create('/email-templates?page=2');
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('tbody')?.textContent).toContain(other.subject);

    await harness.navigateByUrl('/email-templates?page=2&search=Verify&search=Ignored&locale=en');
    await harness.fixture.whenStable();
    harness.detectChanges();
    const rowText = harness.routeNativeElement?.querySelector('tbody')?.textContent;
    expect(rowText).toContain(matchingTemplates.at(-1)?.subject);
    expect(rowText).not.toContain(other.subject);
    expect(getAll).toHaveBeenCalledOnce();

    harness.routeNativeElement?.querySelector<HTMLButtonElement>('tbody button')?.click();
    await harness.fixture.whenStable();
    harness.detectChanges();
    const router = TestBed.inject(Router);
    expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe('1');
    expect(router.parseUrl(router.url).queryParamMap.get('search')).toBe('Verify');
    expect(harness.routeNativeElement?.querySelector('tbody')?.textContent).toContain(templates[0].subject);
    expect(getAll).toHaveBeenCalledTimes(2);
});

async function configureRenderedTemplatesAsync(
    getAll: AdminEmailTemplatesService['getAll'],
    closed: () => Observable<AdminEmailTemplate | undefined>,
): Promise<void> {
    await TestBed.configureTestingModule({
        imports: [AdminEmailTemplatesComponent],
        providers: [
            provideRouter(
                [{ path: 'email-templates', component: AdminEmailTemplatesComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            ...provideTranslateTesting(),
            { provide: AdminEmailTemplatesService, useValue: { getAll } },
            { provide: FdUiDialogService, useValue: { open: (): { afterClosed: typeof closed } => ({ afterClosed: closed }) } },
        ],
    }).compileComponents();
}
