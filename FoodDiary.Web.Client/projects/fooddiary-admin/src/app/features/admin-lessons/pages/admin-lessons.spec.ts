import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog';
import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import type { AdminLesson, AdminLessonsImportRequest, AdminLessonsImportResponse } from '../models/admin-lesson.data';
import { AdminLessonsComponent } from './admin-lessons';

const LESSON: AdminLesson = {
    id: adminId<'lesson'>('qa-lesson'),
    title: 'QA lesson',
    content: '<p>QA content</p>',
    summary: null,
    locale: 'en',
    category: 'NutritionBasics',
    difficulty: 'Beginner',
    estimatedReadMinutes: 3,
    sortOrder: 0,
    isPublished: false,
    createdOnUtc: adminUtcInstant('2026-10-06T00:00:00Z'),
    modifiedOnUtc: null,
};

describe('AdminLessonsComponent dialog names', () => {
    const open = vi.fn(
        (
            _component: typeof AdminLessonEditDialogComponent,
            _config: FdUiDialogConfig<AdminLesson & { isNew?: boolean }>,
        ): { afterClosed: () => Observable<boolean> } => ({ afterClosed: (): Observable<boolean> => of(false) }),
    );

    beforeEach(async () => {
        open.mockClear();
        await TestBed.configureTestingModule({
            imports: [AdminLessonsComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll: (): Observable<AdminLesson[]> => of([LESSON]) } },
                { provide: FdUiDialogService, useValue: { open } },
            ],
        }).compileComponents();
    });

    it.each([
        { locale: 'en', create: 'Create lesson', edit: 'Edit lesson' },
        { locale: 'ru', create: 'Создать урок', edit: 'Редактировать урок' },
    ])('names create and edit dialogs in $locale without changing lesson data', ({ locale, create, edit }) => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation(locale, { ADMIN_LESSONS: { CREATE_DIALOG_TITLE: create, EDIT_DIALOG_TITLE: edit } });
        translate.use(locale);
        const fixture = TestBed.createComponent(AdminLessonsComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('header fd-ui-button:last-child button')?.click();
        expect(open).toHaveBeenLastCalledWith(AdminLessonEditDialogComponent, expect.objectContaining({ ariaLabel: create }));
        expect(open.mock.calls.at(-1)?.[1].data).toMatchObject({ isNew: true, isPublished: false });
        host.querySelector<HTMLButtonElement>('.actions fd-ui-button:first-child button')?.click();
        expect(open).toHaveBeenLastCalledWith(AdminLessonEditDialogComponent, expect.objectContaining({ ariaLabel: edit, data: LESSON }));
        expect(LESSON.title).toBe('QA lesson');
    });
});

