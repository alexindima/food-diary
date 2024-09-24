import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfigService } from './app-config.service';

interface LoginRequest {
    email: string;
    password: string;
    rememberMe: boolean;
}

interface RegisterRequest {
    email: string;
    password: string;
    confirmPassword: string;
    agreeTerms: boolean;
}

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private readonly apiUrl: string;

    public constructor(
        private readonly appConfigService: AppConfigService,
        private readonly http: HttpClient
    ) {
        const appConfig = this.appConfigService.getConfig();
        this.apiUrl = appConfig.apiUrls.auth;
    }

    public login(data: LoginRequest): Observable<any> {
        return this.http.post(`${this.apiUrl}/login`, data);
    }

    public register(data: RegisterRequest): Observable<any> {
        return this.http.post(`${this.apiUrl}/register`, data);
    }
}
