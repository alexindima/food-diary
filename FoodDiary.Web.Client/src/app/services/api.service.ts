import { HttpClient, HttpContext, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpRequestParams } from '../shared/models/http-request.params';

type RequestBody = unknown;

export abstract class ApiService {
    private readonly http = inject(HttpClient);

    protected abstract readonly baseUrl: string;

    protected get<T>(endpoint: string, params?: HttpRequestParams, headers?: HttpHeaders, context?: HttpContext): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params: httpParams, headers, context });
    }

    protected post<T>(endpoint: string, body: RequestBody, headers?: HttpHeaders, params?: HttpRequestParams): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, { headers, params: httpParams });
    }

    protected put<T>(endpoint: string, body: RequestBody, headers?: HttpHeaders, params?: HttpRequestParams): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, { headers, params: httpParams });
    }

    protected patch<T>(endpoint: string, body: RequestBody, headers?: HttpHeaders, params?: HttpRequestParams): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body, { headers, params: httpParams });
    }

    protected delete<T>(endpoint: string, headers?: HttpHeaders, params?: HttpRequestParams): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, { headers, params: httpParams });
    }

    protected getBlob(endpoint: string, params?: HttpRequestParams): Observable<HttpResponse<Blob>> {
        const httpParams = this.buildHttpParams(params);
        return this.http.get(`${this.baseUrl}/${endpoint}`, {
            params: httpParams,
            responseType: 'blob',
            observe: 'response',
        });
    }

    private buildHttpParams(params?: HttpRequestParams): HttpParams | undefined {
        if (!params) {
            return undefined;
        }

        let httpParams = new HttpParams();
        Object.keys(params).forEach(key => {
            const value = params[key];
            if (value !== undefined && value !== null) {
                httpParams = httpParams.set(key, String(value));
            }
        });
        return httpParams;
    }
}
