// Matches PaginationPolicy.MaxPageNumber and PresentationQueryLimits.MaximumPage in the API.
export const API_MAX_PAGE_NUMBER = 10_000;

export function readPaginationPage(value: string | null): number {
    const page = Number(value);
    return Number.isSafeInteger(page) && page >= 1 && page <= API_MAX_PAGE_NUMBER ? page : 1;
}

export function hasInvalidPaginationPage(value: string | null): boolean {
    return value !== null && Number(value) !== readPaginationPage(value);
}

export function resolvePaginationPage(page: number, totalPages: number): number {
    const lastPage = Math.max(1, Math.min(totalPages, API_MAX_PAGE_NUMBER));
    return Math.min(page, lastPage);
}
