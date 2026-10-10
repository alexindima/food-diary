import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { createImageFailureState, FdUiButtonComponent, FdUiFocusWhenDirective, FdUiHintDirective } from 'fd-ui-kit';

import { NutrientBadgesComponent } from '../../../../components/shared/nutrient-badges/nutrient-badges';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveMealImageUrl } from '../../../../shared/lib/meal-image.util';
import type { FavoriteMeal } from '../../../../shared/models/meal.data';

const MAX_IMAGES = 4;

@Component({
    selector: 'fd-favorite-meal-row',
    imports: [FdUiFocusWhenDirective, TranslatePipe, FdUiButtonComponent, FdUiHintDirective, NutrientBadgesComponent, LocalizedNumberPipe],
    templateUrl: './favorite-meal-row.html',
    styleUrl: './favorite-meal-row.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteMealRowComponent {
    public readonly meal = input.required<FavoriteMeal>();
    public readonly remove = output<FavoriteMeal>();
    public readonly removed = input(false);
    public readonly restoring = input(false);
    public readonly restoreFailed = input(false);
    public readonly restore = output<FavoriteMeal>();
    protected readonly undoMessageKey = computed(() => (this.restoreFailed() ? 'MEAL_FAVORITES.RESTORE_ERROR' : 'MEAL_FAVORITES.REMOVED'));
    public readonly focusRequested = input(false);
    public readonly focusHandled = output();

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
    private readonly imageFailures = createImageFailureState(() => [this.meal().imageUrl?.trim(), ...(this.meal().itemImageUrls ?? [])]);
    protected readonly images = computed(() => {
        const cover = this.meal().imageUrl?.trim();
        const failed = this.imageFailures.unavailable();
        if (cover !== undefined && cover.length > 0 && !failed.has(cover)) {
            return [cover];
        }
        return [...new Set(this.meal().itemImageUrls)].filter(url => url.trim().length > 0 && !failed.has(url)).slice(0, MAX_IMAGES);
    });
    protected readonly placeholder = computed(() => resolveMealImageUrl(null, this.meal().mealType));
    protected imageFailed(url: string): void {
        this.imageFailures.markFailed(url);
    }
}
