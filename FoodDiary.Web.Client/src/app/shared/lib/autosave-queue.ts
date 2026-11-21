import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, takeUntil, timer } from 'rxjs';

export class AutosaveQueue<T> {
    private readonly queue = new Subject<void>();
    private readonly cancelPendingFlush = new Subject<void>();
    private pending: { value: T } | null = null;

    public constructor(
        destroyRef: DestroyRef,
        private readonly debounceMs: number,
        private readonly isBusy: () => boolean,
        private readonly persist: (value: T) => void,
    ) {
        this.queue
            .pipe(
                switchMap(() => timer(this.debounceMs).pipe(takeUntil(this.cancelPendingFlush))),
                takeUntilDestroyed(destroyRef),
            )
            .subscribe(() => {
                this.flush();
            });
    }

    public schedule(value: T): void {
        this.pending = { value };

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
        if (this.pending !== null) {
            return;
        }

        this.pending = { value };
    }

    public scheduleIfPending(): void {
        if (this.pending === null) {
            return;
        }

        this.queue.next();
    }

    public hasPending(): boolean {
        return this.pending !== null;
    }

    public clearPending(): void {
        this.clear();
    }

    private flush(): void {
        if (this.isBusy() || this.pending === null) {
            return;
        }

        const { value } = this.pending;
        this.clear();
        this.persist(value);
    }

    private clear(): void {
        this.pending = null;
        this.cancelPendingFlush.next();
    }
}

export function createAutosaveQueue<T>(options: {
    debounceMs: number;
    isBusy: () => boolean;
    persist: (value: T) => void;
}): AutosaveQueue<T> {
    return new AutosaveQueue<T>(inject(DestroyRef), options.debounceMs, options.isBusy, options.persist);
}
