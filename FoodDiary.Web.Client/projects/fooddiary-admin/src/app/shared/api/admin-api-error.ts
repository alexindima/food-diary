import { HttpErrorResponse } from '@angular/common/http';

export function adminApiErrorCode(error: unknown): string | null {
    if (!(error instanceof HttpErrorResponse)) {
        return null;
    }
    const payload: unknown = error.error;
    if (typeof payload !== 'object' || payload === null || !('error' in payload)) {
        return null;
    }
    return typeof payload.error === 'string' ? payload.error : null;
}
