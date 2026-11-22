import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged, type Observable, tap } from 'rxjs';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { resolvePaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { LessonService } from '../../api/lesson.service';
import type { LessonPage } from '../../models/lesson.data';
import { type LessonListQuery, lessonListQueryKey } from './lesson-list-query';
import { LESSON_LIST_QUERY_STATE } from './lesson-list-query-state';

const LESSON_PAGE_SIZE = 18;
const SEARCH_DEBOUNCE_MS = 300;

@Injectable()
export class LessonListFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(LessonService);
    private readonly translateService = inject(TranslateService);
    private readonly queryState = inject(LESSON_LIST_QUERY_STATE);
    private readonly initialQuery = this.queryState.initial;
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
    private readonly lessonsResource = rxResource({
        params: () => ({ ...this.queryState.current(), locale: this.getCurrentLocale() }),
        stream: ({ params }) => this.loadListPage(params),
    });

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
    public readonly lessons = computed(() => this.page().items);
    public readonly isLoading = computed(() => this.lessonsResource.isLoading());
    public readonly hasLoadError = computed(() => this.lessonsResource.error() !== undefined);

    public constructor() {
        this.connectListRoute();
        effect(() => {
            const page = this.currentLoadedPage();
            if (page !== null) {
                this.lastLoadedPage.set({ key: this.listRequestKey(this.queryState.current(), this.getCurrentLocale()), page });
            }
        });
    }

    public resetPage(): void {
        this.pageIndex.set(0);
    }

    public retryLessons(): void {
        this.lessonsResource.reload();
    }

    private getCurrentLocale(): string {
        return resolveTranslateLanguage(this.translateService).split(/[_-]/)[0];
    }

    private connectListRoute(): void {
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

    private listRequestKey(query: LessonListQuery, locale: string): string {
        return `${locale}:${lessonListQueryKey(query)}`;
    }

    private currentLoadedPage(): LessonPage | null {
        return this.lessonsResource.hasValue() ? this.lessonsResource.value() : null;
    }

    private cachedListPage(): LessonPage | null {
        const loaded = this.lastLoadedPage();
        return loaded?.key === this.listRequestKey(this.queryState.current(), this.getCurrentLocale()) ? loaded.page : null;
    }

    private loadListPage(query: LessonListQuery & { locale: string }): Observable<LessonPage> {
        const key = this.listRequestKey(query, query.locale);
        return this.service
            .getAll({
                ...query,
                category: query.category ?? undefined,
                difficulty: query.difficulty ?? undefined,
                search: query.search.length > 0 ? query.search : undefined,
                pageSize: LESSON_PAGE_SIZE,
            })
            .pipe(
                tap(page => {
                    if (
                        !this.destroyRef.destroyed &&
                        key === this.listRequestKey(this.queryState.current(), this.getCurrentLocale()) &&
                        this.searchQuery().trim() === this.debouncedSearchQuery().trim()
                    ) {
                        const resolved = resolvePaginationPage(query.page, page.totalPages);
                        if (resolved !== query.page) {
                            this.pageIndex.set(resolved - 1);
                            this.writeListQuery({ ...this.queryState.current(), page: resolved }, true);
                        }
                    }
                }),
            );
    }

    private applyListQuery(query: LessonListQuery): void {
        this.categoryFilter.set(query.category);
        this.difficultyFilter.set(query.difficulty);
        this.searchQuery.set(query.search);
        this.sortOrder.set(query.sort);
        this.pageIndex.set(query.page - 1);
    }

    private writeListQuery(query: LessonListQuery, replaceUrl = false): void {
        void this.queryState.writeAsync(query, { replaceUrl }).catch(() => {
            /* Retain the successful list on navigation failure. */
        });
    }
}
