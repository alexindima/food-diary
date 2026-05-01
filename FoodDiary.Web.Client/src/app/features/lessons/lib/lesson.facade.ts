import { computed, DestroyRef, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { LessonService } from '../api/lesson.service';
import { LessonDetail, LessonSummary } from '../models/lesson.data';

@Injectable()
export class LessonFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly translateService = inject(TranslateService);
    private readonly selectedLessonId = signal<string | null>(null);
    private readonly markedReadIds = signal<Set<string>>(new Set());

    public readonly categoryFilter = signal<string | null>(null);
    private readonly lessonsResource = resource({
        params: () => ({
            locale: this.getCurrentLocale(),
            category: this.categoryFilter(),
        }),
        loader: async ({ params }): Promise<LessonSummary[]> =>
            firstValueFrom(this.service.getAll(params.locale, params.category ?? undefined)),
    });
    private readonly selectedLessonResource = resource({
        params: () => this.selectedLessonId(),
        loader: async ({ params }): Promise<LessonDetail | null> => {
            if (!params) {
                return null;
            }

            return firstValueFrom(this.service.getById(params));
        },
    });

    public readonly lessons = computed(() => {
        const lessons = this.lessonsResource.hasValue() ? this.lessonsResource.value() : [];
        const markedReadIds = this.markedReadIds();
        if (markedReadIds.size === 0) {
            return lessons;
        }

        return lessons.map(lesson => (markedReadIds.has(lesson.id) ? { ...lesson, isRead: true } : lesson));
    });
    public readonly isLoading = computed(() => this.lessonsResource.isLoading());
    public readonly selectedLesson = computed(() => {
        const lesson = this.selectedLessonResource.hasValue() ? (this.selectedLessonResource.value() ?? null) : null;
        if (!lesson) {
            return null;
        }

        return this.markedReadIds().has(lesson.id) ? { ...lesson, isRead: true } : lesson;
    });
    public readonly isDetailLoading = computed(() => this.selectedLessonResource.isLoading());

    public loadLessons(category?: string | null): void {
        this.categoryFilter.set(category ?? null);
    }

    public loadLesson(id: string): void {
        this.selectedLessonId.set(id);
    }

    public markRead(id: string): void {
        this.service
            .markRead(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.markedReadIds.update(current => new Set(current).add(id));
            });
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
    }
}
