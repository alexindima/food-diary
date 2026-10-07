import { HttpClient, HttpContext, HttpContextToken, type HttpEvent, type HttpHeaders, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';

import { Configuration } from './generated/configuration';

type SdkConstructor<T> = new (http: HttpClient, basePath: string | string[], configuration?: Configuration) => T;
const SDK_EXTRA_HEADERS = new HttpContextToken<HttpHeaders | null>(() => null);

/** The generated client delegates to the application's existing interceptor chain. */
export function createSdkConnection<T>(
    type: SdkConstructor<T>,
    baseUrl: string,
    http: HttpClient,
    withCredentials = true,
): { client: T; version: string } {
    const marker = '/api/v';
    const markerIndex = baseUrl.lastIndexOf(marker);
    if (markerIndex === -1) {
        throw new Error('API URL must include /api/v{version}.');
    }
    const basePath = baseUrl.slice(0, markerIndex);
    const version = baseUrl.slice(markerIndex + marker.length).split('/')[0];
    if (version.length === 0) {
        throw new Error('API URL must include a version.');
    }
    const transport = new HttpClient({
        handle: (request): Observable<HttpEvent<unknown>> => {
            let headers = request.headers;
            const extra = request.context.get(SDK_EXTRA_HEADERS);
            for (const key of extra?.keys() ?? []) {
                headers = headers.set(key, extra?.getAll(key) ?? []);
            }
            // ASP.NET query binding is case-insensitive. Retain the app's camel-case
            // names and ordinary decoded HttpParams while preserving repeated values.
            const decoded = new HttpParams({ fromString: request.params.toString() });
            let params = new HttpParams();
            for (const key of decoded.keys()) {
                const name = key.charAt(0).toLowerCase() + key.slice(1);
                for (const value of decoded.getAll(key) ?? []) {
                    params = params.append(name, value);
                }
            }
            return http.request(request.clone({ headers, params }));
        },
    });
    return { client: new type(transport, basePath, new Configuration({ withCredentials })), version };
}

/** Preserve per-call headers and existing contexts, including loading suppression. */
export function sdkRequestOptions(headers?: HttpHeaders, context = new HttpContext()): { context: HttpContext } {
    return { context: headers === undefined ? context : context.set(SDK_EXTRA_HEADERS, headers) };
}
