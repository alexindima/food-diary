import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-fasting-active-basic-controls',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './fasting-active-basic-controls.component.html',
    styleUrl: './fasting-controls.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class FastingActiveBasicControlsComponent {
    public readonly canManageCurrentCyclicDay = input.required<boolean>();
    public readonly skipCycleActionLabelKey = input.required<string>();
    public readonly postponeCycleActionLabelKey = input.required<string>();
    public readonly endActionLabelKey = input.required<string>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();

    public readonly skip = output();
    public readonly postpone = output();
    public readonly end = output();

    protected readonly cyclicActionDisabled = (): boolean => this.isUpdatingCycle() || this.isEnding();
}
