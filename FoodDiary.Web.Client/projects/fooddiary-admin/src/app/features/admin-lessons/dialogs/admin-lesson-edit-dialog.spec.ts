import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { finalize, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import type { AdminLesson } from '../models/admin-lesson.data';
import { AdminLessonEditDialogComponent } from './admin-lesson-edit-dialog';

const LESSON: AdminLesson = {
    id: adminId<'lesson'>('audit-lesson'),
    title: 'Audit lesson draft',
    content: '<p>Retained lesson content</p>',
    summary: null,
    locale: 'en',
    category: 'NutritionBasics',
    difficulty: 'Beginner',
    estimatedReadMinutes: 3,
    sortOrder: 0,
    isPublished: false,
    createdOnUtc: adminUtcInstant('2026-10-17T00:00:00Z'),
    modifiedOnUtc: null,
};

describe('AdminLessonEditDialogComponent pending save', () => {
    it.each([true, false])('prevents cancellation while saving and unlocks after failure (new=%s)', async isNew => {
        const pending = new Subject<AdminLesson>();
        const operation = vi.fn().mockReturnValue(pending.asObservable());
        const dialogRef = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [AdminLessonEditDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: { ...LESSON, isNew } },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: AdminLessonsFacade, useValue: { create: operation, update: operation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonEditDialogComponent);
        fixture.componentInstance['onSave']();
        fixture.detectChanges();
        const cancel = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
            '.dialog-actions fd-ui-button:first-child button',
        );
        expect(cancel?.disabled).toBe(true);
        const title = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="text"]');
        expect(title?.disabled).toBe(true);
        expect(fixture.componentInstance['form'].title().disabled()).toBe(true);
        fixture.componentInstance['onCancel']();
        expect(dialogRef.close).not.toHaveBeenCalled();
        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(cancel?.disabled).toBe(false);
        expect(title?.disabled).toBe(false);
        expect(title?.value).toBe(LESSON.title);
        fixture.componentInstance['onCancel']();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(false);
    });

    it('ends a pending save subscription when its dialog is destroyed', async () => {
        const pending = new Subject<AdminLesson>();
        const finished = vi.fn();
        const operation = vi.fn().mockReturnValue(pending.pipe(finalize(finished)));
        const dialogRef = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [AdminLessonEditDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: LESSON },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: AdminLessonsFacade, useValue: { create: operation, update: operation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonEditDialogComponent);
        fixture.componentInstance['onSave']();
        fixture.destroy();
        expect(finished).toHaveBeenCalledOnce();
        pending.next(LESSON);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

describe('AdminLessonEditDialogComponent numeric validation', () => {
    it.each([
        { field: 'estimatedReadMinutes', value: 0 },
        { field: 'estimatedReadMinutes', value: 1.5 },
        { field: 'estimatedReadMinutes', value: 2147483648 },
        { field: 'sortOrder', value: -1 },
        { field: 'sortOrder', value: 1.5 },
        { field: 'sortOrder', value: 2147483648 },
    ] as const)('rejects $field=$value before sending a request and allows a corrected value', async ({ field, value }) => {
        const operation = vi.fn().mockReturnValue(new Subject<AdminLesson>());
        await TestBed.configureTestingModule({
            imports: [AdminLessonEditDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: LESSON },
                { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
                { provide: AdminLessonsFacade, useValue: { create: operation, update: operation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonEditDialogComponent);
        const component = fixture.componentInstance;
        component['formModel'].update(model => ({ ...model, [field]: value }));
        fixture.detectChanges();
        component['onSave']();
        expect(operation).not.toHaveBeenCalled();
        expect(component['form']().invalid()).toBe(true);

        component['formModel'].update(model => ({ ...model, [field]: 2147483647 }));
        component['onSave']();
        expect(operation).toHaveBeenCalledExactlyOnceWith(LESSON.id, expect.objectContaining({ [field]: 2147483647 }));
    });
});

describe('AdminLessonEditDialogComponent save recovery', () => {
    it.each([true, false])('announces a failed save and retains the draft before retry (new=%s)', async isNew => {
        const operation = vi.fn().mockReturnValueOnce(throwError(() => new Error('unavailable')));
        const dialogRef = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [AdminLessonEditDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: { ...LESSON, isNew } },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: AdminLessonsFacade, useValue: { create: operation, update: operation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminLessonEditDialogComponent);
        fixture.detectChanges();
        fixture.componentInstance['onSave']();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_LESSONS.SAVE_ERROR');
        expect(host.querySelector<HTMLInputElement>('fd-ui-input input')?.value).toBe(LESSON.title);
        expect(host.querySelectorAll<HTMLTextAreaElement>('textarea')[1].value).toBe(LESSON.content);
        expect(dialogRef.close).not.toHaveBeenCalled();

        const pending = new Subject<AdminLesson>();
        operation.mockReturnValueOnce(pending.asObservable());
        fixture.componentInstance['onSave']();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect([...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(-1)?.disabled).toBe(true);

        pending.next(LESSON);
        pending.complete();
        fixture.detectChanges();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
        expect(operation).toHaveBeenCalledTimes(2);
    });
});
