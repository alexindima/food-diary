import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';

@Component({
    selector: 'fd-public-site-header',
    imports: [RouterLink, RouterLinkActive, TranslatePipe, FdUiButtonComponent],
    templateUrl: './public-site-header.html',
    styleUrl: './public-site-header.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicSiteHeaderComponent {
    public readonly path = input('/');
    protected readonly locale = computed(() => publicSiteLocale(this.path()));
    protected readonly home = computed(() => localizedPublicPath('/', this.locale()));
    protected readonly links = computed(() =>
        ['food-diary', 'ai-calorie-tracker', 'pricing', 'about'].map(path => ({
            path: localizedPublicPath(`/${path}`, this.locale()),
            label: `PUBLIC_SITE.NAV.${path.replaceAll('-', '_').toUpperCase()}`,
        })),
    );
    protected readonly languages = computed(() => [
        { label: 'EN', lang: 'en', path: localizedPublicPath(this.path(), 'en') },
        { label: 'RU', lang: 'ru', path: localizedPublicPath(this.path(), 'ru') },
    ]);
}
