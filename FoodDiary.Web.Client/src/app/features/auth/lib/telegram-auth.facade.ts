import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { TelegramAuthService } from '../api/telegram-auth.service';
import { isTelegramIntent, type TelegramConfiguration, type TelegramIntent } from '../models/telegram-auth.data';

const INTENT_STORAGE_KEY = 'fooddiary.telegram.intent';

@Injectable()
export class TelegramAuthFacade {
    private readonly api = inject(TelegramAuthService);
    private readonly auth = inject(AuthService);
    private readonly browser = inject(BrowserWindowService);
    private readonly storage = inject(BrowserStorageService);
    private readonly telegram = inject(TelegramWebAppService);

    public readonly configuration = signal<TelegramConfiguration | null>(null);
    public readonly intent = signal<TelegramIntent | null>(null);
    public readonly busy = signal(false);
    public readonly errorKey = signal<string | null>(null);

    public async loadConfigurationAsync(): Promise<void> {
        try {
            this.configuration.set(await firstValueFrom(this.api.configuration()));
        } catch {
            this.configuration.set(null);
        }
    }

    public restoreIntent(): void {
        const stored = this.storage.getJson('session', INTENT_STORAGE_KEY);
        if (isTelegramIntent(stored)) {
            this.intent.set(stored);
        } else {
            this.clearIntent();
        }
    }

    public clearIntent(): void {
        this.intent.set(null);
        this.storage.removeItem('session', INTENT_STORAGE_KEY);
    }

    public async beginAsync(link: boolean): Promise<void> {
        if (this.busy()) {
            return;
        }
        this.busy.set(true);
        this.errorKey.set(null);
        this.clearIntent();
        try {
            await this.telegram.initializeAsync();
            const initData = this.browser.getTelegramInitData();
            if (initData !== null) {
                this.saveIntent(await firstValueFrom(this.api.beginMiniApp(initData, link)));
            } else {
                const response = await firstValueFrom(this.api.startOidc(link));
                const url = new URL(response.authorizationUrl);
                if (url.origin !== 'https://oauth.telegram.org' || url.pathname !== '/auth') {
                    this.errorKey.set('AUTH.TELEGRAM.ERROR');
                    return;
                }
                this.browser.open(url.href, '_self');
            }
        } catch {
            this.errorKey.set('AUTH.TELEGRAM.ERROR');
        } finally {
            this.busy.set(false);
        }
    }

    public async exchangeAsync(code: string, state: string): Promise<void> {
        this.busy.set(true);
        this.errorKey.set(null);
        this.clearIntent();
        try {
            this.saveIntent(await firstValueFrom(this.api.exchange(code, state)));
        } catch {
            this.errorKey.set('AUTH.TELEGRAM.EXPIRED');
        } finally {
            this.busy.set(false);
        }
    }

    public async completeAsync(action: 'login' | 'register' | 'link', language?: string, timeZoneId?: string): Promise<boolean> {
        const intent = this.intent();
        if (intent === null || this.busy() || !isTelegramIntent(intent)) {
            this.errorKey.set('AUTH.TELEGRAM.EXPIRED');
            return false;
        }
        this.busy.set(true);
        this.errorKey.set(null);
        try {
            const response = await firstValueFrom(this.api.complete(intent.ticket, action, language, timeZoneId));
            this.auth.acceptExternalAuthentication(response);
            this.clearIntent();
            return true;
        } catch {
            this.errorKey.set('AUTH.TELEGRAM.ERROR');
            return false;
        } finally {
            this.busy.set(false);
        }
    }

    private saveIntent(intent: TelegramIntent): void {
        this.intent.set(intent);
        this.storage.setJson('session', INTENT_STORAGE_KEY, intent);
    }
}
