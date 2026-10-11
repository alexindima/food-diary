import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';

export type LandingFaqItem = {
    questionKey: string;
    answerKey: string;
};

export type LandingFaqGuide = {
    path: string;
    labelKey: string;
};

@Component({
    selector: 'fd-landing-faq',
    imports: [RouterLink, TranslatePipe],
    templateUrl: './landing-faq.html',
    styleUrls: ['./landing-faq.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingFaqComponent {
    public readonly questions = input.required<readonly LandingFaqItem[]>();
    public readonly guides = input.required<readonly LandingFaqGuide[]>();
    private readonly router = inject(Router);
    protected localizedPath(path: string): string {
        return localizedPublicPath(path, publicSiteLocale(this.router.url));
    }
}
