import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { CycleDayCarePromptViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-day-care-prompts',
    imports: [TranslatePipe],
    templateUrl: './cycle-day-care-prompts.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDayCarePromptsComponent {
    public readonly items = input.required<CycleDayCarePromptViewModel[]>();
}
