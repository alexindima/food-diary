import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpRequestParams } from '../types/http-request.params';

export abstract class ApiService {
    private readonly http = inject(HttpClient);

    protected abstract readonly baseUrl: string;

    protected get<T>(endpoint: string, params?: HttpRequestParams, headers?: HttpHeaders): Observable<T> {
        const httpParams = this.buildHttpParams(params);
        return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params: httpParams, headers });
    }

    protected post<T>(endpoint: string, body: any, headers?: HttpHeaders): Observable<T> {
        return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, { headers });
    }

    protected put<T>(endpoint: string, body: any, headers?: HttpHeaders): Observable<T> {
        return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, { headers });
    }

    protected patch<T>(endpoint: string, body: any, headers?: HttpHeaders): Observable<T> {
        return this.http.patch<T>(`${this.baseUrl}/${endpoint}`, body, { headers });
    }

    protected delete<T>(endpoint: string, headers?: HttpHeaders): Observable<T> {
        return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, { headers });
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
