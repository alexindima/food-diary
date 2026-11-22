import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { fdUiCoerceInputNumberValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';

import type { CyclicPresetSelection, FastingMode, FastingProtocol } from '../../../../../shared/models/fasting.data';
import { normalizeFastingHours, normalizeIntermittentFastHours } from '../../../lib/fasting-input-normalization';
import type { IntermittentFastingProtocol } from '../../../models/fasting-start-intent';
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
    protected readonly normalizeFastingHours = normalizeFastingHours;
    protected readonly normalizeIntermittentFastHours = normalizeIntermittentFastHours;

    public readonly modeOptions = input.required<Array<FdUiSegmentedToggleOption<FastingMode>>>();
    public readonly selectedMode = input.required<FastingMode>();
    public readonly intermittentProtocolOptions = input.required<Array<FdUiSegmentedToggleOption<FastingProtocol>>>();
    public readonly extendedProtocolOptions = input.required<Array<FdUiSegmentedToggleOption<FastingProtocol>>>();
    public readonly cyclicPresetOptions = input.required<Array<FdUiSegmentedToggleOption<CyclicPresetSelection>>>();
    public readonly cyclicEatDayProtocolOptions = input.required<Array<FdUiSegmentedToggleOption<IntermittentFastingProtocol>>>();
    public readonly selectedProtocol = input.required<FastingProtocol>();
    public readonly selectedCyclicPresetValue = input.required<CyclicPresetSelection>();
    public readonly cyclicEatDayProtocol = model.required<IntermittentFastingProtocol>();
    public readonly customIntermittentFastHours = input.required<number>();
    public readonly customIntermittentEatingWindowHours = input.required<number>();
    public readonly customHours = input.required<number>();
    public readonly cyclicFastDays = input.required<number>();
    public readonly cyclicEatDays = input.required<number>();
    public readonly cyclicEatDayFastHours = input.required<number>();
    public readonly cyclicEatDayEatingWindowHours = input.required<number>();
    public readonly isCustomCyclicPresetSelected = input.required<boolean>();
    public readonly isStarting = input.required<boolean>();
    public readonly showGuidance = input(false);
    public readonly descriptionKey = input.required<string>();
    public readonly selectionSummary = input.required<string>();
    public readonly inlineCustomControls = input(false);
    public readonly showStartAction = input(true);

    public readonly modeChange = output<FastingMode>();
    public readonly protocolChange = output<FastingProtocol>();
    public readonly customIntermittentFastHoursChange = output<string | number>();
    public readonly customHoursChange = output<string | number>();
    public readonly cyclicPresetChange = output<CyclicPresetSelection>();
    public readonly cyclicFastDaysChange = output<string | number>();
    public readonly cyclicEatDaysChange = output<string | number>();
    public readonly cyclicEatDayFastHoursChange = output<string | number>();
    public readonly startFasting = output();

    protected normalizeNumericControlValue(value: FdUiInputValue): string | number {
        return fdUiCoerceInputNumberValue(value);
    }
}
