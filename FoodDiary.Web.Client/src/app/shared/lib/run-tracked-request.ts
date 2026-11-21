import type { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ErrorObserver, finalize, type Observable } from 'rxjs';

export function runTrackedRequest<T>(
    destroyRef: DestroyRef,
    state: WritableSignal<boolean>,
    request$: Observable<T>,
    observer: ErrorObserver<T>,
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
            error: observer.error,
            complete: observer.complete,
        });
}
