import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';


@Component({
    selector: 'fd-features',
    imports: [TranslateModule],
    templateUrl: './features.component.html',
    styleUrl: './features.component.scss'
})
export class FeaturesComponent {
    public features: Feature[] = [
        {
            icon: '📊',
            key: 'CALORIE_TRACKING',
        },
        {
            icon: '📅',
            key: 'MEAL_PLANNING',
        },
        {
            icon: '📈',
            key: 'PROGRESS_ANALYTICS',
        },
        {
            icon: '🤝',
            key: 'COMMUNITY_SUPPORT',
        },
    ];
}

interface Feature {
    icon: string;
    key: string;
}
