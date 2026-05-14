import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { buildSeoLandingViewModel, resolveSeoLandingPageData } from '../lib/seo-landing.mapper';
import { SeoLandingContentSectionsComponent } from '../seo-landing-sections/seo-landing-content-sections.component';
import { SeoLandingFooterSectionsComponent } from '../seo-landing-sections/seo-landing-footer-sections.component';

@Component({
    selector: 'fd-seo-landing-page',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, SeoLandingContentSectionsComponent, SeoLandingFooterSectionsComponent],
    templateUrl: './seo-landing-page.component.html',
    styleUrl: './seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly page = resolveSeoLandingPageData(this.route.snapshot.data['seoPage']);
    private readonly viewModel = buildSeoLandingViewModel(this.page);

    protected readonly baseKey = this.viewModel.baseKey;
    protected readonly hero = this.viewModel.hero;
    protected readonly audience = this.viewModel.audience;
    protected readonly features = this.viewModel.features;
    protected readonly steps = this.viewModel.steps;
    protected readonly faq = this.viewModel.faq;
    protected readonly cta = this.viewModel.cta;
    protected readonly relatedPages = this.viewModel.relatedPages;
}
