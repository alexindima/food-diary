import { Component, computed, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'fd-features',
    imports: [TranslateModule, MatIconModule],
    templateUrl: './features.component.html',
    styleUrl: './features.component.scss',
})
export class FeaturesComponent {
    protected readonly categories: FeatureCategory[] = [
        {
            key: 'TRACK',
            icon: 'restaurant_menu',
            itemKeys: ['LOG_MEALS', 'PRODUCT_LIBRARY', 'RECIPE_FLOW'],
        },
        {
            key: 'PLAN',
            icon: 'event_note',
            itemKeys: ['MEAL_PLANS', 'SHOPPING_LISTS', 'GOALS'],
        },
        {
            key: 'PROGRESS',
            icon: 'monitoring',
            itemKeys: ['STATISTICS', 'BODY_HISTORY', 'WEEKLY_CHECKINS'],
        },
        {
            key: 'SPECIAL',
            icon: 'health_and_safety',
            itemKeys: ['FASTING', 'CYCLE_TRACKING', 'PREMIUM_AI'],
        },
        {
            key: 'MOTIVATION',
            icon: 'school',
            itemKeys: ['LESSONS', 'GAMIFICATION', 'PROFILE_SYNC'],
        },
    ];

    protected readonly activeCategoryKey = signal<string>(this.categories[0].key);
    protected readonly activeCategory = computed(
        () => this.categories.find(category => category.key === this.activeCategoryKey()) ?? this.categories[0],
    );

    protected selectCategory(categoryKey: string): void {
        this.activeCategoryKey.set(categoryKey);
    }
}

interface FeatureCategory {
    icon: string;
    key: string;
    itemKeys: string[];
}
