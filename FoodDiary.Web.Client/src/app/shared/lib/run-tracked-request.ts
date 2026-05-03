import { type DestroyRef, type WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, type Observable, type PartialObserver } from 'rxjs';

export function runTrackedRequest<T>(
    destroyRef: DestroyRef,
    state: WritableSignal<boolean>,
    request$: Observable<T>,
    observer: PartialObserver<T>,
): void {
    state.set(true);
    request$
        .pipe(
            finalize(() => {
                state.set(false);
            }),
            takeUntilDestroyed(destroyRef),
        )
        .subscribe({
            next: observer.next,
            error: observer.error ?? ((): void => undefined),
            complete: observer.complete,
        });
}
