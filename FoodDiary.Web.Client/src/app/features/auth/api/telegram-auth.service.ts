import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { authResponseFromSdk } from '../../../shared/api/sdk/auth-sdk.mapper';
import { TelegramAuthSdk } from '../../../shared/api/sdk/generated/api/telegram-auth.service';
import type { TelegramAuthenticationIntentHttpResponse } from '../../../shared/api/sdk/generated/model/telegram-authentication-intent-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import type { AuthResponse } from '../../../shared/auth/auth.data';
import {
    type TelegramLoginTicket,
    telegramLoginTicket,
    type TelegramOAuthCode,
    type TelegramOAuthState,
} from '../../../shared/auth/telegram-oidc-values';
import type { TelegramConfiguration, TelegramIntent } from '../models/telegram-auth.data';

@Service()
export class TelegramAuthService {
    protected readonly baseUrl = `${environment.apiUrls.auth}/telegram`;
    private readonly sdk = createSdkConnection(TelegramAuthSdk, this.baseUrl, inject(HttpClient));

    public configuration(): Observable<TelegramConfiguration> {
        return this.sdk.client
            .getAuthTelegramConfiguration({ version: this.sdk.version })
            .pipe(map(value => requireSdkFields(value, ['loginEnabled', 'registrationEnabled', 'oidcEnabled'])));
    }

    public startOidc(link: boolean): Observable<{ authorizationUrl: string }> {
        const params = { version: this.sdk.version };
        return (link ? this.sdk.client.postAuthTelegramOidcStartLink(params) : this.sdk.client.postAuthTelegramOidcStart(params)).pipe(
            map(value => requireSdkFields(value, ['authorizationUrl'])),
        );
    }

    public beginMiniApp(initData: string, link: boolean): Observable<TelegramIntent> {
        const params = { version: this.sdk.version, telegramAuthHttpRequest: { initData } };
        return (
            link ? this.sdk.client.postAuthTelegramMiniAppBeginLink(params) : this.sdk.client.postAuthTelegramMiniAppBegin(params)
        ).pipe(map(intentFromSdk));
    }

    public exchange(code: TelegramOAuthCode, state: TelegramOAuthState): Observable<TelegramIntent> {
        return this.sdk.client
            .postAuthTelegramOidcExchange({ version: this.sdk.version, exchangeTelegramOidcHttpRequest: { code, state } })
            .pipe(map(intentFromSdk));
    }

    public complete(
        ticket: TelegramLoginTicket,
        action: 'login' | 'register' | 'link',
        language?: string,
        timeZoneId?: string,
    ): Observable<AuthResponse> {
        return (
            action === 'link'
                ? this.sdk.client.postAuthTelegramCompleteLink({ version: this.sdk.version, completeTelegramLinkHttpRequest: { ticket } })
                : this.sdk.client.postAuthTelegramComplete({
                      version: this.sdk.version,
                      completeTelegramAuthenticationHttpRequest: { ticket, action, language, timeZoneId },
                  })
        ).pipe(map(authResponseFromSdk));
    }
}

function intentFromSdk(response: TelegramAuthenticationIntentHttpResponse): TelegramIntent {
    const value = requireSdkFields(response, ['ticket', 'nextAction', 'expiresAtUtc']);
    return {
        ...value,
        ticket: telegramLoginTicket(value.ticket),
        nextAction: sdkEnum(value.nextAction, ['login', 'link', 'onboarding'] as const),
    };
}
