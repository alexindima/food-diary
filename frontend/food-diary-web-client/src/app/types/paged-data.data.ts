import { signal } from '@angular/core';
import { PageOf } from './page-of.data';

export class PagedData<T> {
    public items = signal<T[]>([]);
    public isLoading = signal<boolean>(false);
    public currentPage = 1;
    public totalPages: number = 0;
    public totalItems: number = 0;

    public setData(pageData: PageOf<T>): void {
        this.items.set(pageData.data);
        this.currentPage = pageData.page;
        this.totalPages = pageData.totalPages;
        this.totalItems = pageData.totalItems;
    }

    public clearData(): void {
        this.items.set([]);
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalItems = 0;
    }

    public setLoading(isLoading: boolean): void {
        this.isLoading.set(isLoading);
    }
}
