import { EMPTY, expand, reduce, type Observable } from 'rxjs';

export const COLLECTION_PAGE_SIZE = 100;

export function loadPagedCollection<T>(fetchPage: (page: number, limit: number) => Observable<T[]>): Observable<T[]> {
    return fetchPage(1, COLLECTION_PAGE_SIZE).pipe(
        expand((items, index) => (items.length === COLLECTION_PAGE_SIZE ? fetchPage(index + 2, COLLECTION_PAGE_SIZE) : EMPTY)),
        reduce((allItems, items) => [...allItems, ...items], [] as T[]),
    );
}
