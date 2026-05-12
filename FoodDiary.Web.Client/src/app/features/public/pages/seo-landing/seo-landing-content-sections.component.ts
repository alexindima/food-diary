import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { SeoLandingContentItemKeys, SeoLandingFaqItemKeys, SeoLandingTextKeys } from './seo-landing.types';

type SeoLandingContentSection = {
    items: readonly SeoLandingContentItemKeys[];
} & SeoLandingTextKeys;

type SeoLandingFaqSection = {
    items: readonly SeoLandingFaqItemKeys[];
} & SeoLandingTextKeys;

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
