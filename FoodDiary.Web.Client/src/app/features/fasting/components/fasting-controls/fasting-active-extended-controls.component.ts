import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { FastingCustomActionState } from './fasting-controls.types';

@Component({
    selector: 'fd-fasting-active-extended-controls',
    imports: [FormsModule, TranslatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './fasting-active-extended-controls.component.html',
    styleUrl: './fasting-controls.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class FastingActiveExtendedControlsComponent {
    public readonly canExtendActiveSession = input.required<boolean>();
    public readonly isExtendPanelExpanded = input.required<boolean>();
    public readonly isReducePanelExpanded = input.required<boolean>();
    public readonly isCustomExtendExpanded = input.required<boolean>();
    public readonly isCustomReduceExpanded = input.required<boolean>();
    public readonly extendPanelToggleLabel = input.required<string>();
    public readonly reducePanelToggleLabel = input.required<string>();
    public readonly customExtendActionState = input.required<FastingCustomActionState>();
    public readonly customReduceActionState = input.required<FastingCustomActionState>();
    public readonly extendHours = input.required<number>();
    public readonly reduceHours = input.required<number>();
    public readonly isExtending = input.required<boolean>();
    public readonly isReducing = input.required<boolean>();
    public readonly isEnding = input.required<boolean>();
    public readonly isUpdatingCycle = input.required<boolean>();
    public readonly endActionLabelKey = input.required<string>();

    public readonly extendPanelToggle = output();
    public readonly reducePanelToggle = output();
    public readonly extendDay = output();
    public readonly extend36Hours = output();
    public readonly customExtendShow = output();
    public readonly extendHoursChange = output<string | number>();
    public readonly extendCustom = output();
    public readonly reduce4Hours = output();
    public readonly reduce8Hours = output();
    public readonly customReduceShow = output();
    public readonly reduceHoursChange = output<string | number>();
    public readonly reduceCustom = output();
    public readonly end = output();

    protected readonly extendActionsDisabled = (): boolean => this.isExtending() || this.isReducing() || this.isEnding();
    protected readonly reduceActionsDisabled = (): boolean => this.isReducing() || this.isExtending() || this.isEnding();
    protected readonly endDisabled = (): boolean => this.isEnding() || this.isReducing() || this.isExtending() || this.isUpdatingCycle();
}
