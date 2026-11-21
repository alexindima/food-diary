import {
    HttpClient,
    HttpContext,
    HttpContextToken,
    type HttpEvent,
    type HttpHeaders,
    HttpParams,
    type HttpRequest,
} from '@angular/common/http';
import type { Observable } from 'rxjs';

import { Configuration } from './generated/configuration';

type SdkConstructor<T> = new (http: HttpClient, basePath: string | string[], configuration?: Configuration) => T;
const SDK_EXTRA_HEADERS = new HttpContextToken<HttpHeaders | null>(() => null);
type SdkConnectionOptions = Partial<Pick<HttpRequest<unknown>, 'keepalive' | 'priority' | 'withCredentials'>>;

function* httpEntries(source: HttpHeaders | HttpParams | null): Iterable<readonly [key: string, values: string[]]> {
    if (source === null) {
        return;
    }

    for (const key of source.keys()) {
        const values = source.getAll(key);
        yield [key, values !== null && values.length > 0 ? values : []];
    }
}

/** The generated client delegates to the application's existing interceptor chain. */
export function createSdkConnection<T>(
    type: SdkConstructor<T>,
    baseUrl: string,
    http: HttpClient,
    connectionOptions: boolean | SdkConnectionOptions = true,
): { client: T; version: string } {
    const options: SdkConnectionOptions =
        typeof connectionOptions === 'boolean' ? { withCredentials: connectionOptions } : connectionOptions;
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
            for (const [key, values] of httpEntries(extra)) {
                headers = headers.set(key, values);
            }
            // ASP.NET query binding is case-insensitive. Retain the app's camel-case
            // names and ordinary decoded HttpParams while preserving repeated values.
            const decoded = new HttpParams({ fromString: request.params.toString() });
            let params = new HttpParams();
            for (const [key, values] of httpEntries(decoded)) {
                const name = key.charAt(0).toLowerCase() + key.slice(1);
                for (const value of values) {
                    params = params.append(name, value);
                }
            }
            return http.request(request.clone({ headers, params, keepalive: options.keepalive, priority: options.priority }));
        },
    });
    return { client: new type(transport, basePath, new Configuration({ withCredentials: options.withCredentials ?? true })), version };
}

/** Preserve per-call headers and existing contexts, including loading suppression. */
export function sdkRequestOptions(headers?: HttpHeaders, context = new HttpContext()): { context: HttpContext } {
    return { context: headers === undefined ? context : context.set(SDK_EXTRA_HEADERS, headers) };
}
