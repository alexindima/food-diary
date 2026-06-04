import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import type { FdUiRadioOption } from './fd-ui-radio.types';
import { FdUiRadioOptionsComponent } from './fd-ui-radio-options';

export type { FdUiRadioOption } from './fd-ui-radio.types';

let nextId = 0;

@Component({
    selector: 'fd-ui-radio-group',
    imports: [FdUiRadioOptionsComponent],
    templateUrl: './fd-ui-radio-group.html',
    styleUrls: ['./fd-ui-radio-group.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiRadioGroupComponent<T = unknown> implements FormValueControl<T | null> {
    protected readonly isEqual = Object.is;

    public readonly id = input(`fd-radio-${nextId++}`);
    public readonly label = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly orientation = input<'vertical' | 'horizontal'>('vertical');
    public readonly options = input<Array<FdUiRadioOption<T>>>([]);
    public readonly disabled = input(false);
    public readonly value = model<T | null>(null);
    public readonly touched = model(false);

    protected selectOption(option: FdUiRadioOption<T>): void {
        if (this.disabled()) {
            return;
        }

        this.value.set(option.value);
    }

    protected touchControl(): void {
        this.touched.set(true);
    }

    protected selectOptionByKeyboard(index: number, event: KeyboardEvent): void {
        const options = this.options();
        if (options.length === 0) {
            return;
        }

        let nextIndex: number | null = null;

        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown': {
                nextIndex = (index + 1) % options.length;
                break;
            }
            case 'ArrowLeft':
            case 'ArrowUp': {
                nextIndex = (index - 1 + options.length) % options.length;
                break;
            }
            case 'Home': {
                nextIndex = 0;
                break;
            }
            case 'End': {
                nextIndex = options.length - 1;
                break;
            }
            default: {
                return;
            }
        }

        event.preventDefault();
        this.selectOption(options[nextIndex]);
    }

    protected trackByValue(_: number, option: FdUiRadioOption<T>): unknown {
        return option.value;
    }
}
