import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { CycleDayCarePromptsComponent } from '../cycle-day-care-prompts/cycle-day-care-prompts';
import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-day-item',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiButtonComponent, CycleDayCarePromptsComponent],
    templateUrl: './cycle-day-item.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDayItemComponent {
    public readonly item = input.required<CycleDayViewModel>();
    public readonly isClearing = input(false);
    public readonly clearDay = output<string>();
}
