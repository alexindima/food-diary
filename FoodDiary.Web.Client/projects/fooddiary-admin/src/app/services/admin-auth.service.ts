import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AdminAuthService {
  private readonly authUrl = environment.apiUrls.auth;
  private readonly tokenSignal = signal<string | null>(this.getToken());

  public constructor(private readonly http: HttpClient) {}

  public getToken(): string | null {
    return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
  }

  public isAuthenticated(): boolean {
    return Boolean(this.tokenSignal());
  }

  public isAdmin(): boolean {
    const token = this.tokenSignal();
    if (!token) {
      return false;
    }
    const roles = this.extractRolesFromToken(token);
    return roles.includes('Admin');
  }

  public refreshTokenState(): void {
    this.captureTokenFromQuery();
    this.tokenSignal.set(this.getToken());
  }

  public async applySsoFromQuery(): Promise<void> {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    if (!code) {
      return;
    }

    const success = await this.exchangeSsoCode(code);

    params.delete('code');
    if (!success) {
      params.set('ssoError', '1');
    }

    const nextQuery = params.toString();
    const nextUrl = nextQuery ? `${window.location.pathname}?${nextQuery}` : window.location.pathname;
    window.history.replaceState({}, '', nextUrl);
    this.tokenSignal.set(this.getToken());
  }

  public async tryApplySsoFromReturnUrl(returnUrl: string): Promise<string | null> {
    const result = this.extractCodeFromUrl(returnUrl);
    if (!result) {
      return null;
    }

    const success = await this.exchangeSsoCode(result.code);
    this.tokenSignal.set(this.getToken());
    return success ? result.cleanedUrl : null;
  }

  public async tryUpgradeToAdmin(): Promise<boolean> {
    if (this.isAdmin()) {
      return true;
    }

    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      const response = await firstValueFrom(this.http.post<AdminSsoStartResponse>(`${this.authUrl}/admin-sso/start`, {}));
      if (!response?.code) {
        return false;
      }

      const success = await this.exchangeSsoCode(response.code);
      this.tokenSignal.set(this.getToken());
      return success;
    } catch {
      return false;
    }
  }

  public async exchangeSsoCode(code: string): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.post<AuthenticationResponse>(`${this.authUrl}/admin-sso/exchange`, { code })
      );

      if (!response?.accessToken) {
        return false;
      }

      localStorage.setItem('authToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      return true;
    } catch {
      return false;
    }
  }

  private captureTokenFromQuery(): void {
    const params = new URLSearchParams(window.location.search);
    const token = params.get('authToken') || params.get('accessToken');
    if (!token) {
      return;
    }

    localStorage.setItem('authToken', token);
    params.delete('authToken');
    params.delete('accessToken');
    const nextQuery = params.toString();
    const nextUrl = nextQuery ? `${window.location.pathname}?${nextQuery}` : window.location.pathname;
    window.history.replaceState({}, '', nextUrl);
  }

  private extractRolesFromToken(token: string): string[] {
    const payload = this.decodePayload(token);
    if (!payload) {
      return [];
    }

    const roleClaim =
      payload['role'] ??
      payload['roles'] ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'];

    if (Array.isArray(roleClaim)) {
      return roleClaim.filter((role): role is string => typeof role === 'string');
    }

    if (typeof roleClaim === 'string') {
      return [roleClaim];
    }

    return [];
  }

  private extractCodeFromUrl(value: string): { code: string; cleanedUrl: string } | null {
    const decoded = this.safeDecode(value);
    if (!decoded) {
      return null;
    }

    try {
      const url = new URL(decoded, window.location.origin);
      const code = url.searchParams.get('code');
      if (!code) {
        return null;
      }

      url.searchParams.delete('code');
      const cleanedSearch = url.searchParams.toString();
      const cleanedUrl = cleanedSearch ? `${url.pathname}?${cleanedSearch}` : url.pathname;
      return { code, cleanedUrl };
    } catch {
      return null;
    }
  }

  private safeDecode(value: string): string {
    try {
      return decodeURIComponent(value);
    } catch {
      return value;
    }
  }

  private decodePayload(token: string): Record<string, unknown> | null {
    const [, payloadSegment] = token.split('.');
    if (!payloadSegment) {
      return null;
    }

    try {
      const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const padLength = (4 - (normalized.length % 4 || 4)) % 4;
      const padded = normalized.padEnd(normalized.length + padLength, '=');
      const decoded = atob(padded);
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}

type AuthenticationResponse = {
  accessToken: string;
  refreshToken: string;
};

type AdminSsoStartResponse = {
  code: string;
};
