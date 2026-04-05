import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { LessonService } from '../api/lesson.service';
import { LessonDetail, LessonSummary } from '../models/lesson.data';

@Injectable()
export class LessonFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly translateService = inject(TranslateService);

    public readonly lessons = signal<LessonSummary[]>([]);
    public readonly isLoading = signal(false);
    public readonly selectedLesson = signal<LessonDetail | null>(null);
    public readonly isDetailLoading = signal(false);
    public readonly categoryFilter = signal<string | null>(null);

    public loadLessons(category?: string | null): void {
        this.isLoading.set(true);
        const locale = this.getCurrentLocale();
        this.service
            .getAll(locale, category ?? undefined)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: lessons => {
                    this.lessons.set(lessons);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.lessons.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    public loadLesson(id: string): void {
        this.isDetailLoading.set(true);
        this.service
            .getById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: lesson => {
                    this.selectedLesson.set(lesson);
                    this.isDetailLoading.set(false);
                },
                error: () => {
                    this.selectedLesson.set(null);
                    this.isDetailLoading.set(false);
                },
            });
    }

    public markRead(id: string): void {
        this.service
            .markRead(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                const current = this.selectedLesson();
                if (current && current.id === id) {
                    this.selectedLesson.set({ ...current, isRead: true });
                }
                this.lessons.update(list => list.map(l => (l.id === id ? { ...l, isRead: true } : l)));
            });
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
    }
}
