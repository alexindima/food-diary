import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { resolveMealImageUrl } from '../../../shared/lib/meal-image.util';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';
import { MediaCardComponent } from '../media-card/media-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

export interface MealCardItem {
    id: string;
    date: string | Date;
    mealType?: string | null;
    imageUrl?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    totalAlcohol: number;
    items?: Array<unknown> | null;
    aiSessions?: Array<{ items?: Array<unknown> | null } | null> | null;
}

@Component({
    selector: 'fd-meal-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NgOptimizedImage, NutrientBadgesComponent, MediaCardComponent],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);

    public readonly meal = input.required<MealCardItem>();
    public readonly open = output<void>();
    private readonly fallbackMealImage = 'assets/images/stubs/meals/other.svg';

    public readonly coverImage = computed(() => {
        const image = this.meal().imageUrl?.trim();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        return resolved ?? this.fallbackMealImage;
    });

    public readonly itemCount = computed(() => {
        const meal = this.meal();
        const manualCount = meal.items?.length ?? 0;
        const aiCount = meal.aiSessions?.reduce((total, session) => total + (session?.items?.length ?? 0), 0) ?? 0;
        return manualCount + aiCount;
    });

    public handleOpen(): void {
        this.open.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.meal().imageUrl?.trim());
    }

    public handlePreview(event: Event): void {
        event.stopPropagation();

        const imageUrl = this.meal().imageUrl?.trim();
        if (!imageUrl) {
            return;
        }

        const title = this.meal().mealType
            ? this.translateService.instant(`MEAL_CARD.MEAL_TYPES.${this.meal().mealType}`)
            : this.translateService.instant('MEAL_CARD.MEAL_TYPES.OTHER');

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'min(calc(100vw - 3rem), 1200px)',
            maxWidth: '1200px',
            data: {
                imageUrl,
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', {
                    name: title,
                }),
                title,
            },
        });
    }
}
