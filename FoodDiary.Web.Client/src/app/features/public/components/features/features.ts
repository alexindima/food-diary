import { ChangeDetectionStrategy, Component, computed, type ElementRef, signal, viewChildren } from '@angular/core';
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
    private readonly categoryTabs = viewChildren<ElementRef<HTMLButtonElement>>('categoryTab');
    protected readonly categories = FEATURE_CATEGORIES;

    protected readonly activeCategoryKey = signal<string>(this.categories[0].key);
    protected readonly activeCategory = computed(
        () => this.categories.find(category => category.key === this.activeCategoryKey()) ?? this.categories[0],
    );

    protected selectCategory(categoryKey: string): void {
        this.activeCategoryKey.set(categoryKey);
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
        this.selectCategory(this.categories[nextIndex].key);
        this.categoryTabs()[nextIndex]?.nativeElement.focus();
    }
}
