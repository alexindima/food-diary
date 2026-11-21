import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, type Observable, of, Subject, takeUntil } from 'rxjs';

import type { LessonId } from '../../../shared/models/semantics/entity-id';
import { LessonService } from '../api/lesson.service';
import type { LessonDetail } from '../models/lesson.data';

@Injectable()
export class LessonFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly selectedLessonId = signal<LessonId | null>(null);
    private readonly selectionChanged = new Subject<void>();
    private readonly markedReadIds = signal<Set<LessonId>>(new Set());
    private readonly selectedLessonResource = rxResource({
        params: () => this.selectedLessonId(),
        stream: ({ params }): Observable<LessonDetail | null> => {
            if (params === null || params.length === 0) {
                return of(null);
            }

            return this.service.getById(params);
        },
    });

    public readonly selectedLesson = computed(() => {
        const lesson = this.selectedLessonResource.hasValue() ? (this.selectedLessonResource.value() ?? null) : null;
        if (lesson === null) {
            return null;
        }

        return this.markedReadIds().has(lesson.id) ? { ...lesson, isRead: true } : lesson;
    });
    public readonly isDetailLoading = computed(() => this.selectedLessonResource.isLoading());
    public readonly hasDetailError = computed(() => this.selectedLessonResource.error() !== undefined);
    public readonly isLessonMissing = computed(() => {
        if (this.selectedLessonId() === null) {
            return true;
        }
        const error = this.selectedLessonResource.error();
        return error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.NotFound);
    });
    public readonly isMarkingRead = signal(false);
    public readonly markReadFailed = signal(false);

    public loadLesson(id: LessonId | null): void {
        if (id !== this.selectedLessonId()) {
            this.selectionChanged.next();
        }
        this.markReadFailed.set(false);
        this.selectedLessonId.set(id);
    }

    public retryLesson(): void {
        this.selectedLessonResource.reload();
    }

    public markRead(id: LessonId): void {
        if (this.isMarkingRead() || this.markedReadIds().has(id)) {
            return;
        }
        this.isMarkingRead.set(true);
        this.markReadFailed.set(false);
        this.service
            .markRead(id)
            .pipe(
                takeUntil(this.selectionChanged),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isMarkingRead.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.markedReadIds.update(current => new Set(current).add(id));
                },
                error: () => {
                    this.markReadFailed.set(true);
                },
            });
    }
}
