import type { FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete';

import type { ProductSearchSuggestion } from '../../../models/product.data';

export type ProductNameSuggestion = ProductSearchSuggestion;

export type ProductNameAutocompleteOption = FdUiAutocompleteOption<string> & { data: ProductNameSuggestion };
