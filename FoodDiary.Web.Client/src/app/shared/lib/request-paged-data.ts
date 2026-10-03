import { computed, type Signal } from '@angular/core';

import type { PageOf } from '../models/page-of.data';
import { RequestStateController } from './request-state';

export class RequestPagedData<T> {
    private readonly request = new RequestStateController<PageOf<T>>();
    public readonly state = this.request.state;
    public readonly error = this.request.error;
    public readonly items = computed(() => this.request.data()?.data ?? []);
    public readonly isLoading: Signal<boolean>;

    public constructor(extraLoading: Signal<boolean>) {
        this.isLoading = computed(() => this.request.isLoading() || extraLoading());
    }

    public get currentPage(): number {
        return this.request.data()?.page ?? 1;
    }
    public get totalPages(): number {
        return this.request.data()?.totalPages ?? 0;
    }
    public get totalItems(): number {
        return this.request.data()?.totalItems ?? 0;
    }

    public begin(): number {
        return this.request.begin();
    }
    public succeed(requestId: number, data: PageOf<T>): boolean {
        return this.request.succeed(requestId, data);
    }
    public fail(requestId: number, error: string): boolean {
        return this.request.fail(requestId, error, { preserveData: false });
    }
    public isCurrent(requestId: number): boolean {
        return this.request.isCurrent(requestId);
    }

    public setData(data: PageOf<T>): void {
        this.request.succeed(this.request.begin({ showLoading: false }), data);
    }

    public updateItems(update: (items: T[]) => T[]): void {
        this.request.updateData(data => ({ ...data, data: update(data.data) }));
    }
}
