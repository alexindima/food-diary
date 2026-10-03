import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiDialogService,
    FdUiIconComponent,
    FdUiMenuComponent,
    FdUiMenuItemComponent,
    FdUiMenuTriggerDirective,
} from 'fd-ui-kit';

import { AuthService } from '../../../../services/auth.service';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { ThemeService } from '../../../../shared/theme/theme.service';
import { PublicAuthDialogService } from '../../../public/contracts/auth-dialog';

@Component({
    selector: 'fd-public-recipe-navigation',
    host: { '[style.display]': "auth.isAuthenticated() ? 'none' : 'block'" },
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        RouterLink,
        RouterLinkActive,
        TranslatePipe,
        UpperCasePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    templateUrl: './public-navigation.html',
    styleUrl: './public-navigation.scss',
})
export class PublicRecipeNavigationComponent {
    protected readonly theme = inject(ThemeService);
    protected readonly language = injectCurrentLanguage();
    protected readonly themeIcon = computed(() => ({ ocean: 'water', leaf: 'eco', dark: 'dark_mode' })[this.theme.theme()]);
    private readonly localization = inject(LocalizationService);
    protected readonly auth = inject(AuthService);
    private readonly dialog = inject(PublicAuthDialogService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);

    protected async changeLanguageAsync(language: string): Promise<void> {
        await this.localization.applyLanguagePreferenceAsync(language);
        await this.localization.loadTranslationsForRouteAsync(this.router.url);
    }

    protected async openAppearanceDialogAsync(): Promise<void> {
        const { AppearanceDialogComponent } = await import('../../../../components/shared/appearance-dialog/appearance-dialog');
        this.dialogService.open(AppearanceDialogComponent, {
            size: 'md',
            data: { theme: this.theme.theme(), uiStyle: this.theme.uiStyle(), persistence: 'local' },
        });
    }

    protected login(): void {
        void this.dialog.openAsync({ mode: 'login', returnUrl: this.router.url, destroyRef: this.destroyRef });
    }
}
