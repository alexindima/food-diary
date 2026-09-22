import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';

import { NutrientBadgesComponent } from '../../../../components/shared/nutrient-badges/nutrient-badges';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveMealImageUrl } from '../../../../shared/lib/meal-image.util';
import type { FavoriteMeal } from '../../models/meal.data';

const MAX_IMAGES = 4;

@Component({
    selector: 'fd-favorite-meal-row',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, NutrientBadgesComponent, LocalizedNumberPipe],
    templateUrl: './favorite-meal-row.html',
    styleUrl: './favorite-meal-row.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteMealRowComponent {
    public readonly meal = input.required<FavoriteMeal>();
    public readonly remove = output<FavoriteMeal>();
    public readonly removing = input(false);
    public readonly loading = input(false);
    public readonly disabled = input(false);
    public readonly add = output<FavoriteMeal>();
    protected readonly locale = injectCurrentLanguage();
    protected readonly name = computed(() => this.meal().name?.trim() ?? '');
    protected readonly composition = computed(() => this.meal().itemNames?.join(', ') ?? '');
    protected readonly typeKey = computed(() => {
        const type = this.meal().mealType?.toUpperCase() ?? '';
        return ['BREAKFAST', 'LUNCH', 'DINNER', 'SNACK'].includes(type) ? `MEAL_TYPES.${type}` : 'MEAL_LIST.FAVORITE_UNNAMED';
    });
    private readonly failedImages = signal<ReadonlySet<string>>(new Set());
    protected readonly images = computed(() => {
        const cover = this.meal().imageUrl?.trim();
        const failed = this.failedImages();
        if (cover !== undefined && cover.length > 0 && !failed.has(cover)) {
            return [cover];
        }
        return [...new Set(this.meal().itemImageUrls)].filter(url => url.trim().length > 0 && !failed.has(url)).slice(0, MAX_IMAGES);
    });
    protected readonly placeholder = computed(() => resolveMealImageUrl(null, this.meal().mealType));
    protected imageFailed(url: string): void {
        this.failedImages.update(current => new Set([...current, url]));
    }
}
