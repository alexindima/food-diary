import { inject, Injectable } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { AuthService } from '../../../../../services/auth.service';
import { LocalizationService } from '../../../../../services/localization.service';
import { LoginRequest, PasswordResetRequest, RegisterRequest, RestoreAccountRequest } from '../../../models/auth.data';
import type { GoogleLoginRequest } from '../../../models/google-auth.data';

export type AuthLoginResult = 'success' | 'invalidCredentials' | 'accountDeleted' | 'unknown';
export type AuthRegisterResult = 'success' | 'emailExists' | 'accountDeleted' | 'unknown';

@Injectable({ providedIn: 'root' })
export class AuthFlowFacade {
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);

    public login(formValue: Partial<LoginRequest>): Observable<AuthLoginResult> {
        return this.authService.login(new LoginRequest(formValue)).pipe(
            map(() => 'success' as const),
            catchError((error: unknown) => of(this.mapLoginError(this.getApiErrorCode(error)))),
        );
    }

    public restoreAccount(formValue: Partial<RestoreAccountRequest>, rememberMe: boolean): Observable<boolean> {
        return this.authService.restoreAccount(new RestoreAccountRequest(formValue), rememberMe).pipe(
            map(() => true),
            catchError(() => of(false)),
        );
    }

    public register(formValue: Partial<RegisterRequest>): Observable<AuthRegisterResult> {
        return this.authService
            .register(
                new RegisterRequest({
                    ...formValue,
                    language: this.localizationService.getCurrentLanguage(),
                }),
            )
            .pipe(
                map(() => 'success' as const),
                catchError((error: unknown) => of(this.mapRegisterError(this.getApiErrorCode(error)))),
            );
    }

    public loginWithGoogle(request: GoogleLoginRequest): Observable<boolean> {
        return this.authService.loginWithGoogle(request).pipe(
            map(() => true),
            catchError(() => of(false)),
        );
    }

    public requestPasswordReset(formValue: Partial<PasswordResetRequest>): Observable<boolean> {
        return this.authService.requestPasswordReset(new PasswordResetRequest(formValue)).pipe(
            map(() => true),
            catchError(() => of(false)),
        );
    }

    private mapLoginError(errorCode?: string): AuthLoginResult {
        if (errorCode === 'User.InvalidCredentials' || errorCode === 'Authentication.InvalidCredentials') {
            return 'invalidCredentials';
        }

        return errorCode === 'Authentication.AccountDeleted' ? 'accountDeleted' : 'unknown';
    }

    private mapRegisterError(errorCode?: string): AuthRegisterResult {
        if (errorCode === 'User.EmailAlreadyExists' || errorCode === 'Validation.Conflict') {
            return 'emailExists';
        }

        return errorCode === 'Authentication.AccountDeleted' ? 'accountDeleted' : 'unknown';
    }

    private getApiErrorCode(error: unknown): string | undefined {
        if (!this.isRecord(error)) {
            return undefined;
        }

        const responseBody = error['error'];
        return this.isRecord(responseBody) && typeof responseBody['error'] === 'string' ? responseBody['error'] : undefined;
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }
}
