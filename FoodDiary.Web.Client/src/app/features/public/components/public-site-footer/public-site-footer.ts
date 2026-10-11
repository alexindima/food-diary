import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { environment } from '../../../../../environments/environment';
import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';

@Component({
    selector: 'fd-public-site-footer',
    imports: [RouterLink, TranslatePipe],
    templateUrl: './public-site-footer.html',
    styleUrl: './public-site-footer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicSiteFooterComponent {
    public readonly path = input('/');
    protected readonly supportEmail = environment.supportEmail;
    protected readonly links = computed(() =>
        ['about', 'pricing', 'contact', 'privacy-policy', 'guides/how-to-keep-a-food-diary', 'guides/calories-in-homemade-meals'].map(
            path => ({
                path: localizedPublicPath(`/${path}`, publicSiteLocale(this.path())),
                label: `PUBLIC_SITE.FOOTER.${path.split('/').at(-1)?.replaceAll('-', '_').toUpperCase()}`,
            }),
        ),
    );
}
