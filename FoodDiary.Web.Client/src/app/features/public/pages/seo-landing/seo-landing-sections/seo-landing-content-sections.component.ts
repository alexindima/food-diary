import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { SeoLandingFaqSectionKeys, SeoLandingSectionKeys } from '../lib/seo-landing.types';

@Component({
    selector: 'fd-seo-landing-content-sections',
    imports: [TranslatePipe],
    templateUrl: './seo-landing-content-sections.component.html',
    styleUrl: '../seo-landing-page/seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingContentSectionsComponent {
    public readonly audience = input.required<SeoLandingSectionKeys>();
    public readonly features = input.required<SeoLandingSectionKeys>();
    public readonly steps = input.required<SeoLandingSectionKeys>();
    public readonly faq = input.required<SeoLandingFaqSectionKeys>();
}
