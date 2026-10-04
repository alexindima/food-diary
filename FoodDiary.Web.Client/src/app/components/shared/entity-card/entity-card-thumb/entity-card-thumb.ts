import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { EntityCardCollageState, EntityCardPreviewInteractionState } from '../entity-card-lib/entity-card.types';

@Component({
    selector: 'fd-entity-card-thumb',
    imports: [FdUiHintDirective, FdUiIconComponent],
    templateUrl: './entity-card-thumb.html',
    styleUrl: '../entity-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardThumbComponent {
    public readonly imageUrl = input<string | null | undefined>(null);
    public readonly imageAlt = input.required<string>();
    public readonly imageIcon = input.required<string>();
    public readonly collage = input.required<EntityCardCollageState>();
    public readonly hasPreviewImage = input.required<boolean>();
    public readonly previewInteraction = input.required<EntityCardPreviewInteractionState>();

    public readonly preview = output();
    private readonly unavailableImages = linkedSignal({
        source: () => ({ imageUrl: this.imageUrl(), collage: this.collage() }),
        computation: (): ReadonlySet<string> => new Set(),
    });
    protected readonly visibleImageUrl = computed(() => {
        const imageUrl = this.imageUrl();
        return imageUrl !== null && imageUrl !== undefined && imageUrl.trim().length > 0 && !this.unavailableImages().has(imageUrl)
            ? imageUrl
            : null;
    });
    protected readonly visibleCollageImages = computed(() =>
        this.collage().images.filter(image => !this.unavailableImages().has(image.url)),
    );
    protected readonly hasVisibleImage = computed(() => this.visibleImageUrl() !== null || this.visibleCollageImages().length > 0);
    protected readonly isPlaceholder = computed(() =>
        /assets\/images\/stubs\/(?:meals\/(?:breakfast|lunch|dinner|snack|other)\.(?:svg|png)|products\/(?:meat|fruit|vegetable|cheese|dairy|seafood|grain|beverage|dessert|other)\.png|receipt\.png)$/.test(
            this.imageUrl() ?? '',
        ),
    );

    protected markImageUnavailable(imageUrl: string): void {
        this.unavailableImages.update(images => new Set([...images, imageUrl]));
    }

    protected previewCardImage(event: Event): void {
        event.stopPropagation();

        if (!this.hasPreviewImage()) {
            return;
        }

        this.preview.emit();
    }
}