describe('AdminLessonsComponent deletion recovery', () => {
    it('announces failed deletion, retains the lesson and clears the error while retrying', async () => {
        const getAll = vi.fn().mockReturnValue(of([LESSON]));
        const remove = vi.fn().mockReturnValueOnce(throwError(() => new Error('unavailable')));
        await TestBed.configureTestingModule({
            imports: [AdminLessonsComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll, delete: remove } },
                {
                    provide: FdUiDialogService,
                    useValue: { open: (): { afterClosed: () => Observable<boolean> } => ({ afterClosed: () => of(true) }) },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonsComponent);
        fixture.detectChanges();
        fixture.componentInstance['deleteLesson'](LESSON);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_LESSONS.DELETE_ERROR');
        expect(host.textContent).toContain(LESSON.title);
        expect(getAll).toHaveBeenCalledTimes(1);

        const pending = new Subject<void>();
        remove.mockReturnValueOnce(pending.asObservable());
        getAll.mockReturnValueOnce(of([]));
        fixture.componentInstance['deleteLesson'](LESSON);
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(getAll).toHaveBeenCalledTimes(1);

        pending.next();
        pending.complete();
        fixture.detectChanges();
        expect(getAll).toHaveBeenCalledTimes(2);
        expect(host.textContent).toContain('No lessons found.');
        expect(remove).toHaveBeenCalledTimes(2);
    });
});

describe('AdminLessonsComponent dialog ownership', () => {
    it.each(['create', 'edit', 'delete'] as const)('closes its %s dialog on owner destruction without stale actions', async action => {
        const closed = new Subject<boolean>();
        const close = vi.fn();
        const getAll = vi.fn().mockReturnValue(of([LESSON]));
        const remove = vi.fn().mockReturnValue(of(undefined));
        await TestBed.configureTestingModule({
            imports: [AdminLessonsComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll, delete: remove } },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: (): { afterClosed: () => Observable<boolean>; close: typeof close } => ({
                            afterClosed: (): Observable<boolean> => closed.asObservable(),
                            close,
                        }),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonsComponent);
        fixture.detectChanges();
        if (action === 'create') {
            fixture.componentInstance['openCreate']();
        } else if (action === 'edit') {
            fixture.componentInstance['openEdit'](LESSON);
        } else {
            fixture.componentInstance['deleteLesson'](LESSON);
        }
        fixture.destroy();
        expect(close).toHaveBeenCalledExactlyOnceWith();
        closed.next(true);
        expect(getAll).toHaveBeenCalledTimes(1);
        expect(remove).not.toHaveBeenCalled();
    });

    it('unregisters cleanup after the dialog closes normally', async () => {
        const closed = new Subject<boolean>();
        const close = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AdminLessonsComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll: (): Observable<AdminLesson[]> => of([LESSON]) } },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: (): { afterClosed: () => Observable<boolean>; close: typeof close } => ({
                            afterClosed: (): Observable<boolean> => closed.asObservable(),
                            close,
                        }),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonsComponent);
        fixture.detectChanges();
        fixture.componentInstance['openCreate']();
        closed.next(false);
        closed.complete();
        fixture.destroy();
        expect(close).not.toHaveBeenCalled();
    });
});

describe('AdminLessonsComponent refresh ordering', () => {
    it('retains the lesson created while an earlier list request is pending', async () => {
        const previous = new Subject<AdminLesson[]>();
        const refresh = new Subject<AdminLesson[]>();
        const getAll = vi.fn().mockReturnValueOnce(previous).mockReturnValueOnce(refresh);
        await TestBed.configureTestingModule({
            imports: [AdminLessonsComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll } },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: (): { afterClosed: () => Observable<boolean> } => ({ afterClosed: (): Observable<boolean> => of(true) }),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonsComponent);
        fixture.detectChanges();
        fixture.componentInstance['openCreate']();
        refresh.next([LESSON]);
        previous.next([]);
        previous.error(new Error('obsolete request failed'));

        expect(fixture.componentInstance['lessons']()).toEqual([LESSON]);
        expect(fixture.componentInstance['isLoading']()).toBe(false);
        expect(fixture.componentInstance['loadFailed']()).toBe(false);
    });
});

