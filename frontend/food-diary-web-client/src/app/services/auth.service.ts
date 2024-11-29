import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, Observable, of, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth.data';
import { environment } from '../../environments/environment';
import { ApiService } from './api.service';
import { ApiResponse } from '../types/api-response.data';
import { NavigationService } from './navigation.service';

@Injectable({
    providedIn: 'root',
})
export class AuthService extends ApiService {
    private readonly navigationService = inject(NavigationService);
    protected readonly baseUrl = environment.apiUrls.auth;

    private authTokenSignal = signal<string | null>(this.getToken());

    public readonly isAuthenticated = computed(() => this.authTokenSignal() !== null);

    public initializeAuth(): void {
        const token = this.getToken();
        if (token) {
            this.authTokenSignal.set(token);
        }
    }

    public login(data: LoginRequest): Observable<ApiResponse<AuthResponse | null>> {
        const loginData = { ...data, rememberMe: undefined };
        return this.post<ApiResponse<AuthResponse>>('login', loginData).pipe(
            tap(response => {
                if (response.status === 'success' && response.data) {
                    this.onLogin(response.data, data.rememberMe || false);
                }
            }),
            catchError(error => {
                return of(ApiResponse.error(error.error?.error, null));
            }),
        );
    }

    public register(data: RegisterRequest): Observable<ApiResponse<AuthResponse | null>> {
        return this.post<ApiResponse<AuthResponse>>('register', data).pipe(
            tap(response => {
                if (response.status === 'success' && response.data) {
                    this.onLogin(response.data, false);
                }
            }),
            catchError(error => {
                return of(ApiResponse.error(error.error?.error, null));
            }),
        );
    }

    public refreshToken(): Observable<AuthResponse | null> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            this.onLogout();
            return of(null);
        }

        return this.post<AuthResponse>('refresh', { refreshToken }).pipe(
            tap(response => {
                if (response) {
                    this.setToken(response.accessToken);
                    this.authTokenSignal.set(response.accessToken);
                }
            }),
            catchError(error => {
                console.error('refreshToken error', error);
                this.onLogout();
                return of(null);
            }),
        );
    }

    public async onLogout(): Promise<void> {
        this.authTokenSignal.set(null);
        this.clearToken();
        await this.navigationService.navigateToHome();
    }

    private onLogin(authResponse: AuthResponse, rememberMe: boolean): void {
        this.setToken(authResponse.accessToken, rememberMe);
        this.setRefreshToken(authResponse.refreshToken);
        this.authTokenSignal.set(authResponse.accessToken);
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

    private setRefreshToken(token: string): void {
        localStorage.setItem('refreshToken', token);
    }

    private getRefreshToken(): string | null {
        return localStorage.getItem('refreshToken');
    }
}
