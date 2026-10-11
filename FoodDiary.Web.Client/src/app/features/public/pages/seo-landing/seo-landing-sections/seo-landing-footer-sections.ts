import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { localizedPublicPath, publicSiteLocale } from '../../../../../config/public-site-paths';
import type { SeoLandingCtaKeys, SeoLandingRelatedPage } from '../lib/seo-landing.types';

@Component({
    selector: 'fd-seo-landing-footer-sections',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './seo-landing-footer-sections.html',
    styleUrl: './seo-landing-sections.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingFooterSectionsComponent {
    public readonly relatedPages = input.required<readonly SeoLandingRelatedPage[]>();
    public readonly cta = input.required<SeoLandingCtaKeys>();
    public readonly home = input('/');
    private readonly router = inject(Router);
    protected localizedPath(path: string): string {
        return localizedPublicPath(path, publicSiteLocale(this.router.url));
    }
}
