import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { filter, finalize, type Observable, switchMap, tap } from 'rxjs';

import { AdminCatalogFilterComponent, matchesAdminCatalog } from '../../../shared/catalog/admin-catalog-filter';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminLessonEditDialogComponent } from '../dialogs/admin-lesson-edit-dialog';
import { AdminLessonsFacade } from '../lib/admin-lessons.facade';
import type { AdminLesson, AdminLessonCreateRequest, AdminLessonsImportRequest } from '../models/admin-lesson.data';

const DEFAULT_ESTIMATED_READ_MINUTES = 5;
const EXPORT_DATE_LENGTH = 10;

@Component({
    selector: 'fd-admin-lessons',
    imports: [
        AdminCatalogFilterComponent,
        AdminLoadErrorComponent,
        FdUiPaginationComponent,
        TranslatePipe,
        CommonModule,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
    ],
    templateUrl: './admin-lessons.html',
    styleUrl: './admin-lessons.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLessonsComponent {
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly locale = input(undefined, { transform: firstAdminQueryValue });
    public readonly category = input(undefined, { transform: firstAdminQueryValue });
    public readonly state = input(undefined, { transform: firstAdminQueryValue });

    private readonly lessonsFacade = inject(AdminLessonsFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly reloadVersion = signal(0);
    private readonly lessonsResource = rxResource({
        params: this.reloadVersion,
        stream: () => this.lessonsFacade.getAll(),
        defaultValue: [],
    });

    protected readonly lessons = linkedSignal({
        source: this.lessonsResource.snapshot,
        computation: (snapshot, previous): AdminLesson[] => {
            if (snapshot.status === 'loading') {
                return previous?.value ?? [];
            }
            return snapshot.status === 'resolved' ? snapshot.value : [];
        },
    }).asReadonly();
    protected readonly isLoading = this.lessonsResource.isLoading;
    protected readonly loadFailed = computed(() => this.lessonsResource.error() !== undefined);
    protected readonly isImporting = signal(false);
    protected readonly importMessage = signal<string | null>(null);
    protected readonly deleteError = signal<string | null>(null);

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly appliedFilters = computed(() =>
        convertToParamMap({
            page: this.page() ?? null,
            search: this.search() ?? null,
            locale: this.locale() ?? null,
            category: this.category() ?? null,
            state: this.state() ?? null,
        }),
    );
    protected readonly categories = computed(() => [...new Set(this.lessons().map(item => item.category))].sort());
    protected readonly filteredItems = computed(() =>
        this.lessons().filter(item =>
            matchesAdminCatalog(this.appliedFilters(), {
                text: `${item.title} ${item.summary ?? ''}`,
                locale: item.locale,
                category: item.category,
                isActive: item.isPublished ?? true,
            }),
        ),
    );
    protected readonly canExport = computed(() => !this.isLoading() && !this.loadFailed() && this.filteredItems().length > 0);
    protected readonly pageSize = 20;
    protected readonly requestedPage = computed(() => adminPage(this.page() ?? null) - 1);
    protected readonly pageIndex = computed(() =>
        Math.min(this.requestedPage(), Math.max(0, Math.ceil(this.filteredItems().length / this.pageSize) - 1)),
    );
    protected readonly pageItems = computed(() =>
        this.filteredItems().slice(this.pageIndex() * this.pageSize, (this.pageIndex() + 1) * this.pageSize),
    );
    protected readonly rangeStart = computed(() => (this.filteredItems().length === 0 ? 0 : this.pageIndex() * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => Math.min((this.pageIndex() + 1) * this.pageSize, this.filteredItems().length));

    protected goToPage(pageIndex: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: pageIndex + 1 }, queryParamsHandling: 'merge' });
    }

    public constructor() {
        effect(() => {
            if (!this.isLoading() && !this.loadFailed()) {
                restoreAdminPage(this.router, this.route, this.pageIndex() + 1, {
                    totalItems: this.filteredItems().length,
                    pageSize: this.pageSize,
                });
            }
        });
    }

    protected loadLessons(): void {
        this.reloadVersion.update(version => version + 1);
    }

    protected openCreate(): void {
        const dialogData: AdminLesson & { isNew: boolean } = {
            id: adminId<'lesson'>(''),
            title: '',
            content: '',
            summary: null,
            locale: 'ru',
            category: 'NutritionBasics',
            difficulty: 'Beginner',
            estimatedReadMinutes: DEFAULT_ESTIMATED_READ_MINUTES,
            sortOrder: 0,
            isPublished: false,
            createdOnUtc: adminUtcInstant(new Date().toISOString()),
            modifiedOnUtc: null,
            isNew: true,
        };

        const dialogRef = this.dialogService.open(AdminLessonEditDialogComponent, {
            ariaLabel: String(this.translate.instant('ADMIN_LESSONS.CREATE_DIALOG_TITLE')),
            size: 'lg',
            panelClass: ['fd-admin-lesson-dialog', 'fd-admin-lesson-dialog--fullscreen'],
            data: dialogData,
        });
        this.afterClosedWithOwner(dialogRef).subscribe(updated => {
            if (updated === true) {
                this.loadLessons();
            }
        });
    }

    protected openEdit(lesson: AdminLesson): void {
        const dialogRef = this.dialogService.open(AdminLessonEditDialogComponent, {
            ariaLabel: String(this.translate.instant('ADMIN_LESSONS.EDIT_DIALOG_TITLE')),
            size: 'lg',
            panelClass: ['fd-admin-lesson-dialog', 'fd-admin-lesson-dialog--fullscreen'],
            data: lesson,
        });
        this.afterClosedWithOwner(dialogRef).subscribe(updated => {
            if (updated === true) {
                this.loadLessons();
            }
        });
    }

    protected deleteLesson(lesson: AdminLesson): void {
        const dialogRef = this.dialogService.open(FdUiConfirmDialogComponent, {
            size: 'sm',
            data: {
                title: 'Delete lesson',
                message: `Delete "${lesson.title}"?`,
                confirmLabel: 'Delete',
                danger: true,
            },
        });
        this.afterClosedWithOwner(dialogRef)
            .pipe(
                filter((confirmed): confirmed is true => confirmed === true),
                tap(() => {
                    this.deleteError.set(null);
                }),
                switchMap(() => this.lessonsFacade.delete(lesson.id)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.loadLessons();
                },
                error: () => {
                    this.deleteError.set('ADMIN_LESSONS.DELETE_ERROR');
                },
            });
    }

    private afterClosedWithOwner<T, R>(dialogRef: FdUiDialogRef<T, R>): Observable<R | undefined> {
        const unregisterCleanup = this.destroyRef.onDestroy(() => {
            dialogRef.close();
        });
        return dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef), finalize(unregisterCleanup));
    }

    protected exportLessons(): void {
        if (!this.canExport()) {
            return;
        }
        const payload: AdminLessonsImportRequest = {
            version: 1,
            lessons: this.filteredItems().map(lesson => this.toImportLesson(lesson)),
        };
        const fileName = `fooddiary-lessons-${new Date().toISOString().slice(0, EXPORT_DATE_LENGTH)}.json`;
        const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const anchor = this.document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
    }

    protected importLessons(fileInput: HTMLInputElement): void {
        const file = fileInput.files?.[0];
        if (file === undefined || this.isImporting()) {
            return;
        }

        this.importMessage.set(null);
        this.isImporting.set(true);
        void this.importLessonsFileAsync(file, fileInput);
    }

    private async importLessonsFileAsync(file: File, fileInput: HTMLInputElement): Promise<void> {
        try {
            const payload = JSON.parse(await file.text()) as unknown;
            if (!this.isImportPayload(payload)) {
                this.importMessage.set('Import file has an invalid lesson format.');
                this.isImporting.set(false);
                fileInput.value = '';
                return;
            }

            this.lessonsFacade
                .importLessons(payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        this.importMessage.set(`Imported ${response.importedCount} lessons.`);
                        this.isImporting.set(false);
                        fileInput.value = '';
                        this.loadLessons();
                    },
                    error: () => {
                        this.importMessage.set('Lesson import failed.');
                        this.isImporting.set(false);
                        fileInput.value = '';
                    },
                });
        } catch {
            this.importMessage.set('Import file is not valid JSON.');
            this.isImporting.set(false);
            fileInput.value = '';
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
            isPublished: lesson.isPublished ?? true,
        };
    }

    private isImportPayload(value: unknown): value is AdminLessonsImportRequest {
        if (!this.isRecord(value) || value['version'] !== 1 || !Array.isArray(value['lessons']) || value['lessons'].length === 0) {
            return false;
        }

        return value['lessons'].every(lesson => {
            if (!this.isRecord(lesson)) {
                return false;
            }

            return (
                ['title', 'content', 'locale', 'category', 'difficulty'].every(field => typeof lesson[field] === 'string') &&
                (typeof lesson['summary'] === 'string' || lesson['summary'] === null) &&
                Number.isInteger(lesson['estimatedReadMinutes']) &&
                Number.isInteger(lesson['sortOrder']) &&
                (lesson['isPublished'] === undefined || typeof lesson['isPublished'] === 'boolean')
            );
        });
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null;
    }
}
