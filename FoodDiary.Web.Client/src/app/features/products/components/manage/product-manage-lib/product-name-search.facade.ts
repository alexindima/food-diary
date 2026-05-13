import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, map, of, Subject, switchMap } from 'rxjs';

import { NAME_SEARCH_DEBOUNCE_MS as NAME_SEARCH_DEBOUNCE_MS_TOKEN } from '../../../../../config/runtime-ui.tokens';
import { ProductService } from '../../../api/product.service';
import { PRODUCT_NAME_SEARCH_MIN_LENGTH, PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT } from '../../../lib/product-manage.constants';
import type { ProductSearchSuggestion } from '../../../models/product.data';
import type { ProductNameAutocompleteOption } from './product-name-search.types';

@Injectable({ providedIn: 'root' })
export class ProductNameSearchFacade {
    private readonly productService = inject(ProductService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly nameSearchDebounceMs = inject(NAME_SEARCH_DEBOUNCE_MS_TOKEN);
    private readonly nameQuery$ = new Subject<string>();

    public readonly options = signal<ProductNameAutocompleteOption[]>([]);
    public readonly isLoading = signal(false);

    public constructor() {
        this.bindNameSearch();
    }

    public search(query: string): void {
        this.nameQuery$.next(query);
    }

    public setSelectedSuggestion(suggestion: ProductSearchSuggestion): void {
        this.options.set([this.toProductNameOption(suggestion)]);
    }

    private bindNameSearch(): void {
        this.nameQuery$
            .pipe(
                debounceTime(this.nameSearchDebounceMs),
                switchMap(query => {
                    const trimmed = query.trim();
                    if (trimmed.length < PRODUCT_NAME_SEARCH_MIN_LENGTH) {
                        this.isLoading.set(false);
                        this.options.set([]);
                        return of<ProductNameAutocompleteOption[]>([]);
                    }

                    this.isLoading.set(true);
                    return this.productService
                        .searchSuggestions(trimmed, PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT)
                        .pipe(map(suggestions => suggestions.map(suggestion => this.toProductNameOption(suggestion))))
                        .pipe(catchError(() => of<ProductNameAutocompleteOption[]>([])));
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(options => {
                this.options.set(options);
                this.isLoading.set(false);
            });
    }

    private toProductNameOption(suggestion: ProductSearchSuggestion): ProductNameAutocompleteOption {
        return {
            id:
                suggestion.source === 'usda'
                    ? `usda:${suggestion.usdaFdcId ?? suggestion.name}`
                    : `open-food-facts:${suggestion.barcode ?? suggestion.name}`,
            value: suggestion.name,
            label: suggestion.name,
            hint: suggestion.brand ?? suggestion.category ?? suggestion.barcode,
            badge: suggestion.source === 'usda' ? 'USDA' : 'Open Food Facts',
            data: suggestion,
        };
    }
}
