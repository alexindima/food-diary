import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth.data';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';
import { NavigationService } from './navigation.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
    providedIn: 'root',
})
export class AuthService extends ApiService {
    private readonly navigationService = inject(NavigationService);
    protected readonly baseUrl = environment.apiUrls.auth;

    private authTokenSignal = signal<string | null>(this.getToken());
    private userSignal = signal<string | null>(this.loadUserId());

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);

    public initializeAuth(): void {
        const token = this.getToken();
        if (!token) {
            this.clearUserId();
            return;
        }

        this.authTokenSignal.set(token);
        if (!this.userSignal()) {
            const resolvedUserId = this.extractUserIdFromToken(token);
            if (resolvedUserId) {
                this.setUserId(resolvedUserId);
                this.userSignal.set(resolvedUserId);
            }
        }
    }

    public login(data: LoginRequest): Observable<AuthResponse> {
        const loginData = { ...data, rememberMe: undefined };
        return this.post<AuthResponse>('login', loginData).pipe(
            tap(response => {
                this.onLogin(response, data.rememberMe || false);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Login error', error);
                return throwError(() => error);
            }),
        );
    }

    public register(data: RegisterRequest): Observable<AuthResponse> {
        return this.post<AuthResponse>('register', data).pipe(
            tap(response => {
                this.onLogin(response, false);
            }),
            catchError((error: HttpErrorResponse) => {
                console.error('Register error', error);
                return throwError(() => error);
            }),
        );
    }

    public refreshToken(): Observable<string | null> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            void this.onLogout(true);
            return of(null);
        }

        return this.post<{ accessToken: string }>('refresh', { refreshToken }).pipe(
            map(response => {
                const accessToken = response?.accessToken ?? null;
                if (accessToken) {
                    this.setToken(accessToken);
                    this.authTokenSignal.set(accessToken);
                }
                return accessToken;
            }),
            catchError(error => {
                console.error('refreshToken error', error);
                void this.onLogout(true);
                return of(null);
            }),
        );
    }

    public async onLogout(redirectToAuth = false): Promise<void> {
        this.authTokenSignal.set(null);
        this.userSignal.set(null);
        this.clearToken();
        this.clearUserId();
        this.clearRefreshToken();
        if (redirectToAuth) {
            await this.navigationService.navigateToAuth('login');
            return;
        }
        await this.navigationService.navigateToHome();
    }

    private onLogin(authResponse: AuthResponse, rememberMe: boolean): void {
        this.setToken(authResponse.accessToken, rememberMe);
        this.setRefreshToken(authResponse.refreshToken);
        this.authTokenSignal.set(authResponse.accessToken);

        const userId = authResponse.user?.id ?? this.extractUserIdFromToken(authResponse.accessToken);
        if (userId) {
            this.setUserId(userId);
            this.userSignal.set(userId);
        } else {
            console.warn('Auth response did not include user ID');
            this.clearUserId();
            this.userSignal.set(null);
        }
    }

    public getToken(): string | null {
        return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    }

    private setToken(token: string, rememberMe?: boolean): void {
        if (rememberMe !== undefined) {
            if (rememberMe) {
                localStorage.setItem('authToken', token);
                sessionStorage.removeItem('authToken');
            } else {
                sessionStorage.setItem('authToken', token);
                localStorage.removeItem('authToken');
            }
        } else {
            if (localStorage.getItem('authToken') !== null) {
                localStorage.setItem('authToken', token);
            } else {
                sessionStorage.setItem('authToken', token);
            }
        }
    }

    private clearToken(): void {
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
    }

    private setRefreshToken(token: string | null | undefined): void {
        if (!token) {
            this.clearRefreshToken();
            return;
        }
        localStorage.setItem('refreshToken', token);
    }

    private getRefreshToken(): string | null {
        const token = localStorage.getItem('refreshToken');
        if (!token || token === 'undefined' || token === 'null') {
            this.clearRefreshToken();
            return null;
        }
        return token;
    }

    private clearRefreshToken(): void {
        localStorage.removeItem('refreshToken');
    }

    public getUserId(): string | null {
        return this.userSignal();
    }

    private setUserId(userId: string | null): void {
        if (userId) {
            localStorage.setItem('userId', userId);
        } else {
            localStorage.removeItem('userId');
        }
    }

    private loadUserId(): string | null {
        const storedId = localStorage.getItem('userId');
        if (!storedId || storedId === 'undefined') {
            return null;
        }
        return storedId;
    }

    private clearUserId(): void {
        localStorage.removeItem('userId');
        sessionStorage.removeItem('userId');
    }

    private extractUserIdFromToken(token: string | null): string | null {
        if (!token) {
            return null;
        }

        const [, payloadSegment] = token.split('.');
        if (!payloadSegment) {
            return null;
        }

        try {
            const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = (4 - (normalized.length % 4 || 4)) % 4;
            const padded = normalized.padEnd(normalized.length + padLength, '=');
            const decoded = atob(padded);
            const payload = JSON.parse(decoded);
            return (
                payload['nameid'] ||
                payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                payload['sub'] ||
                null
            );
        } catch {
            return null;
        }
    }
}
