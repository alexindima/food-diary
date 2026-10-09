import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, effect, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, finalize, firstValueFrom } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import type { LessonId } from '../../../shared/models/semantics/entity-id';
import { resolvePaginationPage } from '../../../shared/navigation/pagination-query.utils';
import { LessonService } from '../api/lesson.service';
import type { LessonDetail, LessonPage } from '../models/lesson.data';
import { type LessonListQuery, lessonListQueryKey } from './list/lesson-list-query';
import { LESSON_LIST_QUERY_STATE } from './list/lesson-list-query-state';

const LESSON_PAGE_SIZE = 18;
const SEARCH_DEBOUNCE_MS = 300;

@Injectable()
export class LessonFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly translateService = inject(TranslateService);
    private readonly queryState = inject(LESSON_LIST_QUERY_STATE, { optional: true });
    private readonly initialQuery: LessonListQuery = this.queryState?.initial ?? {
        page: 1,
        search: '',
        category: null,
        difficulty: null,
        sort: 'recommended',
    };
    private readonly selectedLessonId = signal<LessonId | null>(null);
    private readonly markedReadIds = signal<Set<LessonId>>(new Set());
    private readonly lastLoadedPage = signal<{ key: string; page: LessonPage } | null>(null);

    public readonly categoryFilter = signal(this.initialQuery.category);
    public readonly difficultyFilter = signal(this.initialQuery.difficulty);
    public readonly searchQuery = signal(this.initialQuery.search);
    public readonly sortOrder = signal(this.initialQuery.sort);
    public readonly pageIndex = signal(this.initialQuery.page - 1);
    private readonly debouncedSearchQuery = toSignal(
        toObservable(this.searchQuery).pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged()),
        { initialValue: this.initialQuery.search },
    );
    private readonly lessonsResource = resource({
        params: () => ({ ...this.currentListQuery(), locale: this.getCurrentLocale() }),
        loader: async ({ params }): Promise<{ key: string; page: LessonPage }> => this.loadListPageAsync(params),
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
        this.connectListRoute();
        effect(() => {
            const page = this.currentLoadedPage();
            if (page !== null) {
                this.lastLoadedPage.set({ key: this.listRequestKey(this.currentListQuery(), this.getCurrentLocale()), page });
            }
        });
    }

    public readonly page = computed<LessonPage>(
        () =>
            this.currentLoadedPage() ??
            this.cachedListPage() ?? {
                items: [],
                page: this.pageIndex() + 1,
                pageSize: LESSON_PAGE_SIZE,
                totalCount: 0,
                totalPages: 0,
                totalLessonCount: 0,
                readLessonCount: 0,
                availableCategories: [],
            },
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
        if (category !== undefined) {
            this.categoryFilter.set(category);
        }
    }

    public resetPage(): void {
        this.pageIndex.set(0);
    }

    public retryLessons(): void {
        this.lessonsResource.reload();
    }

    public loadLesson(id: LessonId): void {
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

    private connectListRoute(): void {
        if (this.queryState === null) {
            return;
        }
        this.queryState.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(query => {
            this.applyListQuery(query);
        });
        effect(() => {
            if (this.searchQuery().trim() === this.debouncedSearchQuery().trim()) {
                this.writeListQuery(this.signalListQuery());
            }
        });
        void this.queryState.normalizePageAsync().catch(() => {
            /* Keep the current list usable if navigation fails. */
        });
    }

    private signalListQuery(): LessonListQuery {
        return {
            page: this.pageIndex() + 1,
            search: this.debouncedSearchQuery().trim(),
            category: this.categoryFilter(),
            difficulty: this.difficultyFilter(),
            sort: this.sortOrder(),
        };
    }

    private currentListQuery(): LessonListQuery {
        return this.queryState?.current() ?? this.signalListQuery();
    }

    private listRequestKey(query: LessonListQuery, locale: string): string {
        return `${locale}:${lessonListQueryKey(query)}`;
    }

    private currentLoadedPage(): LessonPage | null {
        if (!this.lessonsResource.hasValue()) {
            return null;
        }
        const loaded = this.lessonsResource.value();
        return loaded.key === this.listRequestKey(this.currentListQuery(), this.getCurrentLocale()) ? loaded.page : null;
    }

    private cachedListPage(): LessonPage | null {
        const loaded = this.lastLoadedPage();
        return loaded?.key === this.listRequestKey(this.currentListQuery(), this.getCurrentLocale()) ? loaded.page : null;
    }

    private async loadListPageAsync(query: LessonListQuery & { locale: string }): Promise<{ key: string; page: LessonPage }> {
        const key = this.listRequestKey(query, query.locale);
        const page = await firstValueFrom(
            this.service.getAll({
                ...query,
                category: query.category ?? undefined,
                difficulty: query.difficulty ?? undefined,
                search: query.search.length > 0 ? query.search : undefined,
                pageSize: LESSON_PAGE_SIZE,
            }),
        );
        if (
            key === this.listRequestKey(this.currentListQuery(), this.getCurrentLocale()) &&
            this.searchQuery().trim() === this.debouncedSearchQuery().trim()
        ) {
            const resolved = resolvePaginationPage(query.page, page.totalPages);
            if (resolved !== query.page) {
                this.pageIndex.set(resolved - 1);
                this.writeListQuery({ ...this.currentListQuery(), page: resolved }, true);
            }
        }
        return { key, page };
    }

    private applyListQuery(query: LessonListQuery): void {
        this.categoryFilter.set(query.category);
        this.difficultyFilter.set(query.difficulty);
        this.searchQuery.set(query.search);
        this.sortOrder.set(query.sort);
        this.pageIndex.set(query.page - 1);
    }

    private writeListQuery(query: LessonListQuery, replaceUrl = false): void {
        if (this.queryState !== null) {
            void this.queryState.writeAsync(query, { replaceUrl }).catch(() => {
                /* Retain the successful list on navigation failure. */
            });
        }
    }
}
