import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { FdUiRadioOption } from './fd-ui-radio-group.component';

interface FdUiRadioOptionKeydownEvent {
    index: number;
    event: KeyboardEvent;
}

@Component({
    selector: 'fd-ui-radio-options',
    templateUrl: './fd-ui-radio-options.component.html',
    styleUrl: './fd-ui-radio-group.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiRadioOptionsComponent<T = unknown> {
    protected readonly isEqual = Object.is;

    public readonly id = input.required<string>();
    public readonly options = input.required<FdUiRadioOption<T>[]>();
    public readonly disabled = input.required<boolean>();
    public readonly value = input.required<T | null>();

    public readonly optionSelected = output<FdUiRadioOption<T>>();
    public readonly optionBlur = output<void>();
    public readonly optionKeydown = output<FdUiRadioOptionKeydownEvent>();

    protected trackByValue(_: number, option: FdUiRadioOption<T>): unknown {
        return option.value;
    }
}
