import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog';
import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import type { AdminLesson } from '../models/admin-lesson.data';
import { AdminLessonsComponent } from './admin-lessons';

const LESSON: AdminLesson = {
    id: 'qa-lesson',
    title: 'QA lesson',
    content: '<p>QA content</p>',
    summary: null,
    locale: 'en',
    category: 'NutritionBasics',
    difficulty: 'Beginner',
    estimatedReadMinutes: 3,
    sortOrder: 0,
    isPublished: false,
    createdOnUtc: '2026-10-06T00:00:00Z',
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
