import { ChangeDetectionStrategy, Component, computed, inject, input, output, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import { environment } from '../../../../../environments/environment';
import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';
import { MARKETING_ATTRIBUTION_ENABLED } from '../../../../shared/marketing/marketing-attribution-enabled';

@Component({
    selector: 'fd-public-site-footer',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './public-site-footer.html',
    styleUrl: './public-site-footer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicSiteFooterComponent {
    public readonly path = input('/');
    public readonly marketingSettingsRequested = output();
    private readonly marketingSettingsButton = viewChild(FdUiButtonComponent);
    protected readonly marketingAttributionEnabled = inject(MARKETING_ATTRIBUTION_ENABLED);
    protected readonly supportEmail = environment.supportEmail;
    protected readonly links = computed(() =>
        ['about', 'pricing', 'contact', 'privacy-policy', 'guides/how-to-keep-a-food-diary', 'guides/calories-in-homemade-meals'].map(
            path => ({
                path: localizedPublicPath(`/${path}`, publicSiteLocale(this.path())),
                label: `PUBLIC_SITE.FOOTER.${path.split('/').at(-1)?.replaceAll('-', '_').toUpperCase()}`,
            }),
        ),
    );

    // eslint-disable-next-line local/prefer-protected-template-members -- Shell restores focus after the requested consent notice closes.
    public focusMarketingSettings(): void {
        this.marketingSettingsButton()?.focus();
    }
}
