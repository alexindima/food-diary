import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { NutrientBadgesComponent } from '../../nutrient-badges/nutrient-badges';
import type { EntityCardNormalizedQuality, EntityCardNutrition, EntityCardOwnershipIcon } from '../entity-card-lib/entity-card.types';

@Component({
    selector: 'fd-entity-card-body',
    imports: [FdUiIconComponent, NutrientBadgesComponent],
    templateUrl: './entity-card-body.html',
    styleUrl: '../entity-card.scss',
    host: {
        class: 'entity-card-body-host',
    },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityCardBodyComponent {
    public readonly ownershipIcon = input<EntityCardOwnershipIcon>(null);
    public readonly title = input.required<string>();
    public readonly titleMuted = input<string | null>(null);
    public readonly description = input<string | null>(null);
    public readonly nutrition = input.required<EntityCardNutrition>();
    public readonly calories = input.required<number>();
    public readonly quality = input<EntityCardNormalizedQuality | null>(null);
}
