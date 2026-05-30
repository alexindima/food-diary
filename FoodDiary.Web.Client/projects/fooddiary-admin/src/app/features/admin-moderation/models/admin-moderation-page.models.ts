export type PagedResponse<T> = {
    items: T[];
    totalPages: number;
    totalItems: number;
};
