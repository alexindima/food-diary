export interface FdUiAutocompleteOption<T = unknown> {
    id?: string | number;
    value: T;
    label: string;
    hint?: string | null;
    badge?: string | null;
    data?: unknown;
}
