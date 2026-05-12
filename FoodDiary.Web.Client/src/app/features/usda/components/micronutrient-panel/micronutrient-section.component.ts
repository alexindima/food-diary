import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { MicronutrientView } from './micronutrient-panel.component';

@Component({
    selector: 'fd-micronutrient-section',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './micronutrient-section.component.html',
    styleUrl: './micronutrient-panel.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientSectionComponent {
    public readonly titleKey = input.required<string>();
    public readonly nutrients = input.required<MicronutrientView[]>();
}
