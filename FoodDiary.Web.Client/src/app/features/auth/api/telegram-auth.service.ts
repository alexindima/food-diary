import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type { AuthResponse } from '../../../shared/auth/auth.data';
import type { TelegramConfiguration, TelegramIntent } from '../models/telegram-auth.data';

@Service()
export class TelegramAuthService extends ApiService {
    protected readonly baseUrl = `${environment.apiUrls.auth}/telegram`;

    public configuration(): Observable<TelegramConfiguration> {
        return this.get<TelegramConfiguration>('configuration');
    }

    public startOidc(link: boolean): Observable<{ authorizationUrl: string }> {
        return this.post<{ authorizationUrl: string }>(link ? 'oidc/start-link' : 'oidc/start', {});
    }

    public beginMiniApp(initData: string, link: boolean): Observable<TelegramIntent> {
        return this.post<TelegramIntent>(link ? 'mini-app/begin-link' : 'mini-app/begin', { initData });
    }

    public exchange(code: string, state: string): Observable<TelegramIntent> {
        return this.post<TelegramIntent>('oidc/exchange', { code, state });
    }

    public complete(
        ticket: string,
        action: 'login' | 'register' | 'link',
        language?: string,
        timeZoneId?: string,
    ): Observable<AuthResponse> {
        return action === 'link'
            ? this.post<AuthResponse>('complete-link', { ticket })
            : this.post<AuthResponse>('complete', { ticket, action, language, timeZoneId });
    }
}
