import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

@Component({
    selector: 'fd-features',
    imports: [TranslateModule, FdUiIconComponent],
    templateUrl: './features.component.html',
    styleUrl: './features.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturesComponent {
    protected readonly categories: FeatureCategory[] = [
        {
            key: 'TRACK',
            icon: 'restaurant_menu',
            itemKeys: this.createItemKeys(['LOG_MEALS', 'PRODUCT_LIBRARY', 'RECIPE_FLOW']),
        },
        {
            key: 'PLAN',
            icon: 'event_note',
            itemKeys: this.createItemKeys(['MEAL_PLANS', 'SHOPPING_LISTS', 'GOALS']),
        },
        {
            key: 'PROGRESS',
            icon: 'bar_chart',
            itemKeys: this.createItemKeys(['STATISTICS', 'BODY_HISTORY', 'WEEKLY_CHECKINS']),
        },
        {
            key: 'SPECIAL',
            icon: 'health_and_safety',
            itemKeys: this.createItemKeys(['FASTING', 'CYCLE_TRACKING', 'PREMIUM_AI']),
        },
        {
            key: 'MOTIVATION',
            icon: 'school',
            itemKeys: this.createItemKeys(['LESSONS', 'GAMIFICATION', 'PROFILE_SYNC']),
        },
    ].map(category => ({
        ...category,
        tabId: `features-tab-${category.key}`,
        panelId: `features-panel-${category.key}`,
        labelKey: `FEATURES.CATEGORIES.${category.key}.LABEL`,
        eyebrowKey: `FEATURES.CATEGORIES.${category.key}.EYEBROW`,
        titleKey: `FEATURES.CATEGORIES.${category.key}.TITLE`,
        descriptionKey: `FEATURES.CATEGORIES.${category.key}.DESCRIPTION`,
    }));

    protected readonly activeCategoryKey = signal<string>(this.categories[0].key);
    protected readonly activeCategory = computed(
        () => this.categories.find(category => category.key === this.activeCategoryKey()) ?? this.categories[0],
    );

    protected selectCategory(categoryKey: string): void {
        this.activeCategoryKey.set(categoryKey);
    }

    private createItemKeys(keys: string[]): FeatureItem[] {
        return keys.map(key => ({
            key,
            kickerKey: `FEATURES.ITEMS.${key}.KICKER`,
            titleKey: `FEATURES.ITEMS.${key}.TITLE`,
            descriptionKey: `FEATURES.ITEMS.${key}.DESCRIPTION`,
        }));
    }
}

interface FeatureCategory {
    icon: string;
    key: string;
    labelKey: string;
    eyebrowKey: string;
    titleKey: string;
    descriptionKey: string;
    tabId: string;
    panelId: string;
    itemKeys: FeatureItem[];
}

interface FeatureItem {
    key: string;
    kickerKey: string;
    titleKey: string;
    descriptionKey: string;
}
