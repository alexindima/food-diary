import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { SeoLandingContentItemKeys, SeoLandingFaqItemKeys, SeoLandingTextKeys } from './seo-landing-page.component';

interface SeoLandingContentSection extends SeoLandingTextKeys {
    items: readonly SeoLandingContentItemKeys[];
}

interface SeoLandingFaqSection extends SeoLandingTextKeys {
    items: readonly SeoLandingFaqItemKeys[];
}

@Component({
    selector: 'fd-seo-landing-content-sections',
    imports: [TranslatePipe],
    templateUrl: './seo-landing-content-sections.component.html',
    styleUrl: './seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingContentSectionsComponent {
    public readonly audience = input.required<SeoLandingContentSection>();
    public readonly features = input.required<SeoLandingContentSection>();
    public readonly steps = input.required<SeoLandingContentSection>();
    public readonly faq = input.required<SeoLandingFaqSection>();
}
