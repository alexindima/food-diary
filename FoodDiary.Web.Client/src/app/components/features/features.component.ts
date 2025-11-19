import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';


@Component({
    selector: 'fd-features',
    imports: [FdUiButtonComponent, TranslateModule],
    templateUrl: './features.component.html',
    styleUrl: './features.component.scss'
})
export class FeaturesComponent {
    public features: Feature[] = [
        {
            icon: 'tracking.webp',
            key: 'CALORIE_TRACKING',
        },
        {
            icon: 'planning.webp',
            key: 'MEAL_PLANNING',
        },
        {
            icon: 'analytics.webp',
            key: 'PROGRESS_ANALYTICS',
        },
        {
            icon: 'support.webp',
            key: 'COMMUNITY_SUPPORT',
        },
    ];
}

interface Feature {
    icon: string;
    key: string;
}
