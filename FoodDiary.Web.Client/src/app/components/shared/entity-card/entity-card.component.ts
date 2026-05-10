import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { type QualityGrade } from '../../../features/products/models/product.data';
import { MediaCardComponent } from '../media-card/media-card.component';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';

export interface EntityCardNutrition {
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}

export interface EntityCardQuality {
    score: number;
    grade: QualityGrade;
}

export interface EntityCardCollageImage {
    url: string;
    alt: string;
}

export type EntityCardOwnershipIcon = 'person' | 'groups' | null;

@Component({
    selector: 'fd-entity-card',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        NutrientBadgesComponent,
        MediaCardComponent,
    ],
    templateUrl: './entity-card.component.html',
    styleUrl: './entity-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardComponent {
    public readonly imageUrl = input<string | null | undefined>(null);
    public readonly collageImages = input<ReadonlyArray<EntityCardCollageImage>>([]);
    public readonly imageAlt = input.required<string>();
    public readonly imageIcon = input('restaurant');
    public readonly previewable = input(false);

    public readonly showFavorite = input(false);
    public readonly isFavorite = input(false);
    public readonly favoriteLoading = input(false);
    public readonly favoriteAriaLabel = input<string | null>(null);

    public readonly ownershipIcon = input<EntityCardOwnershipIcon>(null);

    public readonly title = input.required<string>();
    public readonly titleMuted = input<string | null>(null);
    public readonly description = input<string | null>(null);

    public readonly nutrition = input.required<EntityCardNutrition>();
    public readonly quality = input<EntityCardQuality | null>(null);
    public readonly calories = input.required<number>();

    public readonly showAction = input(false);
    public readonly actionIcon = input('add');
    public readonly actionAriaLabel = input<string | null>(null);

    public readonly open = output<void>();
    public readonly preview = output<void>();
    public readonly favoriteToggle = output<void>();
    public readonly action = output<void>();

    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly normalizedQuality = computed(() => {
        const quality = this.quality();
        if (!quality) {
            return null;
        }

        return {
            ...quality,
            score: Math.round(Math.min(100, Math.max(0, quality.score))),
            hintKey: `QUALITY.${quality.grade.toUpperCase()}`,
        };
    });

    public readonly visibleCollageImages = computed(() => this.collageImages().slice(0, 4));
    public readonly collageState = computed(() => {
        const images = this.visibleCollageImages();

        return {
            images,
            count: images.length,
            hasImages: images.length > 0,
        };
    });
    public readonly hasPreviewImage = computed(() =>
        Boolean(this.previewable() && (this.imageUrl()?.trim() || this.collageState().hasImages)),
    );
    public readonly previewInteractionState = computed<EntityCardPreviewInteractionState>(() =>
        this.hasPreviewImage()
            ? {
                  hintKey: 'IMAGE_PREVIEW.OPEN',
                  role: 'button',
                  tabIndex: '0',
                  ariaLabelKey: 'IMAGE_PREVIEW.OPEN',
              }
            : {
                  hintKey: null,
                  role: null,
                  tabIndex: null,
                  ariaLabelKey: null,
              },
    );

    public handleOpen(): void {
        this.open.emit();
    }

    public handlePreview(event: Event): void {
        event.stopPropagation();

        if (!this.hasPreviewImage()) {
            return;
        }

        this.preview.emit();
    }

    public handleFavoriteToggle(event: Event): void {
        event.stopPropagation();
        this.favoriteToggle.emit();
    }

    public handleAction(event: Event): void {
        event.stopPropagation();
        this.action.emit();
    }
}

interface EntityCardPreviewInteractionState {
    hintKey: string | null;
    role: string | null;
    tabIndex: string | null;
    ariaLabelKey: string | null;
}
