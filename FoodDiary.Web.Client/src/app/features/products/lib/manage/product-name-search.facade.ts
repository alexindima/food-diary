import { inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { catchError, map, of, switchMap, timer } from 'rxjs';

import { NAME_SEARCH_DEBOUNCE_MS as NAME_SEARCH_DEBOUNCE_MS_TOKEN } from '../../../../config/runtime-ui.tokens';
import type { ProductSearchSuggestion } from '../../../../shared/models/product.data';
import { ProductService } from '../../api/product.service';
import type { ProductNameAutocompleteOption } from '../../components/manage/product-manage-lib/product-name-search.types';
import { PRODUCT_NAME_SEARCH_MIN_LENGTH, PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT } from '../product-manage.constants';

@Injectable()
export class ProductNameSearchFacade {
    private readonly productService = inject(ProductService);
    private readonly nameSearchDebounceMs = inject(NAME_SEARCH_DEBOUNCE_MS_TOKEN);
    private readonly request = signal<{ query: string } | undefined>(undefined);
    private readonly suggestions = rxResource({
        params: this.request,
        stream: ({ params }) =>
            timer(this.nameSearchDebounceMs).pipe(
                switchMap(() => this.productService.searchSuggestions(params.query, PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT)),
                map(suggestions => suggestions.map(suggestion => this.toProductNameOption(suggestion))),
                catchError(() => of<ProductNameAutocompleteOption[]>([])),
            ),
        defaultValue: [],
    });
    private readonly displayedOptions = linkedSignal(() => this.suggestions.value());

    public readonly options = this.displayedOptions.asReadonly();
    public readonly isLoading = this.suggestions.isLoading;

    public search(query: string): void {
        const trimmedQuery = query.trim();
        this.request.set(trimmedQuery.length < PRODUCT_NAME_SEARCH_MIN_LENGTH ? undefined : { query: trimmedQuery });
        this.displayedOptions.set([]);
    }

    public setSelectedSuggestion(suggestion: ProductSearchSuggestion): void {
        this.displayedOptions.set([this.toProductNameOption(suggestion)]);
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
