import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { fdUiCoerceInputNumberValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';

import type { FastingMode, FastingProtocol } from '../../../models/fasting.data';
import { FastingCyclicSetupControlsComponent } from '../cyclic-setup/fasting-cyclic-setup-controls';

@Component({
    selector: 'fd-fasting-setup-controls',
    imports: [TranslatePipe, FdUiSegmentedToggleComponent, FdUiButtonComponent, FdUiInputComponent, FastingCyclicSetupControlsComponent],
    templateUrl: './fasting-setup-controls.html',
    styleUrl: '../fasting-controls.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class FastingSetupControlsComponent {
    public readonly modeOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly selectedMode = input.required<FastingMode>();
    public readonly intermittentProtocolOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly extendedProtocolOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly cyclicPresetOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly cyclicEatDayProtocolOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly selectedProtocol = input.required<FastingProtocol>();
    public readonly selectedCyclicPresetValue = input.required<string>();
    public readonly cyclicEatDayProtocol = input.required<FastingProtocol>();
    public readonly customIntermittentFastHours = input.required<number>();
    public readonly customIntermittentEatingWindowHours = input.required<number>();
    public readonly customHours = input.required<number>();
    public readonly cyclicFastDays = input.required<number>();
    public readonly cyclicEatDays = input.required<number>();
    public readonly cyclicEatDayFastHours = input.required<number>();
    public readonly cyclicEatDayEatingWindowHours = input.required<number>();
    public readonly isCustomCyclicPresetSelected = input.required<boolean>();
    public readonly isStarting = input.required<boolean>();

    public readonly modeChange = output<string>();
    public readonly protocolChange = output<string>();
    public readonly customIntermittentFastHoursChange = output<string | number>();
    public readonly customHoursChange = output<string | number>();
    public readonly cyclicPresetChange = output<string>();
    public readonly cyclicFastDaysChange = output<string | number>();
    public readonly cyclicEatDaysChange = output<string | number>();
    public readonly cyclicEatDayProtocolChange = output<string>();
    public readonly cyclicEatDayFastHoursChange = output<string | number>();
    public readonly startFasting = output();

    protected normalizeNumericControlValue(value: FdUiInputValue): string | number {
        return fdUiCoerceInputNumberValue(value);
    }
}
