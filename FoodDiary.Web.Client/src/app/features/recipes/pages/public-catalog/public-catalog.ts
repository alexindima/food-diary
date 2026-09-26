import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, FormField } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiLoaderComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { catchError, combineLatest, debounceTime, of, skip, startWith, Subject, switchMap, tap } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { PublicRecipeCardComponent } from '../../components/public-card/public-card';
import { PublicRecipeNavigationComponent } from '../../components/public-navigation/public-navigation';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import type { PublicRecipe } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-recipe-catalog',
    templateUrl: './public-catalog.html',
    styleUrl: './public-catalog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [PublicRecipesFacade],
    imports: [
        PageBodyComponent,
        PublicRecipeCardComponent,
        FormField,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        PublicRecipeNavigationComponent,
    ],
})
export class PublicRecipeCatalogComponent {
    private readonly facade = inject(PublicRecipesFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly filters = signal({ search: '', category: '', maxTotalTime: '' });
    protected readonly filterForm = form(this.filters);
    private readonly reload = new Subject<void>();
    private readonly searchDebounceMs = 350;
    protected readonly result = signal<PageOf<PublicRecipe> | null>(null);
    protected readonly loading = signal(true);
    protected readonly failed = signal(false);

    public constructor() {
        combineLatest([this.route.queryParamMap, this.reload.pipe(startWith(undefined))])
            .pipe(
                tap(([params]) => {
                    this.filters.set({
                        search: params.get('search') ?? '',
                        category: params.get('category') ?? '',
                        maxTotalTime: params.get('maxTotalTime') ?? '',
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
                    filters.maxTotalTime !== (params.get('maxTotalTime') ?? '')
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
                page: index > 0 ? index + 1 : null,
            },
        });
    }

    protected retry(): void {
        this.reload.next();
    }

    protected reset(): void {
        this.filters.set({ search: '', category: '', maxTotalTime: '' });
    }

    private positiveNumber(value: string | null): number | null {
        const number = Number(value);
        return Number.isInteger(number) && number > 0 ? number : null;
    }
}
