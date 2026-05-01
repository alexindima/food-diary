import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

export interface LandingFaqItem {
    questionKey: string;
    answerKey: string;
}

export interface LandingFaqGuide {
    path: string;
    labelKey: string;
}

@Component({
    selector: 'fd-landing-faq',
    imports: [RouterLink, TranslatePipe],
    templateUrl: './landing-faq.component.html',
    styleUrls: ['./landing-faq.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingFaqComponent {
    public readonly eyebrowKey = input('LANDING_FAQ.EYEBROW');
    public readonly titleKey = input('LANDING_FAQ.TITLE');
    public readonly subtitleKey = input('LANDING_FAQ.SUBTITLE');
    public readonly guidesTitleKey = input('LANDING_FAQ.GUIDES_TITLE');
    public readonly questions = input<readonly LandingFaqItem[]>([]);
    public readonly guides = input<readonly LandingFaqGuide[]>([]);
}
