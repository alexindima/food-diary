import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { form, FormField } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiLoaderComponent, FdUiPaginationComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { catchError, debounceTime, map, of, skip, throwError } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { hasInvalidPaginationPage, readPaginationPage, resolvePaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { PublicRecipeCardComponent } from '../../components/public-card/public-card';
import { PublicCategoryFilterComponent } from '../../components/public-category-filter/public-category-filter';
import { PublicRecipeNavigationComponent } from '../../components/public-navigation/public-navigation';
import { PublicCatalogFavorites } from '../../lib/public-catalog-favorites.facade';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { normalizeRecipeLanguage } from '../../lib/recipe-language.utils';
import type { PublicRecipe, PublicRecipeFilters } from '../../models/public-recipe.data';

type CatalogFilters = {
    search: string;
    category: string;
    maxTotalTime: string;
    sortBy: string;
    language: string;
};

type CatalogRequest = {
    query: PublicRecipeFilters;
    filters: CatalogFilters;
    filterKey: string;
    invalidPage: boolean;
};

function firstQueryValue(value: string | string[] | undefined): string | undefined {
    return Array.isArray(value) ? value[0] : value;
}

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
    public readonly search = input(undefined, { transform: firstQueryValue });
    public readonly category = input(undefined, { transform: firstQueryValue });
    public readonly maxTotalTime = input(undefined, { transform: firstQueryValue });
    public readonly sortBy = input(undefined, { transform: firstQueryValue });
    public readonly language = input(undefined, { transform: firstQueryValue });
    public readonly page = input(undefined, { transform: firstQueryValue });

    protected readonly favorites = inject(PublicCatalogFavorites);
    protected readonly timePresets = ['15', '30', '60'];
    protected readonly customTime = computed(() => {
        const value = this.positiveNumber(this.filters().maxTotalTime);
        return value !== null && !this.timePresets.includes(String(value)) ? value : null;
    });
    private readonly translate = inject(TranslateService);
    private readonly appLanguage = toSignal(this.translate.onLangChange.pipe(map(event => event.lang)), {
        initialValue: this.translate.getCurrentLang() ?? 'en',
    });
    protected readonly countKey = computed(
        () => `PUBLIC_RECIPES.COUNT_${new Intl.PluralRules(this.appLanguage()).select(this.result()?.totalItems ?? 0).toUpperCase()}`,
    );
    private readonly facade = inject(PublicRecipesFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly catalogRequest = computed<CatalogRequest>(() => {
        const page = this.page() ?? null;
        const fallbackLanguage = normalizeRecipeLanguage(this.appLanguage());
        const filters = {
            search: this.search() ?? '',
            category: this.category() ?? '',
            maxTotalTime: this.maxTotalTime() ?? '',
            sortBy: this.normalizeSort(this.sortBy()),
            language: this.catalogLanguage(this.language(), fallbackLanguage),
        };
        const queryFilters = this.resolveQueryFilters(filters);
        return {
            filters,
            query: { ...queryFilters, page: readPaginationPage(page) },
            filterKey: JSON.stringify(queryFilters),
            invalidPage: hasInvalidPaginationPage(page),
        };
    });
    protected readonly filters = linkedSignal(() => this.catalogRequest().filters);
    protected readonly filterForm = form(this.filters);
    private readonly searchDebounceMs = 350;
    private readonly catalog = rxResource({
        params: this.catalogRequest,
        stream: ({ params }) =>
            this.facade.query(params.query).pipe(
                map(result => this.resolveResult(params, result)),
                catchError((error: unknown) => (this.isCurrentRequest(params) ? throwError(() => error) : of(null))),
            ),
    });
    protected readonly result = computed(() => (this.catalog.hasValue() ? this.catalog.value() : null));
    protected readonly failed = computed(() => this.catalog.error() !== undefined);
    // Keep the loader visible until the URL reflects the latest filters and a valid page.
    protected readonly loading = computed(() => this.catalog.isLoading() || (!this.failed() && this.result() === null));
    protected readonly showCount = computed(() => !this.loading() && !this.failed() && this.result() !== null);

    public constructor() {
        toObservable(this.filters)
            .pipe(skip(1), debounceTime(this.searchDebounceMs), takeUntilDestroyed())
            .subscribe(filters => {
                if (JSON.stringify(this.resolveQueryFilters(filters)) !== this.catalogRequest().filterKey) {
                    this.changePage(0);
                }
            });
    }

    private resolveResult(request: CatalogRequest, result: PageOf<PublicRecipe>): PageOf<PublicRecipe> | null {
        if (!this.isCurrentRequest(request)) {
            return null;
        }
        const page = resolvePaginationPage(request.query.page, result.totalPages);
        if (request.invalidPage || page !== request.query.page) {
            void this.writePageAsync(page, true);
            return null;
        }
        return result;
    }

    private isCurrentRequest(request: CatalogRequest): boolean {
        return this.catalogRequest() === request && request.filterKey === JSON.stringify(this.resolveQueryFilters(this.filters()));
    }

    protected changePage(index: number): void {
        void this.writePageAsync(index + 1);
    }

    private async writePageAsync(page: number, replaceUrl = false): Promise<boolean> {
        const filters = this.resolveQueryFilters(this.filters());
        return this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {
                search: filters.search.length > 0 ? filters.search : null,
                category: filters.category.length > 0 ? filters.category : null,
                maxTotalTime: filters.maxTotalTime ?? null,
                sortBy: filters.sortBy === 'newest' ? null : filters.sortBy,
                language: filters.language ?? 'all',
                page: page > 1 ? page : null,
            },
            replaceUrl,
        });
    }

    protected setCategory(category: string): void {
        this.filters.update(filters => ({ ...filters, category }));
    }

    protected retry(): void {
        this.filters.set(this.catalogRequest().filters);
        this.catalog.reload();
    }

    protected reset(): void {
        this.filters.update(value => ({
            search: '',
            category: '',
            maxTotalTime: '',
            sortBy: value.sortBy,
            language: normalizeRecipeLanguage(this.appLanguage()),
        }));
        this.changePage(0);
    }

    private resolveQueryFilters(filters: CatalogFilters): Omit<PublicRecipeFilters, 'page'> {
        return {
            search: filters.search.trim(),
            category: filters.category.trim(),
            maxTotalTime: this.positiveNumber(filters.maxTotalTime) ?? undefined,
            sortBy: filters.sortBy,
            language: filters.language === '' ? undefined : filters.language,
        };
    }

    private catalogLanguage(value: string | undefined, fallback: string): string {
        return value === 'all' ? '' : value === 'en' || value === 'ru' ? value : fallback;
    }

    private normalizeSort(value: string | undefined): string {
        return value !== undefined && ['newest', 'oldest', 'fastest', 'slowest', 'name', 'name_desc'].includes(value) ? value : 'newest';
    }

    private positiveNumber(value: string | null): number | null {
        const number = Number(value);
        return Number.isInteger(number) && number > 0 ? number : null;
    }
}
