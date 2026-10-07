import { HttpClient, HttpContext, HttpContextToken, type HttpEvent, type HttpHeaders, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';

import { Configuration } from './generated/configuration';

type SdkConstructor<T> = new (http: HttpClient, basePath: string | string[], configuration?: Configuration) => T;
const SDK_EXTRA_HEADERS = new HttpContextToken<HttpHeaders | null>(() => null);
const SDK_EXTRA_PARAMS = new HttpContextToken<HttpParams | null>(() => null);

function requestHeaders(headers: HttpHeaders, extra: HttpHeaders | null): HttpHeaders {
    for (const key of extra?.keys() ?? []) {
        headers = headers.set(key, extra?.getAll(key) ?? []);
    }
    return headers;
}

function decodedParams(source: HttpParams): HttpParams {
    const decoded = new HttpParams({ fromString: source.toString() });
    let params = new HttpParams();
    for (const key of decoded.keys()) {
        const name = key.charAt(0).toLowerCase() + key.slice(1);
        for (const value of decoded.getAll(key) ?? []) {
            params = params.append(name, value);
        }
    }
    return params;
}

function requestParams(source: HttpParams, extra: HttpParams | null): HttpParams {
    let params = decodedParams(source);
    // Retain existing open filter records, including values outside the current schema.
    for (const key of extra?.keys() ?? []) {
        params = params.delete(key);
        for (const value of extra?.getAll(key) ?? []) {
            params = params.append(key, value);
        }
    }
    return params;
}

/** The generated client delegates to the application's existing interceptor chain. */
export function createSdkConnection<T>(
    type: SdkConstructor<T>,
    baseUrl: string,
    http: HttpClient,
    withCredentials = false,
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
            const headers = requestHeaders(request.headers, request.context.get(SDK_EXTRA_HEADERS));
            const params = requestParams(request.params, request.context.get(SDK_EXTRA_PARAMS));
            return http.request(request.clone({ headers, params }));
        },
    });
    return { client: new type(transport, basePath, new Configuration({ withCredentials })), version };
}

/** Preserve per-call headers and existing contexts, including loading suppression. */
export function sdkRequestOptions(
    headers?: HttpHeaders,
    params?: HttpParams | Record<string, string | number | boolean>,
    context = new HttpContext(),
): { context: HttpContext } {
    if (headers !== undefined) {
        context = context.set(SDK_EXTRA_HEADERS, headers);
    }
    if (params !== undefined) {
        context = context.set(SDK_EXTRA_PARAMS, params instanceof HttpParams ? params : new HttpParams({ fromObject: params }));
    }
    return { context };
}
