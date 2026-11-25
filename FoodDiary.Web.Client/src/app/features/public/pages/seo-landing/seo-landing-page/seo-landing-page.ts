import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { localizedPublicPath, publicSiteLocale } from '../../../../../config/public-site-paths';
import { buildSeoLandingViewModel, resolveSeoLandingPageData } from '../lib/seo-landing.mapper';
import { SeoLandingContentSectionsComponent } from '../seo-landing-sections/seo-landing-content-sections';
import { SeoLandingFooterSectionsComponent } from '../seo-landing-sections/seo-landing-footer-sections';

@Component({
    selector: 'fd-seo-landing-page',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, SeoLandingContentSectionsComponent, SeoLandingFooterSectionsComponent],
    templateUrl: './seo-landing-page.html',
    styleUrl: './seo-landing-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingPageComponent {
    public readonly seoPage = input.required({ transform: resolveSeoLandingPageData });

    private readonly router = inject(Router);
    protected readonly viewModel = computed(() => buildSeoLandingViewModel(this.seoPage()));
    protected readonly home = localizedPublicPath('/', publicSiteLocale(this.router.url));
}
