import { Component } from '@angular/core';
import { TuiButton, TuiSurface } from '@taiga-ui/core';
import { TuiCardLarge } from '@taiga-ui/layout';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-features',
    standalone: true,
    imports: [TuiButton, TuiCardLarge, TuiSurface, TranslateModule],
    templateUrl: './features.component.html',
    styleUrl: './features.component.less',
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
