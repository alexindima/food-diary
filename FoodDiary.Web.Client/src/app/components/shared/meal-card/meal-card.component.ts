import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { Meal } from '../../../features/meals/models/meal.data';
import { resolveMealImageUrl } from '../../../utils/meal-stub.utils';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';

@Component({
    selector: 'fd-meal-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NgOptimizedImage, NutrientBadgesComponent],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    public readonly meal = input.required<Meal>();
    public readonly open = output<Meal>();
    private readonly fallbackMealImage = 'assets/images/stubs/meals/other.svg';

    public readonly coverImage = computed(() => {
        const image = this.meal().imageUrl?.trim();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        return resolved ?? this.fallbackMealImage;
    });

    public readonly itemCount = computed(() => {
        const meal = this.meal();
        const manualCount = meal.items?.length ?? 0;
        const aiCount = meal.aiSessions?.reduce((total, session) => total + (session.items?.length ?? 0), 0) ?? 0;
        return manualCount + aiCount;
    });

    public handleOpen(): void {
        this.open.emit(this.meal());
    }
}
