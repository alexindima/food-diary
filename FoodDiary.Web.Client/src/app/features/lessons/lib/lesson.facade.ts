import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, effect, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, finalize, firstValueFrom } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { LessonService } from '../api/lesson.service';
import type { LessonDetail, LessonPage } from '../models/lesson.data';

const LESSON_PAGE_SIZE = 18;
const SEARCH_DEBOUNCE_MS = 300;

@Injectable()
export class LessonFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly translateService = inject(TranslateService);
    private readonly selectedLessonId = signal<string | null>(null);
    private readonly markedReadIds = signal<Set<string>>(new Set());
    private readonly lastLoadedPage = signal<LessonPage | null>(null);

    public readonly categoryFilter = signal<string | null>(null);
    public readonly difficultyFilter = signal<string | null>(null);
    public readonly searchQuery = signal('');
    public readonly sortOrder = signal<'recommended' | 'shortest'>('recommended');
    public readonly pageIndex = signal(0);
    private readonly debouncedSearchQuery = toSignal(
        toObservable(this.searchQuery).pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()),
        { initialValue: '' },
    );
    private readonly lessonsResource = resource({
        params: () => ({
            locale: this.getCurrentLocale(),
            category: this.categoryFilter(),
            difficulty: this.difficultyFilter(),
            search: this.debouncedSearchQuery(),
            sort: this.sortOrder(),
            page: this.pageIndex() + 1,
            pageSize: LESSON_PAGE_SIZE,
        }),
        loader: async ({ params }): Promise<LessonPage> =>
            firstValueFrom(
                this.service.getAll({
                    ...params,
                    category: params.category ?? undefined,
                    difficulty: params.difficulty ?? undefined,
                    search: params.search.length > 0 ? params.search : undefined,
                }),
            ),
    });
    private readonly selectedLessonResource = resource({
        params: () => this.selectedLessonId(),
        loader: async ({ params }): Promise<LessonDetail | null> => {
            if (params === null || params.length === 0) {
                return null;
            }

            return firstValueFrom(this.service.getById(params));
        },
    });

    public constructor() {
        effect(() => {
            if (this.lessonsResource.hasValue()) {
                this.lastLoadedPage.set(this.lessonsResource.value());
            }
        });
    }

    public readonly page = computed<LessonPage>(() =>
        this.lessonsResource.hasValue()
            ? this.lessonsResource.value()
            : (this.lastLoadedPage() ?? {
                  items: [],
                  page: this.pageIndex() + 1,
                  pageSize: LESSON_PAGE_SIZE,
                  totalCount: 0,
                  totalPages: 0,
                  totalLessonCount: 0,
                  readLessonCount: 0,
                  availableCategories: [],
              }),
    );
    public readonly lessons = computed(() => {
        const lessons = this.page().items;
        const markedReadIds = this.markedReadIds();
        if (markedReadIds.size === 0) {
            return lessons;
        }

        return lessons.map(lesson => (markedReadIds.has(lesson.id) ? { ...lesson, isRead: true } : lesson));
    });
    public readonly isLoading = computed(() => this.lessonsResource.isLoading());
    public readonly hasLoadError = computed(() => this.lessonsResource.error() !== undefined);
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
        const error = this.selectedLessonResource.error();
        return error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.NotFound);
    });
    public readonly isMarkingRead = signal(false);
    public readonly markReadFailed = signal(false);

    public loadLessons(category?: string | null): void {
        this.categoryFilter.set(category ?? null);
    }

    public resetPage(): void {
        this.pageIndex.set(0);
    }

    public retryLessons(): void {
        this.lessonsResource.reload();
    }

    public loadLesson(id: string): void {
        this.markReadFailed.set(false);
        this.selectedLessonId.set(id);
    }

    public retryLesson(): void {
        this.selectedLessonResource.reload();
    }

    public markRead(id: string): void {
        if (this.isMarkingRead() || this.markedReadIds().has(id)) {
            return;
        }
        this.isMarkingRead.set(true);
        this.markReadFailed.set(false);
        this.service
            .markRead(id)
            .pipe(
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

    private getCurrentLocale(): string {
        return resolveTranslateLanguage(this.translateService).split(/[_-]/)[0];
    }
}
