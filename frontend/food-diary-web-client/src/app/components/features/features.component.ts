import { Component } from '@angular/core';
import { NgForOf } from '@angular/common';
import { TuiButton, TuiSurface, TuiTitle } from '@taiga-ui/core';
import { TuiCardLarge, TuiHeader } from '@taiga-ui/layout';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-features',
    standalone: true,
    imports: [NgForOf, TuiButton, TuiCardLarge, TuiSurface, TuiHeader, TuiTitle, TranslateModule],
    templateUrl: './features.component.html',
    styleUrl: './features.component.less',
})
export class FeaturesComponent {
    features: Feature[] = [
        {
            icon: 'tracking.png',
            key: 'CALORIE_TRACKING',
        },
        {
            icon: 'planning.png',
            key: 'MEAL_PLANNING',
        },
        {
            icon: 'analytics.png',
            key: 'PROGRESS_ANALYTICS',
        },
        {
            icon: 'support.png',
            key: 'COMMUNITY_SUPPORT',
        },
    ];
}

interface Feature {
    icon: string;
    key: string;
}
