import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { filter, switchMap } from 'rxjs';

import { AdminLessonsService } from '../api/admin-lessons.service';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog.component';
import type { AdminLesson, AdminLessonCreateRequest, AdminLessonsImportRequest } from '../models/admin-lesson.data';

const DEFAULT_ESTIMATED_READ_MINUTES = 5;
const EXPORT_DATE_LENGTH = 10;

@Component({
    selector: 'fd-admin-lessons',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent],
    templateUrl: './admin-lessons.component.html',
    styleUrl: './admin-lessons.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLessonsComponent {
    private readonly lessonsService = inject(AdminLessonsService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly lessons = signal<AdminLesson[]>([]);
    public readonly isLoading = signal(false);
    public readonly isImporting = signal(false);
    public readonly importMessage = signal<string | null>(null);

    public constructor() {
        this.loadLessons();
    }

    public loadLessons(): void {
        this.isLoading.set(true);
        this.lessonsService
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.lessons.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.lessons.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    public openCreate(): void {
        const dialogData: AdminLesson & { isNew: boolean } = {
            id: '',
            title: '',
            content: '',
            summary: null,
            locale: 'ru',
            category: 'NutritionBasics',
            difficulty: 'Beginner',
            estimatedReadMinutes: DEFAULT_ESTIMATED_READ_MINUTES,
            sortOrder: 0,
            createdOnUtc: new Date().toISOString(),
            modifiedOnUtc: null,
            isNew: true,
        };

        this.dialogService
            .open(AdminLessonEditDialogComponent, {
                size: 'lg',
                panelClass: ['fd-admin-lesson-dialog', 'fd-admin-lesson-dialog--fullscreen'],
                data: dialogData,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.loadLessons();
                }
            });
    }

    public openEdit(lesson: AdminLesson): void {
        this.dialogService
            .open(AdminLessonEditDialogComponent, {
                size: 'lg',
                panelClass: ['fd-admin-lesson-dialog', 'fd-admin-lesson-dialog--fullscreen'],
                data: lesson,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.loadLessons();
                }
            });
    }

    public deleteLesson(lesson: AdminLesson): void {
        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                size: 'sm',
                data: {
                    title: 'Delete lesson',
                    message: `Delete "${lesson.title}"?`,
                    confirmLabel: 'Delete',
                    danger: true,
                },
            })
            .afterClosed()
            .pipe(
                filter((confirmed): confirmed is true => confirmed === true),
                switchMap(() => this.lessonsService.delete(lesson.id)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.loadLessons();
                },
            });
    }

    public exportLessons(): void {
        const payload: AdminLessonsImportRequest = {
            version: 1,
            lessons: this.lessons().map(lesson => this.toImportLesson(lesson)),
        };
        const fileName = `fooddiary-lessons-${new Date().toISOString().slice(0, EXPORT_DATE_LENGTH)}.json`;
        const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
    }

    public importLessons(event: Event): void {
        const input = event.target instanceof HTMLInputElement ? event.target : null;
        const file = input?.files?.[0];
        if (input === null || file === undefined || this.isImporting()) {
            return;
        }

        this.importMessage.set(null);
        this.isImporting.set(true);
        void this.importLessonsFileAsync(file, input);
    }

    private async importLessonsFileAsync(file: File, input: HTMLInputElement): Promise<void> {
        try {
            const payload = JSON.parse(await file.text()) as unknown;
            if (!this.isImportPayload(payload)) {
                this.importMessage.set('Import file has an invalid lesson format.');
                this.isImporting.set(false);
                input.value = '';
                return;
            }

            this.lessonsService
                .importLessons(payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        this.importMessage.set(`Imported ${response.importedCount} lessons.`);
                        this.isImporting.set(false);
                        input.value = '';
                        this.loadLessons();
                    },
                    error: () => {
                        this.importMessage.set('Lesson import failed.');
                        this.isImporting.set(false);
                        input.value = '';
                    },
                });
        } catch {
            this.importMessage.set('Import file is not valid JSON.');
            this.isImporting.set(false);
            input.value = '';
        }
    }

    private toImportLesson(lesson: AdminLesson): AdminLessonCreateRequest {
        return {
            title: lesson.title,
            content: lesson.content,
            summary: lesson.summary,
            locale: lesson.locale,
            category: lesson.category,
            difficulty: lesson.difficulty,
            estimatedReadMinutes: lesson.estimatedReadMinutes,
            sortOrder: lesson.sortOrder,
        };
    }

    private isImportPayload(value: unknown): value is AdminLessonsImportRequest {
        if (!this.isRecord(value) || value['version'] !== 1 || !Array.isArray(value['lessons'])) {
            return false;
        }

        return value['lessons'].every(lesson => {
            if (!this.isRecord(lesson)) {
                return false;
            }

            return (
                typeof lesson['title'] === 'string' &&
                typeof lesson['content'] === 'string' &&
                (typeof lesson['summary'] === 'string' || lesson['summary'] === null) &&
                typeof lesson['locale'] === 'string' &&
                typeof lesson['category'] === 'string' &&
                typeof lesson['difficulty'] === 'string' &&
                typeof lesson['estimatedReadMinutes'] === 'number' &&
                typeof lesson['sortOrder'] === 'number'
            );
        });
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null;
    }
}
