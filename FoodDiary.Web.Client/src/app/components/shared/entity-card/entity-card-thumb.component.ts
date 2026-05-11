import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import type { EntityCardCollageState } from './entity-card.component';

@Component({
    selector: 'fd-entity-card-thumb',
    imports: [NgOptimizedImage, FdUiHintDirective, FdUiIconComponent],
    templateUrl: './entity-card-thumb.component.html',
    styleUrl: './entity-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardThumbComponent {
    public readonly imageUrl = input<string | null | undefined>(null);
    public readonly imageAlt = input.required<string>();
    public readonly imageIcon = input.required<string>();
    public readonly collage = input.required<EntityCardCollageState>();
    public readonly hasPreviewImage = input.required<boolean>();
    public readonly previewInteraction = input.required<EntityCardPreviewInteractionState>();

    public readonly preview = output<void>();

    public handlePreview(event: Event): void {
        event.stopPropagation();

        if (!this.hasPreviewImage()) {
            return;
        }

        this.preview.emit();
    }
}

export interface EntityCardPreviewInteractionState {
    hint: string | null;
    role: string | null;
    tabIndex: string | null;
    ariaLabel: string | null;
}
