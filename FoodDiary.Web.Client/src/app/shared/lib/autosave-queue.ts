import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

export class AutosaveQueue<T> {
    private readonly queue = new Subject<void>();
    private pendingValue!: T;
    private hasPendingValue = false;

    public constructor(
        destroyRef: DestroyRef,
        private readonly debounceMs: number,
        private readonly isBusy: () => boolean,
        private readonly persist: (value: T) => void,
    ) {
        this.queue.pipe(debounceTime(this.debounceMs), takeUntilDestroyed(destroyRef)).subscribe(() => this.flush());
    }

    public schedule(value: T): void {
        this.pendingValue = value;
        this.hasPendingValue = true;

        if (this.isBusy()) {
            return;
        }

        this.queue.next();
    }

    public flushNow(value: T): void {
        this.clear();
        this.persist(value);
    }

    public restore(value: T): void {
        if (this.hasPendingValue) {
            return;
        }

        this.pendingValue = value;
        this.hasPendingValue = true;
    }

    public scheduleIfPending(): void {
        if (!this.hasPendingValue) {
            return;
        }

        this.queue.next();
    }

    public hasPending(): boolean {
        return this.hasPendingValue;
    }

    private flush(): void {
        if (this.isBusy()) {
            return;
        }

        const value = this.takePending();
        if (value === null) {
            return;
        }

        this.persist(value);
    }

    private takePending(): T | null {
        if (!this.hasPendingValue) {
            return null;
        }

        const value = this.pendingValue;
        this.clear();
        return value;
    }

    private clear(): void {
        this.hasPendingValue = false;
    }
}

export function createAutosaveQueue<T>(options: {
    debounceMs: number;
    isBusy: () => boolean;
    persist: (value: T) => void;
}): AutosaveQueue<T> {
    return new AutosaveQueue<T>(inject(DestroyRef), options.debounceMs, options.isBusy, options.persist);
}
