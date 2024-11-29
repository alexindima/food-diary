export interface PageOf<T> {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
}
