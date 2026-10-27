import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { UserService } from '../api/user.service';
import { AuthRecoveryStateService } from '../auth/auth-recovery-state.service';
import { telegramMiniAppEntryUrl } from '../auth/telegram-mini-app-navigation';
import { TelegramWebAppService } from '../auth/telegram-web-app.service';
import { LocalizationService } from '../i18n/localization.service';
import { MarketingAttributionService } from '../marketing/marketing-attribution.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ThemeService } from '../theme/theme.service';

@Service()
export class AppBootstrapService {
    private readonly authService = inject(AuthService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly marketingAttribution = inject(MarketingAttributionService);
    private readonly localizationService = inject(LocalizationService);
    private readonly themeService = inject(ThemeService);
    private readonly userService = inject(UserService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly telegram = inject(TelegramWebAppService);
    private readonly recovery = inject(AuthRecoveryStateService);

    public initializeTheme(): void {
        this.themeService.initializeTheme();
    }

    public async initializeSessionAsync(): Promise<void> {
        const hadPendingLogout = this.recovery.pendingLogout() !== null;
        // Transferred state can resolve entirely in microtasks. Split the work
        // at phase boundaries so input and paint can run between them.
        await this.browserWindow.yieldToBrowserAsync();
        await this.localizationService.initializeLocalizationAsync();
        await this.browserWindow.yieldToBrowserAsync();
        await this.authService.restoreSessionAsync();
        await this.browserWindow.yieldToBrowserAsync();

        if (!this.authService.isAuthenticated()) {
            await this.routeMiniAppAsync(hadPendingLogout);
            return;
        }

        await this.localizationService.loadApplicationTranslationsAsync();
        const user = await firstValueFrom(this.userService.getInfoSilently());
        await this.localizationService.applyLanguagePreferenceAsync(user?.language ?? null);
        await this.localizationService.loadApplicationTranslationsAsync();
        this.themeService.syncWithUserPreferences(user?.theme, user?.uiStyle, user?.surfaceStyle);
    }

    private async routeMiniAppAsync(hadPendingLogout: boolean): Promise<void> {
        if (hadPendingLogout || this.recovery.issue() !== null || !(await this.telegram.isAutomaticSignInAllowedAsync())) {
            return;
        }
        const href = this.browserWindow.getHref();
        const entryUrl = href === null ? null : telegramMiniAppEntryUrl(href);
        if (entryUrl !== null) {
            this.browserWindow.replaceCurrentUrl(entryUrl);
        }
    }

    public initializeObservability(): void {
        this.marketingAttribution.initialize();
        this.frontendObservability.initialize();
    }
}
