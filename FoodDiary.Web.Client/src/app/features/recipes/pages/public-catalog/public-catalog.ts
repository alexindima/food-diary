import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { form, FormField } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiLoaderComponent, FdUiPaginationComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { catchError, combineLatest, debounceTime, map, of, skip, startWith, Subject, switchMap, tap } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { PublicRecipeCardComponent } from '../../components/public-card/public-card';
import { PublicCategoryFilterComponent } from '../../components/public-category-filter/public-category-filter';
import { PublicRecipeNavigationComponent } from '../../components/public-navigation/public-navigation';
import { PublicCatalogFavorites } from '../../lib/public-catalog-favorites.facade';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { normalizeRecipeLanguage } from '../../lib/recipe-language.utils';
import type { PublicRecipe } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-recipe-catalog',
    templateUrl: './public-catalog.html',
    styleUrl: './public-catalog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [PublicRecipesFacade, PublicCatalogFavorites],
    imports: [
        PageBodyComponent,
        PublicRecipeCardComponent,
        PublicCategoryFilterComponent,
        FormField,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiSelectComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        PublicRecipeNavigationComponent,
    ],
})
export class PublicRecipeCatalogComponent {
    protected readonly favorites = inject(PublicCatalogFavorites);
    protected readonly timePresets = ['15', '30', '60'];
    protected readonly hasFilters = computed(
        () =>
            [this.filters().search, this.filters().category, this.filters().maxTotalTime].some(value => value.trim().length > 0) ||
            this.filters().language !== normalizeRecipeLanguage(this.language()),
    );
    protected readonly customTime = computed(() => {
        const value = this.positiveNumber(this.filters().maxTotalTime);
        return value !== null && !this.timePresets.includes(String(value)) ? value : null;
    });
    private readonly translate = inject(TranslateService);
    private readonly language = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: this.translate.getCurrentLang() ?? 'en',
    });
    protected readonly countKey = computed(
        () => `PUBLIC_RECIPES.COUNT_${new Intl.PluralRules(this.language()).select(this.result()?.totalItems ?? 0).toUpperCase()}`,
    );
    private readonly facade = inject(PublicRecipesFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly filters = signal({
        search: '',
        category: '',
        maxTotalTime: '',
        sortBy: 'newest',
        language: String(normalizeRecipeLanguage(this.language())),
    });
    protected readonly filterForm = form(this.filters);
    private readonly reload = new Subject<void>();
    private readonly searchDebounceMs = 350;
    protected readonly result = signal<PageOf<PublicRecipe> | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);
    protected readonly showCount = computed(() => !this.loading() && !this.failed() && this.result() !== null);

    public constructor() {
        combineLatest([this.route.queryParamMap, this.reload.pipe(startWith(undefined)), toObservable(this.language)])
            .pipe(
                tap(([params]) => {
                    this.filters.set({
                        search: params.get('search') ?? '',
                        category: params.get('category') ?? '',
                        maxTotalTime: params.get('maxTotalTime') ?? '',
                        sortBy: this.normalizeSort(params.get('sortBy')),
                        language: this.catalogLanguage(params.get('language')),
                    });
                    this.loading.set(true);
                    this.failed.set(false);
                }),
                switchMap(([params]) =>
                    this.facade
                        .query({
                            page: this.positiveNumber(params.get('page')) ?? 1,
                            ...this.filters(),
                            maxTotalTime: this.positiveNumber(this.filters().maxTotalTime) ?? undefined,
                            language: this.filters().language === '' ? undefined : this.filters().language,
                        })
                        .pipe(
                            catchError(() => {
                                this.failed.set(true);
                                return of(null);
                            }),
                        ),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(result => {
                this.result.set(result);
                this.loading.set(false);
            });
        toObservable(this.filters)
            .pipe(skip(1), debounceTime(this.searchDebounceMs), takeUntilDestroyed(this.destroyRef))
            .subscribe(filters => {
                const params = this.route.snapshot.queryParamMap;
                if (
                    filters.search !== (params.get('search') ?? '') ||
                    filters.category !== (params.get('category') ?? '') ||
                    filters.maxTotalTime !== (params.get('maxTotalTime') ?? '') ||
                    filters.sortBy !== this.normalizeSort(params.get('sortBy')) ||
                    filters.language !== this.catalogLanguage(params.get('language'))
                ) {
                    this.changePage(0);
                }
            });
    }

    protected changePage(index: number): void {
        const filters = this.filters();
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {
                search: filters.search.trim().length > 0 ? filters.search.trim() : null,
                category: filters.category.trim().length > 0 ? filters.category.trim() : null,
                maxTotalTime: this.positiveNumber(filters.maxTotalTime),
                sortBy: filters.sortBy === 'newest' ? null : filters.sortBy,
                language: filters.language === '' ? 'all' : filters.language,
                page: index > 0 ? index + 1 : null,
            },
        });
    }

    protected setCategory(category: string): void {
        this.filters.update(filters => ({ ...filters, category }));
    }

    protected retry(): void {
        this.reload.next();
    }

    protected reset(): void {
        this.filters.update(value => ({
            search: '',
            category: '',
            maxTotalTime: '',
            sortBy: value.sortBy,
            language: String(normalizeRecipeLanguage(this.language())),
        }));
    }

    private catalogLanguage(value: string | null): string {
        return value === 'all' ? '' : value === 'en' || value === 'ru' ? value : normalizeRecipeLanguage(this.language());
    }

    private normalizeSort(value: string | null): string {
        return value === 'fastest' || value === 'name' ? value : 'newest';
    }

    private positiveNumber(value: string | null): number | null {
        const number = Number(value);
        return Number.isInteger(number) && number > 0 ? number : null;
    }
}
