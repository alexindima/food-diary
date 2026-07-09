import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { UserService } from '../api/user.service';
import { LocalizationService } from '../i18n/localization.service';
import { MarketingAttributionService } from '../marketing/marketing-attribution.service';
import { ThemeService } from '../theme/theme.service';

@Service()
export class AppBootstrapService {
    private readonly authService = inject(AuthService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly marketingAttribution = inject(MarketingAttributionService);
    private readonly localizationService = inject(LocalizationService);
    private readonly themeService = inject(ThemeService);
    private readonly userService = inject(UserService);

    public initializeTheme(): void {
        this.themeService.initializeTheme();
    }

    public async initializeSessionAsync(): Promise<void> {
        await this.localizationService.initializeLocalizationAsync();
        await this.authService.restoreSessionAsync();

        if (!this.authService.isAuthenticated()) {
            return;
        }

        await this.localizationService.loadApplicationTranslationsAsync();
        const user = await firstValueFrom(this.userService.getInfoSilently());
        await this.localizationService.applyLanguagePreferenceAsync(user?.language ?? null);
        await this.localizationService.loadApplicationTranslationsAsync();
        this.themeService.syncWithUserPreferences(user?.theme, user?.uiStyle);
    }

    public initializeObservability(): void {
        this.marketingAttribution.initialize();
        this.frontendObservability.initialize();
    }
}
