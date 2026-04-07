import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AdminLessonsService } from '../api/admin-lessons.service';
import { AdminLesson } from '../models/admin-lesson.data';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog.component';

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
            estimatedReadMinutes: 5,
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
                if (updated) {
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
                if (updated) {
                    this.loadLessons();
                }
            });
    }

    public deleteLesson(lesson: AdminLesson): void {
        if (!confirm(`Delete "${lesson.title}"?`)) {
            return;
        }

        this.lessonsService
            .delete(lesson.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.loadLessons(),
            });
    }
}
