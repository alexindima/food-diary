import { ChangeDetectionStrategy, Component, computed, type ElementRef, signal, viewChildren } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { FEATURE_CATEGORIES, type FeatureCategory } from './features.config';

@Component({
    selector: 'fd-features',
    imports: [TranslatePipe, FdUiIconComponent],
    templateUrl: './features.html',
    styleUrl: './features.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturesComponent {
    private readonly categoryTabs = viewChildren<ElementRef<HTMLButtonElement>>('categoryTab');
    protected readonly categories = FEATURE_CATEGORIES;

    protected readonly activeCategory = signal(this.categories[0]);
    protected readonly activeCategoryKey = computed(() => this.activeCategory().key);

    protected selectCategory(category: FeatureCategory): void {
        this.activeCategory.set(category);
    }

    protected onCategoryKeydown(index: number, event: KeyboardEvent): void {
        let nextIndex: number;
        switch (event.key) {
            case 'ArrowRight': {
                nextIndex = (index + 1) % this.categories.length;
                break;
            }
            case 'ArrowLeft': {
                nextIndex = (index - 1 + this.categories.length) % this.categories.length;
                break;
            }
            case 'Home': {
                nextIndex = 0;
                break;
            }
            case 'End': {
                nextIndex = this.categories.length - 1;
                break;
            }
            default: {
                return;
            }
        }

        event.preventDefault();
        this.selectCategory(this.categories[nextIndex]);
        this.categoryTabs()[nextIndex]?.nativeElement.focus();
    }
}
