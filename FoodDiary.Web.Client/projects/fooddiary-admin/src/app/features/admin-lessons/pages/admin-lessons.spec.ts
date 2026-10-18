import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog';
import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import type { AdminLesson } from '../models/admin-lesson.data';
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
