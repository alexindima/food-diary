import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { AdminLessonsService } from '../api/admin-lessons.service';
import { type AdminLesson, CONTENT_MAX_LENGTH, LESSON_CATEGORIES, LESSON_DIFFICULTIES, LESSON_LOCALES } from '../models/admin-lesson.data';

type LessonForm = {
    title: FormControl<string>;
    content: FormControl<string>;
    summary: FormControl<string>;
    locale: FormControl<string>;
    category: FormControl<string>;
    difficulty: FormControl<string>;
    estimatedReadMinutes: FormControl<number>;
    sortOrder: FormControl<number>;
};

@Component({
    selector: 'fd-admin-lesson-edit-dialog',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FdUiInputComponent, FdUiTextareaComponent, FdUiButtonComponent, FdUiSelectComponent],
    templateUrl: './admin-lesson-edit-dialog.component.html',
    styleUrl: './admin-lesson-edit-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLessonEditDialogComponent {
    public readonly data = inject<AdminLesson>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<AdminLessonEditDialogComponent, boolean>>(FdUiDialogRef);
    private readonly service = inject(AdminLessonsService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly sanitizer = inject(DomSanitizer);

    public readonly isNew = (this.data as AdminLesson & { isNew?: boolean }).isNew === true;
    public readonly isSaving = signal(false);
    public readonly showPreview = signal(false);
    public readonly previewHtml = signal<SafeHtml>('' as SafeHtml);
    public readonly contentLength = signal(this.data.content.length);
    public readonly contentMaxLength = CONTENT_MAX_LENGTH;
    public readonly contentRemaining = computed(() => this.contentMaxLength - this.contentLength());

    public readonly categoryOptions: FdUiSelectOption<string>[] = LESSON_CATEGORIES.map(c => ({ value: c, label: c }));
    public readonly difficultyOptions: FdUiSelectOption<string>[] = LESSON_DIFFICULTIES.map(d => ({ value: d, label: d }));
    public readonly localeOptions: FdUiSelectOption<string>[] = LESSON_LOCALES.map(l => ({ value: l, label: l }));

    public readonly form = new FormGroup<LessonForm>({
        title: new FormControl(this.data.title, { nonNullable: true, validators: [Validators.required, Validators.maxLength(256)] }),
        content: new FormControl(this.data.content, {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(CONTENT_MAX_LENGTH)],
        }),
        summary: new FormControl(this.data.summary ?? '', { nonNullable: true, validators: [Validators.maxLength(512)] }),
        locale: new FormControl(this.data.locale || 'ru', { nonNullable: true, validators: [Validators.required] }),
        category: new FormControl(this.data.category || 'NutritionBasics', { nonNullable: true, validators: [Validators.required] }),
        difficulty: new FormControl(this.data.difficulty || 'Beginner', { nonNullable: true, validators: [Validators.required] }),
        estimatedReadMinutes: new FormControl(this.data.estimatedReadMinutes || 5, {
            nonNullable: true,
            validators: [Validators.required, Validators.min(1)],
        }),
        sortOrder: new FormControl(this.data.sortOrder || 0, { nonNullable: true, validators: [Validators.min(0)] }),
    });

    public constructor() {
        this.form.controls.content.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
            this.contentLength.set(value.length);
            if (this.showPreview()) {
                this.updatePreview();
            }
        });
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }

    public onSave(): void {
        if (this.form.invalid || this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        const value = this.form.getRawValue();
        const request = {
            title: value.title,
            content: value.content,
            summary: value.summary || null,
            locale: value.locale,
            category: value.category,
            difficulty: value.difficulty,
            estimatedReadMinutes: value.estimatedReadMinutes,
            sortOrder: value.sortOrder,
        };

        const operation = this.isNew ? this.service.create(request) : this.service.update(this.data.id, request);

        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.isSaving.set(false);
                this.dialogRef.close(true);
            },
            error: () => {
                this.isSaving.set(false);
            },
        });
    }

    public togglePreview(): void {
        const next = !this.showPreview();
        this.showPreview.set(next);
        if (next) {
            this.updatePreview();
        }
    }

    private updatePreview(): void {
        const html = this.form.controls.content.value;
        this.previewHtml.set(this.sanitizer.bypassSecurityTrustHtml(html));
    }
}