it('refreshes the rendered lesson after its edit dialog reports a completed save', async () => {
    const result = new Subject<boolean>();
    const revised = { ...LESSON, title: 'Revised lesson' };
    const getAll = vi
        .fn<AdminLessonsFacade['getAll']>()
        .mockReturnValueOnce(of([LESSON]))
        .mockReturnValueOnce(of([revised]));
    await TestBed.configureTestingModule({
        imports: [AdminLessonsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AdminLessonsFacade, useValue: { getAll } },
            {
                provide: FdUiDialogService,
                useValue: { open: (): { afterClosed: () => Observable<boolean> } => ({ afterClosed: (): Observable<boolean> => result }) },
            },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminLessonsComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const edit = [...host.querySelectorAll<HTMLButtonElement>('tbody button')].find(button => button.textContent.trim() === 'Edit');
    expect(edit).toBeDefined();
    edit?.click();
    expect(getAll).toHaveBeenCalledOnce();

    result.next(true);
    result.complete();
    fixture.detectChanges();

    expect(getAll).toHaveBeenCalledTimes(2);
    expect(host.querySelector('tbody')?.textContent).toContain(revised.title);
    expect(host.querySelector('tbody')?.textContent).not.toContain(LESSON.title);
});

it('shows a lesson list error and loads the available rows after Retry', async () => {
    const getAll = vi
        .fn<AdminLessonsFacade['getAll']>()
        .mockReturnValueOnce(throwError(() => new Error('offline')))
        .mockReturnValue(of([LESSON]));
    await TestBed.configureTestingModule({
        imports: [AdminLessonsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AdminLessonsFacade, useValue: { getAll } },
            { provide: FdUiDialogService, useValue: {} },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminLessonsComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
    expect(retry).not.toBeNull();
    expect(host.querySelector('tbody')).toBeNull();

    retry?.click();
    fixture.detectChanges();

    expect(host.querySelector('fd-admin-load-error')).toBeNull();
    expect(host.querySelector('tbody')?.textContent).toContain(LESSON.title);
});

const importPayload: AdminLessonsImportRequest = {
    version: 1,
    lessons: [
        {
            title: LESSON.title,
            content: LESSON.content,
            summary: LESSON.summary,
            locale: LESSON.locale,
            category: LESSON.category,
            difficulty: LESSON.difficulty,
            estimatedReadMinutes: LESSON.estimatedReadMinutes,
            sortOrder: LESSON.sortOrder,
            isPublished: false,
        },
    ],
};

function selectFile(fixture: ComponentFixture<AdminLessonsComponent>, contents: string): void {
    const host = fixture.nativeElement as HTMLElement;
    const input = host.querySelector<HTMLInputElement>('input[type="file"]');
    if (input === null) {
        throw new Error('Expected a lesson import input');
    }
    const file = new File([contents], 'lessons.json', { type: 'application/json' });
    Object.defineProperty(file, 'text', { value: vi.fn<() => Promise<string>>().mockResolvedValue(contents) });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();
}

async function setupImportAsync(): Promise<{
    fixture: ComponentFixture<AdminLessonsComponent>;
    host: HTMLElement;
    getAll: Mock<AdminLessonsFacade['getAll']>;
    importLessons: Mock<AdminLessonsFacade['importLessons']>;
}> {
    const getAll = vi.fn<AdminLessonsFacade['getAll']>().mockReturnValue(of([]));
    const importLessons = vi.fn<AdminLessonsFacade['importLessons']>().mockReturnValue(throwError(() => new Error('Invalid import data')));
    await TestBed.configureTestingModule({
        imports: [AdminLessonsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AdminLessonsFacade, useValue: { getAll, importLessons } },
            { provide: FdUiDialogService, useValue: {} },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminLessonsComponent);
    fixture.detectChanges();
    return { fixture, host: fixture.nativeElement as HTMLElement, getAll, importLessons };
}

describe('AdminLessonsComponent JSON import', () => {
    it.each([
        { contents: 'not JSON', message: 'Import file is not valid JSON.' },
        { contents: '{"version":2,"lessons":[]}', message: 'Import file has an invalid lesson format.' },
        { contents: '{"version":1,"lessons":[]}', message: 'Import file has an invalid lesson format.' },
        { contents: '{"version":1,"lessons":[null]}', message: 'Import file has an invalid lesson format.' },
        { contents: '{"version":1,"lessons":[{"title":"Missing required fields"}]}', message: 'Import file has an invalid lesson format.' },
        {
            contents: JSON.stringify({ ...importPayload, lessons: [{ ...importPayload.lessons[0], isPublished: 'false' }] }),
            message: 'Import file has an invalid lesson format.',
        },
        {
            contents: JSON.stringify({ ...importPayload, lessons: [{ ...importPayload.lessons[0], sortOrder: 1.5 }] }),
            message: 'Import file has an invalid lesson format.',
        },
    ])('rejects $contents before making an API request', async ({ contents, message }) => {
        const { fixture, host, getAll, importLessons } = await setupImportAsync();
        selectFile(fixture, contents);
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.textContent).toContain(message);
        expect(host.textContent).toContain('No lessons found.');
        expect(importLessons).not.toHaveBeenCalled();
        expect(getAll).toHaveBeenCalledOnce();
    });

    it('locks the import while it is pending and refreshes the lessons after a successful retry', async () => {
        const { fixture, host, getAll, importLessons } = await setupImportAsync();
        const pending = new Subject<AdminLessonsImportResponse>();
        importLessons.mockReturnValueOnce(pending).mockReturnValueOnce(of({ importedCount: 1, lessons: [LESSON] }));
        const contents = JSON.stringify(importPayload);

        selectFile(fixture, contents);
        await fixture.whenStable();
        expect(host.textContent).toContain('Importing...');
        selectFile(fixture, contents);
        await fixture.whenStable();
        expect(importLessons).toHaveBeenCalledExactlyOnceWith(importPayload);
        expect(getAll).toHaveBeenCalledOnce();

        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(host.textContent).toContain('Lesson import failed.');
        expect(host.textContent).toContain('Import JSON');
        expect(getAll).toHaveBeenCalledOnce();

        getAll.mockReturnValueOnce(of([LESSON]));
        selectFile(fixture, contents);
        await fixture.whenStable();
        fixture.detectChanges();
        expect(importLessons.mock.calls).toEqual([[importPayload], [importPayload]]);
        expect(getAll).toHaveBeenCalledTimes(2);
        expect(host.textContent).toContain('Imported 1 lessons.');
        expect(host.querySelector('tbody')?.textContent).toContain(LESSON.title);
    });
});

async function readLessonFileAsync(value: Blob | MediaSource | undefined): Promise<unknown> {
    if (!(value instanceof Blob)) {
        throw new Error('Expected a lesson export file');
    }
    const json = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.addEventListener('loadend', () => {
            if (typeof reader.result === 'string') {
                resolve(reader.result);
            } else {
                reject(new Error('Could not read lesson export'));
            }
        });
        reader.readAsText(value);
    });
    return JSON.parse(json) as unknown;
}

describe('AdminLessonsComponent filtered JSON export', () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('exports all matching pages in the reusable file format and excludes other locales', async () => {
        const matching = Array.from({ length: 21 }, (_, index) => ({
            ...LESSON,
            id: adminId<'lesson'>(`lesson-${index}`),
            title: `English lesson ${index + 1}`,
            sortOrder: index,
            isPublished: index === 0 ? undefined : false,
        }));
        const russian = { ...LESSON, id: adminId<'lesson'>('ru-lesson'), locale: 'ru', title: 'Урок по питанию' };
        const getAll = vi.fn<AdminLessonsFacade['getAll']>().mockReturnValue(of([...matching, russian]));
        await TestBed.configureTestingModule({
            providers: [
                provideRouter([{ path: 'lessons', component: AdminLessonsComponent }]),
                provideTranslateTesting(),
                { provide: AdminLessonsFacade, useValue: { getAll } },
                { provide: FdUiDialogService, useValue: {} },
            ],
        }).compileComponents();
        const harness = await RouterTestingHarness.create('/lessons?locale=en&page=2');
        const root = harness.routeNativeElement;
        if (root === null) {
            throw new Error('Missing lessons route');
        }
        const exportButton = Array.from(root.querySelectorAll('button')).find(button => button.textContent.trim() === 'Export JSON');
        if (exportButton === undefined) {
            throw new Error('Missing lesson export button');
        }
        expect(root.querySelector('tbody tr')?.textContent).toContain('English lesson 21');
        expect(root.querySelectorAll('tbody tr')).toHaveLength(1);
        const previous = root.querySelector<HTMLButtonElement>('fd-ui-pagination button:first-child');
        expect(previous).not.toBeNull();
        previous?.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(root.querySelector('tbody tr')?.textContent).toContain('English lesson 1');
        expect(TestBed.inject(Router).url).toBe('/lessons?locale=en&page=1');

        const link = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
        const create = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:lesson-export');
        const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
        exportButton.click();

        const anchor = link.mock.contexts[0];
        if (!(anchor instanceof HTMLAnchorElement)) {
            throw new Error('Missing lesson download link');
        }
        expect(anchor.download).toMatch(/^fooddiary-lessons-\d{4}-\d{2}-\d{2}\.json$/);
        expect(revoke).toHaveBeenCalledExactlyOnceWith('blob:lesson-export');
        const file = await readLessonFileAsync(create.mock.calls[0]?.[0]);
        expect(file).toEqual({
            version: 1,
            lessons: matching.map(lesson => ({
                ...importPayload.lessons[0],
                title: lesson.title,
                sortOrder: lesson.sortOrder,
                isPublished: lesson.isPublished ?? true,
            })),
        });
    });
});
