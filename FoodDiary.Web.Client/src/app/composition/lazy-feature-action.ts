import type { Injector, Type } from '@angular/core';
import { defer, type Observable, switchMap } from 'rxjs';

/** Load an owning implementation on demand, keeping one cold request per invocation. */
export function lazyFeatureAction<T, R>(
    injector: Injector,
    load: () => Promise<Type<T>>,
    invoke: (service: T) => Observable<R>,
): Observable<R> {
    let request: Observable<R> | undefined;
    return defer(
        () =>
            request ??
            defer(load).pipe(
                switchMap(type => {
                    // Re-subscription must retain request identity, including idempotency keys.
                    request ??= invoke(injector.get(type));
                    return request;
                }),
            ),
    );
}
