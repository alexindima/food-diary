import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { FEATURE_CATEGORIES } from './features.config';

@Component({
    selector: 'fd-features',
    imports: [TranslatePipe, FdUiIconComponent],
    templateUrl: './features.html',
    styleUrl: './features.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturesComponent {
    protected readonly categories = FEATURE_CATEGORIES;

    protected readonly activeCategoryKey = signal<string>(this.categories[0].key);
    protected readonly activeCategory = computed(
        () => this.categories.find(category => category.key === this.activeCategoryKey()) ?? this.categories[0],
    );

    protected selectCategory(categoryKey: string): void {
        this.activeCategoryKey.set(categoryKey);
    }
}
