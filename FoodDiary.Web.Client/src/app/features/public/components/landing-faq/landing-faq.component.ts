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
    public readonly questions = input.required<readonly LandingFaqItem[]>();
    public readonly guides = input.required<readonly LandingFaqGuide[]>();
}
