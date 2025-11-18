import { HttpClient } from '@angular/common/http';
import { catchError, Observable, tap, throwError } from 'rxjs';
import { AppConfig } from '../types/app.data';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class AppConfigService {
    private config: AppConfig | null = null;

    public constructor(private http: HttpClient) {}

    public loadConfig(): Observable<AppConfig> {
        return this.http.get<AppConfig>('/assets/config/app-config.json').pipe(
            tap((config: AppConfig) => {
                this.config = config;
            }),
            catchError((error) => {
                console.error('Failed to load app config', error);
                return throwError(() => new Error(error));
            })
        );
    }

    public getConfig(): AppConfig {
        if (!this.config) {
            throw new Error('App config is not loaded');
        }
        return this.config;
    }
}
