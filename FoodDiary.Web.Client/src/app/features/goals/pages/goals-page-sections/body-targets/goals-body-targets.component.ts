import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { BodyTarget, BodyTargetInputChange } from '../../goals-page-lib/goals-page.models';

@Component({
    selector: 'fd-goals-body-targets',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './goals-body-targets.component.html',
    styleUrl: '../../goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsBodyTargetsComponent {
    public readonly targets = input.required<BodyTarget[]>();

    public readonly targetInput = output<BodyTargetInputChange>();
}
