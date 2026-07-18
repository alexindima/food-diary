import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, min, required } from '@angular/forms/signals';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import { type AdminLesson, CONTENT_MAX_LENGTH, LESSON_CATEGORIES, LESSON_DIFFICULTIES, LESSON_LOCALES } from '../models/admin-lesson.data';

type LessonFormModel = {
    title: string;
    content: string;
    summary: string;
    locale: string;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    sortOrder: number;
};

const TITLE_MAX_LENGTH = 256;
const SUMMARY_MAX_LENGTH = 512;
const DEFAULT_LOCALE = 'ru';
const DEFAULT_CATEGORY = 'NutritionBasics';
const DEFAULT_DIFFICULTY = 'Beginner';
const DEFAULT_ESTIMATED_READ_MINUTES = 5;
const DEFAULT_SORT_ORDER = 0;

@Component({
    selector: 'fd-admin-lesson-edit-dialog',
    imports: [DecimalPipe, FormField, FdUiInputComponent, FdUiTextareaComponent, FdUiButtonComponent, FdUiSelectComponent],
    templateUrl: './admin-lesson-edit-dialog.html',
    styleUrl: './admin-lesson-edit-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLessonEditDialogComponent {
    protected readonly data = inject<AdminLesson>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<AdminLessonEditDialogComponent, boolean>>(FdUiDialogRef);
    private readonly lessonsFacade = inject(AdminLessonsFacade);

    protected readonly isNew = (this.data as AdminLesson & { isNew?: boolean }).isNew === true;
    protected readonly isSaving = signal(false);
    protected readonly showPreview = signal(false);
    protected readonly previewHtml = computed(() => this.formModel().content);
    protected readonly contentLength = computed(() => this.formModel().content.length);
    protected readonly contentMaxLength = CONTENT_MAX_LENGTH;
    protected readonly contentRemaining = computed(() => this.contentMaxLength - this.contentLength());

    protected readonly categoryOptions: Array<FdUiSelectOption<string>> = LESSON_CATEGORIES.map(c => ({ value: c, label: c }));
    protected readonly difficultyOptions: Array<FdUiSelectOption<string>> = LESSON_DIFFICULTIES.map(d => ({ value: d, label: d }));
    protected readonly localeOptions: Array<FdUiSelectOption<string>> = LESSON_LOCALES.map(l => ({ value: l, label: l }));

    protected readonly formModel = signal<LessonFormModel>({
        title: this.data.title,
        content: this.data.content,
        summary: this.data.summary ?? '',
        locale: this.resolveStringValue(this.data.locale, DEFAULT_LOCALE),
        category: this.resolveStringValue(this.data.category, DEFAULT_CATEGORY),
        difficulty: this.resolveStringValue(this.data.difficulty, DEFAULT_DIFFICULTY),
        estimatedReadMinutes:
            this.data.estimatedReadMinutes > DEFAULT_SORT_ORDER ? this.data.estimatedReadMinutes : DEFAULT_ESTIMATED_READ_MINUTES,
        sortOrder: this.data.sortOrder,
    });
    protected readonly form = form(this.formModel, path => {
        required(path.title);
        maxLength(path.title, TITLE_MAX_LENGTH);
        required(path.content);
        maxLength(path.content, CONTENT_MAX_LENGTH);
        maxLength(path.summary, SUMMARY_MAX_LENGTH);
        required(path.locale);
        required(path.category);
        required(path.difficulty);
        required(path.estimatedReadMinutes);
        min(path.estimatedReadMinutes, 1);
        min(path.sortOrder, DEFAULT_SORT_ORDER);
    });

    protected onCancel(): void {
        this.dialogRef.close(false);
    }

    protected onSave(): void {
        if (this.form().invalid() || this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        const value = this.formModel();
        const request = {
            title: value.title,
            content: value.content,
            summary: value.summary.length > 0 ? value.summary : null,
            locale: value.locale,
            category: value.category,
            difficulty: value.difficulty,
            estimatedReadMinutes: value.estimatedReadMinutes,
            sortOrder: value.sortOrder,
        };

        const operation = this.isNew ? this.lessonsFacade.create(request) : this.lessonsFacade.update(this.data.id, request);

        operation.subscribe({
            next: () => {
                this.isSaving.set(false);
                this.dialogRef.close(true);
            },
            error: () => {
                this.isSaving.set(false);
            },
        });
    }

    protected togglePreview(): void {
        this.showPreview.update(value => !value);
    }

    private resolveStringValue(value: string, fallback: string): string {
        return value.length > 0 ? value : fallback;
    }
}
