import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import type { FastingProtocol } from '../../../models/fasting.data';

@Component({
    selector: 'fd-fasting-cyclic-setup-controls',
    imports: [TranslatePipe, FdUiSegmentedToggleComponent, FdUiInputComponent],
    templateUrl: './fasting-cyclic-setup-controls.html',
    styleUrl: '../fasting-controls.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class FastingCyclicSetupControlsComponent {
    public readonly cyclicPresetOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly cyclicEatDayProtocolOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly selectedCyclicPresetValue = input.required<string>();
    public readonly cyclicEatDayProtocol = input.required<FastingProtocol>();
    public readonly cyclicFastDays = input.required<number>();
    public readonly cyclicEatDays = input.required<number>();
    public readonly cyclicEatDayFastHours = input.required<number>();
    public readonly cyclicEatDayEatingWindowHours = input.required<number>();
    public readonly isCustomCyclicPresetSelected = input.required<boolean>();

    public readonly cyclicPresetChange = output<string>();
    public readonly cyclicFastDaysChange = output<string | number>();
    public readonly cyclicEatDaysChange = output<string | number>();
    public readonly cyclicEatDayProtocolChange = output<string>();
    public readonly cyclicEatDayFastHoursChange = output<string | number>();

    protected normalizeNumericControlValue(value: string | number | null | undefined): string | number {
        return value ?? '';
    }
}
