import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { normalizeQualityScore } from '../../../shared/lib/quality-score.utils';
import { MediaCardComponent } from '../media-card/media-card.component';
import type {
    EntityCardCollageImage,
    EntityCardCollageState,
    EntityCardNormalizedQuality,
    EntityCardNutrition,
    EntityCardOwnershipIcon,
    EntityCardPreviewInteractionState,
    EntityCardQuality,
} from './entity-card.types';
import { EntityCardActionsComponent } from './entity-card-actions.component';
import { EntityCardBodyComponent } from './entity-card-body.component';
import { EntityCardThumbComponent } from './entity-card-thumb.component';

const COLLAGE_VISIBLE_LIMIT = 4;

@Component({
    selector: 'fd-entity-card',
    imports: [MediaCardComponent, EntityCardThumbComponent, EntityCardBodyComponent, EntityCardActionsComponent],
    templateUrl: './entity-card.component.html',
    styleUrl: './entity-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly imageUrl = input<string | null | undefined>(null);
    public readonly collageImages = input<readonly EntityCardCollageImage[]>([]);
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

    public readonly open = output();
    public readonly preview = output();
    public readonly favoriteToggle = output();
    public readonly action = output();

    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly normalizedQuality = computed<EntityCardNormalizedQuality | null>(() => {
        const quality = this.quality();
        if (quality === null) {
            return null;
        }

        return {
            ...quality,
            score: normalizeQualityScore(quality.score),
            hintKey: `QUALITY.${quality.grade.toUpperCase()}`,
        };
    });

    public readonly visibleCollageImages = computed(() => this.collageImages().slice(0, COLLAGE_VISIBLE_LIMIT));
    public readonly collageState = computed<EntityCardCollageState>(() => {
        const images = this.visibleCollageImages();

        return {
            images,
            count: images.length,
            hasImages: images.length > 0,
        };
    });
    public readonly hasPreviewImage = computed(() => {
        const imageUrl = this.imageUrl()?.trim() ?? '';
        return this.previewable() && (imageUrl.length > 0 || this.collageState().hasImages);
    });
    public readonly previewInteractionState = computed<EntityCardPreviewInteractionState>(() => {
        this.languageVersion();
        const hasPreviewImage = this.hasPreviewImage();
        const previewLabel = hasPreviewImage ? this.translateService.instant('IMAGE_PREVIEW.OPEN') : null;

        return {
            hint: previewLabel,
            role: hasPreviewImage ? 'button' : null,
            tabIndex: hasPreviewImage ? '0' : null,
            ariaLabel: previewLabel,
        };
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    public handleOpen(): void {
        this.open.emit();
    }

    public handlePreview(): void {
        this.preview.emit();
    }

    public handleFavoriteToggle(): void {
        this.favoriteToggle.emit();
    }

    public handleAction(): void {
        this.action.emit();
    }
}
