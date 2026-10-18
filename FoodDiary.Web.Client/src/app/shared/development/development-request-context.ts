import { HttpContext, HttpContextToken } from '@angular/common/http';

/** Correlation stays inside HttpClient; it is never a header or API payload. */
export const DEVELOPMENT_REQUEST_SCOPE = new HttpContextToken<((id: number) => void) | null>(() => null);

export function developmentRequestContext(bind: (id: number) => void): HttpContext {
    return new HttpContext().set(DEVELOPMENT_REQUEST_SCOPE, bind);
}
