import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { SeoLandingCtaKeys, SeoLandingRelatedPage } from '../lib/seo-landing.types';

@Component({
    selector: 'fd-seo-landing-footer-sections',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './seo-landing-footer-sections.component.html',
    styleUrl: '../seo-landing-page/seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingFooterSectionsComponent {
    public readonly relatedPages = input.required<readonly SeoLandingRelatedPage[]>();
    public readonly cta = input.required<SeoLandingCtaKeys>();
}
