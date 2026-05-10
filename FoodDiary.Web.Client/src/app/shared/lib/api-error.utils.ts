import { type Observable, of, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

function logApiError(message: string, error: unknown): void {
    if (!environment.enableGlobalErrorHandler) {
        // eslint-disable-next-line no-console -- Fallback logging when the global handler is disabled.
        console.error(message, error);
    }
}

export function rethrowApiError(message: string, error: unknown): Observable<never> {
    logApiError(message, error);
    return throwError(() => error);
}

export function fallbackApiError<T>(message: string, error: unknown, fallbackValue: T): Observable<T> {
    logApiError(message, error);
    return of(fallbackValue);
}
